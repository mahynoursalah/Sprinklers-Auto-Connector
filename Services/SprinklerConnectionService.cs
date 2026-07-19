using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Sprinklers_Connectors.Models;

namespace Sprinklers_Connectors.Services
{
    public class SprinklerConnectionService
    {
        private readonly PipeSearchServices _pipeSearchService;
        private readonly ConnectorService _connectorService;
        private readonly ConnectionService _connectionService;
        private readonly TeeService _teeService;
        private readonly HashSet<ElementId> _createdBranchPipeIds;

        public SprinklerConnectionService()
        {
            _pipeSearchService = new PipeSearchServices();
            _connectorService = new ConnectorService();
            _connectionService = new ConnectionService();
            _teeService = new TeeService();
            _createdBranchPipeIds = new HashSet<ElementId>();
        }

        // Legacy overload kept for compatibility - returns only a bool
        public bool ConnectSprinkler(Document document, FamilyInstance sprinkler)
        {
            var pipes = _pipeSearchService.GetPipes(document);
            return ConnectSprinklerDetailed(document, sprinkler, pipes).Success;
        }

        public bool ConnectSprinkler(Document document, FamilyInstance sprinkler, List<Pipe> pipes)
        {
            return ConnectSprinklerDetailed(document, sprinkler, pipes).Success;
        }

        public SprinklerConnectionResult ConnectSprinklerDetailed(
            Document document,
            FamilyInstance sprinkler,
            List<Pipe> pipes)
        {
            // 2. Find nearest pipe
            var nearestPipe = _pipeSearchService.FindNearestPipe(
                pipes,
                sprinkler,
                _createdBranchPipeIds);

            if (nearestPipe is null)
                return SprinklerConnectionResult.Fail(ConnectionFailureReason.NoPipeFound);

            // 3. Get sprinkler connector
            var sprinklerConnector = _connectorService.GetUnconnectedConnector(sprinkler);

            if (sprinklerConnector is null)
                return SprinklerConnectionResult.Fail(ConnectionFailureReason.NoUnconnectedSprinklerConnector);

            // 4. Sprinkler connector origin
            var startPoint = sprinklerConnector.Origin;

            // 5. Project connector on pipe
            var connectionPoint = _connectionService.GetEndPoint(nearestPipe, startPoint);

            if (connectionPoint is null)
                return SprinklerConnectionResult.Fail(ConnectionFailureReason.ProjectionFailed);

            // ================== [V2 SMART ROUTING LOGIC] ==================
            // بدلاً من التوصيل المباشر بالرشاش، هنخلق نقطة كوع مساعدة (Elbow Point) 
            // النقطة دي بتاخد الـ X والـ Y بتوع الرشاش، لكن بتحتفظ بـمنسوب الـ Z بتاع الماسورة الرئيسية عشان يفضل أفقي
            XYZ elbowPoint = new XYZ(startPoint.X, startPoint.Y, connectionPoint.Z);

            // 6. Detect connection case
            var connectionPointType = _pipeSearchService.GetConnectionPointType(nearestPipe, connectionPoint);

            // 7. Create branch pipe (الآن هيمشي أفقي تماماً من الماسورة الرئيسية حتى نقطة الكوع فوق الرشاش)
        

            // يبقى:
            var branchPipe = _connectionService.CreateBranchPipe(
                document, nearestPipe, connectionPoint, elbowPoint);// مررنا نقطة الكوع بدلاً من الرشاش ليصبح أفقي

            if (branchPipe is null)
                return SprinklerConnectionResult.Fail(ConnectionFailureReason.BranchPipeCreationFailed);

            _createdBranchPipeIds.Add(branchPipe.Id);
            document.Regenerate();

            SprinklerConnectionResult result;
            Pipe? createdSecondMainPipe = null;

            try
            {
                (result, createdSecondMainPipe) = HandleConnection(
                    document,
                    sprinkler,
                    nearestPipe,
                    branchPipe,
                    connectionPoint,
                    connectionPointType,
                    elbowPoint); // مررنا النقطة المساعدة للدالة الداخلية
            }
            catch (Exception)
            {
                result = SprinklerConnectionResult.Fail(ConnectionFailureReason.UnexpectedException);
            }

            if (!result.Success)
            {
                RollbackBranchPipe(document, branchPipe);
            }
            else if (createdSecondMainPipe is not null)
            {
                pipes.Add(createdSecondMainPipe);
            }

            return result;
        }

        private (SprinklerConnectionResult Result, Pipe? SecondMainPipe) HandleConnection(
            Document document,
            FamilyInstance sprinkler,
            Pipe nearestPipe,
            Pipe branchPipe,
            XYZ connectionPoint,
            ConnectionPointType connectionPointType,
            XYZ elbowPoint) // إضافة البارامتر الجديد هنا
        {
            Pipe? secondMainPipe = null;

            // 8. Handle connection type (Tee or Elbow on Main Pipe)
            if (connectionPointType == ConnectionPointType.Middle)
            {
                secondMainPipe = _teeService.BreakMainPipe(document, nearestPipe, connectionPoint);

                if (secondMainPipe is null)
                    return (SprinklerConnectionResult.Fail(ConnectionFailureReason.MainPipeBreakFailed), null);

                document.Regenerate();

                var tee = _teeService.CreateTee(
                    document,
                    nearestPipe,
                    secondMainPipe,
                    branchPipe,
                    connectionPoint);

                if (tee is null)
                    return (SprinklerConnectionResult.Fail(ConnectionFailureReason.TeeCreationFailed), null);
            }
            else
            {
                var elbow = _connectorService.ConnectBranchToPipeEndpoint(
                    document,
                    nearestPipe,
                    branchPipe,
                    connectionPoint);

                if (elbow is null)
                    return (SprinklerConnectionResult.Fail(ConnectionFailureReason.ElbowCreationFailed), null);
            }

            document.Regenerate();

            // ================== [V2 VERTICAL DROP GENERATION] ==================
            // هنا هنخلق الـ Vertical Drop Pipe من نقطة الكوع أوتوماتيكياً للأسفل باتجاه الرشاش
            Pipe? verticalDropPipe = null;
            try
            {
                // نأخذ نفس الـ Type والـ Level والـ System الخاص بالبرانش الأفقي لضمان عدم حدوث إيرور
                ElementId pipeTypeId = branchPipe.GetTypeId();
                // حطي السطر ده مكانهم:
                ElementId levelId = branchPipe.LevelId;
                ElementId systemTypeId = branchPipe.MEPSystem.GetTypeId();

                // رسم الماسورة الرأسية (Drop)
                verticalDropPipe = Pipe.Create(document, systemTypeId, pipeTypeId, levelId, elbowPoint, _connectorService.GetUnconnectedConnector(sprinkler).Origin);
            }
            catch (Exception)
            {
                return (SprinklerConnectionResult.Fail(ConnectionFailureReason.BranchPipeCreationFailed), null);
            }

            if (verticalDropPipe is null)
                return (SprinklerConnectionResult.Fail(ConnectionFailureReason.BranchPipeCreationFailed), null);

            document.Regenerate();

            // ربط الماسورة الأفقية (branchPipe) بالماسورة الرأسية (verticalDropPipe) عن طريق كوع Fitting أوتوماتيكي
            try
            {
                Connector? branchConnectorOpt = null;
                Connector? dropConnectorOpt = null;

                // البحث عن الـ Connectors القريبة من نقطة الـ Elbow لربط الكوع
                foreach (Connector con in branchPipe.ConnectorManager.Connectors)
                {
                    if (con.Origin.IsAlmostEqualTo(elbowPoint, 0.01)) { branchConnectorOpt = con; break; }
                }
                foreach (Connector con in verticalDropPipe.ConnectorManager.Connectors)
                {
                    if (con.Origin.IsAlmostEqualTo(elbowPoint, 0.01)) { dropConnectorOpt = con; break; }
                }

                if (branchConnectorOpt != null && dropConnectorOpt != null)
                {
                    document.Create.NewElbowFitting(branchConnectorOpt, dropConnectorOpt);
                }
            }
            catch (Exception)
            {
                return (SprinklerConnectionResult.Fail(ConnectionFailureReason.FinalConnectionFailed), null);
            }

            document.Regenerate();

            // 9. Connect sprinkler last (الآن هنوصل الرشاش بالـ verticalDropPipe المظبوطة رأسياً)
            var isConnected = _connectorService.ConnectSprinklerToBranch(sprinkler, verticalDropPipe);

            var result = isConnected
                ? SprinklerConnectionResult.Ok()
                : SprinklerConnectionResult.Fail(ConnectionFailureReason.FinalConnectionFailed);

            return (result, secondMainPipe);
        }

        private void RollbackBranchPipe(Document document, Pipe branchPipe)
        {
            _createdBranchPipeIds.Remove(branchPipe.Id);
            try
            {
                if (document.GetElement(branchPipe.Id) is not null)
                {
                    document.Delete(branchPipe.Id);
                }
            }
            catch (Exception)
            {
                // Safe ignore
            }
        }
    }
}