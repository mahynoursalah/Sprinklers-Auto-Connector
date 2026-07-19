using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Sprinklers_Connectors.Extensions;

namespace Sprinklers_Connectors.Services
{
    public class ConnectorService
    {
        private const double Tolerance = 0.001;

        public Connector? GetUnconnectedConnector(FamilyInstance familyInstance)
        {
            return familyInstance
                .GetConnectors()
                .FirstOrDefault(c => !c.IsConnected);
        }

        public Connector? GetNearestConnector(
            MEPCurve mepCurve,
            XYZ point)
        {
            return mepCurve
                .ConnectorManager
                .Connectors
                .Cast<Connector>()
                .OrderBy(c => c.Origin.DistanceTo(point))
                .FirstOrDefault();
        }

        public Connector? GetConnectorAtPoint(
            MEPCurve mepCurve,
            XYZ point)
        {
            return mepCurve
                .ConnectorManager
                .Connectors
                .Cast<Connector>()
                .Where(c => c.Origin.DistanceTo(point) < Tolerance)
                .FirstOrDefault();
        }

        public bool ConnectSprinklerToBranch(
            FamilyInstance sprinkler,
            Pipe branchPipe)
        {
            var sprinklerConnector =
                GetUnconnectedConnector(sprinkler);

            if (sprinklerConnector is null)
                return false;

            var branchConnector =
                GetNearestConnector(
                    branchPipe,
                    sprinklerConnector.Origin);

            if (branchConnector is null)
                return false;

            if (branchConnector.IsConnected)
                return false;

            try
            {
                sprinklerConnector.ConnectTo(branchConnector);
                return true;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Connectors incompatible (size/shape mismatch, etc.)
                return false;
            }
        }

        // NOTE: These calls (NewElbowFitting / NewUnionFitting) can throw
        // exceptions instead of returning null when the geometry is not
        // suitable (for example, when the branch pipe is collinear with
        // the main pipe). Without try/catch the code would crash here.
        public FamilyInstance? ConnectBranchToPipeEndpoint(
            Document document,
            Pipe mainPipe,
            Pipe branchPipe,
            XYZ connectionPoint)
        {
            var mainConnector =
                GetConnectorAtPoint(
                    mainPipe,
                    connectionPoint);

            var branchConnector =
                GetConnectorAtPoint(
                    branchPipe,
                    connectionPoint);

            if (mainConnector is null ||
                branchConnector is null)
            {
                // fallback
                mainConnector =
                    GetNearestConnector(mainPipe, connectionPoint);

                branchConnector =
                    GetNearestConnector(branchPipe, connectionPoint);
            }

            if (mainConnector is null ||
                branchConnector is null)
                return null;

            if (mainConnector.IsConnected ||
                branchConnector.IsConnected)
                return null;

            try
            {
                return document.Create.NewElbowFitting(
                    mainConnector,
                    branchConnector);
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                // If the pipes are collinear, no Elbow can be created.
                // Try creating a Union instead.
                return TryCreateUnion(document, mainConnector, branchConnector);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return null;
            }
        }

        private FamilyInstance? TryCreateUnion(
            Document document,
            Connector mainConnector,
            Connector branchConnector)
        {
            try
            {
                return document.Create.NewUnionFitting(
                    mainConnector,
                    branchConnector);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}