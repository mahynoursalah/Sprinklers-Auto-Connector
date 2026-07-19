using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Sprinklers_Connectors.Extensions;
using Sprinklers_Connectors.Services;

public class ConnectionService
{
    private readonly ConnectorService _connectorService;

    public ConnectionService()
    {
        _connectorService = new ConnectorService();
    }

    // NEW: Creates the horizontal branch pipe using an explicit start
    // point on the main pipe (connectionPoint) and an explicit end point
    // (elbowPoint above the sprinkler). This does NOT use the sprinkler's
    // connector origin as the start point — using it caused the branch
    // pipe's upper connector to be offset from connectionPoint, which
    // made NewTeeFitting fail (connectors not aligned at the break point).
    public Pipe? CreateBranchPipe(
        Document document,
        Pipe pipe,
        XYZ startPoint,
        XYZ endPoint)
    {
        var pipeType = GetPipeType(document, pipe);

        var pipingSystemType =
            GetpipeSystemType(document, pipe);

        var level = GetLevel(document, pipe);

        if (pipeType is null ||
            pipingSystemType is null ||
            level is null)
        {
            return null;
        }

        try
        {
            var branchPipe = Pipe.Create(
                document,
                pipingSystemType.Id,
                pipeType.Id,
                level.Id,
                startPoint,
                endPoint);

            // Note: Pipe.Create() gives the new pipe a default diameter
            // which might not exist in the Routing Preferences for the
            // chosen pipe type (e.g. "Standard"), causing the error
            // "Revit could not find a matching Pipe Segment". The fix is
            // to set the branch pipe diameter to match the main pipe so
            // a valid size from the project preferences is used.
            MatchDiameterToMainPipe(pipe, branchPipe);

            return branchPipe;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            // e.g., startPoint == endPoint (zero-length pipe)
            return null;
        }
    }

    // NOTE: This method previously re-projected the same point unnecessarily
    // even though the orchestrator (SprinklerConnectionService) already
    // computed it to determine Endpoint/Middle. Now this method accepts
    // a ready endpoint instead of recalculating it (an overload remains
    // for callers that need the endpoint computed internally).
    public Pipe? CreateBranchPipe(
        Document document,
        FamilyInstance sprinkler,
        Pipe pipe,
        XYZ endPoint)
    {
        var pipeType = GetPipeType(document, pipe);

        var pipingSystemType =
            GetpipeSystemType(document, pipe);

        var level = GetLevel(document, pipe);

        var sprinklerConnector =
            _connectorService.GetUnconnectedConnector(sprinkler);

        if (pipeType is null ||
            pipingSystemType is null ||
            level is null ||
            sprinklerConnector is null)
        {
            return null;
        }

        var startPoint = sprinklerConnector.Origin;

        try
        {
            var branchPipe = Pipe.Create(
                document,
                pipingSystemType.Id,
                pipeType.Id,
                level.Id,
                startPoint,
                endPoint);

            MatchDiameterToMainPipe(pipe, branchPipe);

            return branchPipe;
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            // e.g., startPoint == endPoint (zero-length pipe)
            return null;
        }
    }

    private void MatchDiameterToMainPipe(Pipe mainPipe, Pipe branchPipe)
    {
        var mainDiameterParam =
            mainPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);

        var branchDiameterParam =
            branchPipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);

        if (mainDiameterParam is null ||
            branchDiameterParam is null ||
            branchDiameterParam.IsReadOnly)
        {
            return;
        }

        var mainDiameter = mainDiameterParam.AsDouble();

        if (mainDiameter > 0)
        {
            branchDiameterParam.Set(mainDiameter);
        }
    }

    // Legacy overload remains for callers that need the endpoint computed
    // internally rather than being supplied.
    public Pipe? CreateBranchPipe(
        Document document,
        FamilyInstance sprinkler,
        Pipe pipe)
    {
        var sprinklerConnector =
            _connectorService.GetUnconnectedConnector(sprinkler);

        if (sprinklerConnector is null)
            return null;

        var endPoint = GetEndPoint(pipe, sprinklerConnector.Origin);

        if (endPoint is null)
            return null;

        return CreateBranchPipe(document, sprinkler, pipe, endPoint);
    }

    public PipeType? GetPipeType(
        Document document,
        Pipe pipe)
    {
        return pipe.PipeType;
    }

    public PipingSystemType? GetpipeSystemType(
        Document document,
        Pipe pipe)
    {
        var pipeSystemTypeId =
            pipe.MEPSystem.GetTypeId();

        var pipeSystemType =
            document.GetElement(pipeSystemTypeId);

        return pipeSystemType as PipingSystemType;
    }

    public Level? GetLevel(
        Document document,
        Pipe pipe)
    {
        return pipe.ReferenceLevel;
    }

    public XYZ? GetEndPoint(
        Pipe pipe,
        XYZ startPoint)
    {
        var locationCurve =
            pipe.Location as LocationCurve;

        if (locationCurve is null)
            return null;

        var projection =
            locationCurve.Curve.Project(startPoint);

        if (projection is null)
            return null;

        return projection.XYZPoint;
    }
}