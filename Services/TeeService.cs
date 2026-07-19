using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;

namespace Sprinklers_Connectors.Services
{
    public class TeeService
    {
        private readonly ConnectorService _connectorService;

        public TeeService()
        {
            _connectorService =
                new ConnectorService();
        }

        // NOTE: PlumbingUtils.BreakCurve can throw InvalidOperationException
        // if the break point is too close to a pipe end (the resulting
        // segment would be nearly zero length), or if the pipe cannot be
        // split.
        public Pipe? BreakMainPipe(
            Document document,
            Pipe mainPipe,
            XYZ breakPoint)
        {
            try
            {
                var newPipeId =
                    PlumbingUtils.BreakCurve(
                        document,
                        mainPipe.Id,
                        breakPoint);

                if (newPipeId == ElementId.InvalidElementId)
                    return null;

                return document.GetElement(newPipeId) as Pipe;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return null;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return null;
            }
        }

        // NOTE: The same applies to NewTeeFitting - it can throw an
        // exception if the three connectors are not aligned or on the
        // expected plane/direction.
        public FamilyInstance? CreateTee(
            Document document,
            Pipe firstMainPipe,
            Pipe secondMainPipe,
            Pipe branchPipe,
            XYZ connectionPoint)
        {
            var firstMainConnector =
                _connectorService.GetNearestConnector(
                    firstMainPipe,
                    connectionPoint);

            var secondMainConnector =
                _connectorService.GetNearestConnector(
                    secondMainPipe,
                    connectionPoint);

            var branchConnector =
                _connectorService.GetNearestConnector(
                    branchPipe,
                    connectionPoint);

            if (firstMainConnector is null ||
                secondMainConnector is null ||
                branchConnector is null)
            {
                return null;
            }

            try
            {
                return document.Create.NewTeeFitting(
                    firstMainConnector,
                    secondMainConnector,
                    branchConnector);
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return null;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return null;
            }
        }
    }
}