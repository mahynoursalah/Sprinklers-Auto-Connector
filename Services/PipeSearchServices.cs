using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Sprinklers_Connectors.Extensions;
using Sprinklers_Connectors.Models;

namespace Sprinklers_Connectors.Services
{
    public class PipeSearchServices
    {
        private const double EndpointTolerance = 0.1;


        public List<Pipe> GetPipes(Document document)
        {
            return new FilteredElementCollector(document)
                .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .Where(pipe =>
                {
                    if (pipe.MEPSystem is not PipingSystem pipingSystem)
                        return false;


                    var systemType =
                        document.GetElement(
                            pipingSystem.GetTypeId())
                        as PipingSystemType;


                    if (systemType is null)
                        return false;


                    return systemType.SystemClassification ==
                               MEPSystemClassification.FireProtectWet

                           || systemType.SystemClassification ==
                               MEPSystemClassification.FireProtectDry

                           || systemType.SystemClassification ==
                               MEPSystemClassification.FireProtectPreaction

                           || systemType.SystemClassification ==
                               MEPSystemClassification.FireProtectOther;
                })
                .ToList();
        }


        public Pipe? FindNearestPipe(
            List<Pipe> pipes,
            FamilyInstance sprinkler,
            HashSet<ElementId> excludedPipeIds)
        {
            var sprinklerConnector =
                sprinkler
                    .GetConnectors()
                    .FirstOrDefault(
                        connector => !connector.IsConnected);


            if (sprinklerConnector is null)
                return null;


            var sprinklerPoint =
                sprinklerConnector.Origin;


            Pipe? nearestPipe = null;

            double nearestDistance =
                double.MaxValue;


            foreach (var pipe in pipes)
            {
                if (excludedPipeIds.Contains(pipe.Id))
                    continue;


                var locationCurve =
                    pipe.Location as LocationCurve;


                if (locationCurve is null)
                    continue;


                var projection =
                    locationCurve.Curve.Project(
                        sprinklerPoint);


                if (projection is null)
                    continue;


                var projectedPoint =
                    projection.XYZPoint;


                var distance =
                    sprinklerPoint.DistanceTo(
                        projectedPoint);


                if (distance < nearestDistance)
                {
                    nearestDistance =
                        distance;

                    nearestPipe =
                        pipe;
                }
            }


            return nearestPipe;
        }


        public ConnectionPointType GetConnectionPointType(
            Pipe pipe,
            XYZ connectionPoint)
        {
            var locationCurve =
                pipe.Location as LocationCurve;


            if (locationCurve is null)
                return ConnectionPointType.Middle;


            var curve =
                locationCurve.Curve;


            var startPoint =
                curve.GetEndPoint(0);


            var endPoint =
                curve.GetEndPoint(1);


            var distanceFromStart =
                connectionPoint.DistanceTo(
                    startPoint);


            var distanceFromEnd =
                connectionPoint.DistanceTo(
                    endPoint);


            if (distanceFromStart <= EndpointTolerance ||
                distanceFromEnd <= EndpointTolerance)
            {
                return ConnectionPointType.Endpoint;
            }


            return ConnectionPointType.Middle;
        }
    }
}