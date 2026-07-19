using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sprinklers_Connectors.Models;
using Sprinklers_Connectors.Services;
using Sprinklers_Connectors.ViewModels;

namespace Sprinklers_Connectors.Handlers;

public enum SprinklerAutoConnectAction
{
    ScanModel,
    AutoConnect,
    SelectUnconnected,
    SelectFailed,
    ReScan
}

public class SprinklerAutoConnectHandler : IExternalEventHandler
{
    private readonly SprinklerService _sprinklerService = new();
    private readonly PipeSearchServices _pipeSearchService = new();
    private readonly SprinklerConnectionService _connectionService = new();

    public SprinklerAutoConnectViewModel? ViewModel { get; set; }
    public UIDocument? UiDocument { get; set; }
    public SprinklerAutoConnectAction Action { get; set; }

    public void Execute(UIApplication app)
    {
        if (UiDocument is null || ViewModel is null)
            return;

        var document = UiDocument.Document;

        switch (Action)
        {
            case SprinklerAutoConnectAction.ScanModel:
                ExecuteScan(document, isReScan: false);
                break;

            case SprinklerAutoConnectAction.ReScan:
                ExecuteScan(document, isReScan: true);
                break;

            case SprinklerAutoConnectAction.AutoConnect:
                ExecuteAutoConnect(document);
                break;

            case SprinklerAutoConnectAction.SelectUnconnected:
                UiDocument.Selection.SetElementIds(ViewModel.UnconnectedIds);
                ViewModel.SetStatus($"Selected {ViewModel.UnconnectedIds.Count} unconnected sprinklers.");
                break;

            case SprinklerAutoConnectAction.SelectFailed:
                UiDocument.Selection.SetElementIds(ViewModel.FailedIds);
                ViewModel.SetStatus($"Selected {ViewModel.FailedIds.Count} failed sprinklers.");
                break;
        }
    }

    private void ExecuteScan(Document document, bool isReScan)
    {
        var allSprinklers = _sprinklerService.GetSprinklers(document);
        var unconnected = _sprinklerService.GetUnconnectedSprinklers(document);
        var pipes = _pipeSearchService.GetPipes(document);

        ViewModel!.UpdateScanResults(
            totalSprinklers: allSprinklers.Count,
            unconnectedSprinklers: unconnected.Count,
            totalPipes: pipes.Count,
            unconnectedIds: unconnected.Select(s => s.Id).ToList(),
            status: isReScan
                ? $"Re-scan complete. {unconnected.Count} unconnected, {pipes.Count} fire protection pipes."
                : $"Scan complete. {unconnected.Count} unconnected, {pipes.Count} fire protection pipes.");
    }

    private void ExecuteAutoConnect(Document document)
    {
        var unconnected = _sprinklerService.GetUnconnectedSprinklers(document);

        if (!unconnected.Any())
        {
            ViewModel!.SetStatus("No unconnected sprinklers to connect.");
            return;
        }

        var pipes = _pipeSearchService.GetPipes(document);

        int connectedCount = 0;
        int failedCount = 0;

        var failedIds = new List<ElementId>();

        // NOTE: count each failure reason separately so we can know
        // exactly what failed for reporting/debugging
        var failureBreakdown =
            new Dictionary<ConnectionFailureReason, int>();

        foreach (var sprinkler in unconnected)
        {
            using var transaction =
                new Transaction(document, $"Connect {sprinkler.Id}");

            transaction.Start();

            SprinklerConnectionResult result;

            try
            {
                result = _connectionService.ConnectSprinklerDetailed(
                    document, sprinkler, pipes);
            }
            catch (Exception)
            {
                result = SprinklerConnectionResult.Fail(
                    ConnectionFailureReason.UnexpectedException);
            }

            if (result.Success)
            {
                transaction.Commit();
                connectedCount++;
            }
            else
            {
                transaction.RollBack();
                failedCount++;
                failedIds.Add(sprinkler.Id);

                failureBreakdown[result.Reason] =
                    failureBreakdown.GetValueOrDefault(result.Reason) + 1;
            }
        }

        var remaining = _sprinklerService.GetUnconnectedSprinklers(document);

        var all = _sprinklerService.GetSprinklers(document);

        var pipesAfter = _pipeSearchService.GetPipes(document);

        var status =
            $"Done. Connected: {connectedCount}, Failed: {failedCount}.";

        if (failureBreakdown.Any())
        {
            var breakdownText = string.Join(
                ", ",
                failureBreakdown.Select(kv => $"{kv.Key}: {kv.Value}"));

            status += $" ({breakdownText})";
        }

        ViewModel!.UpdateAfterConnect(
            totalSprinklers: all.Count,
            unconnectedSprinklers: remaining.Count,
            totalPipes: pipesAfter.Count,
            connected: connectedCount,
            failed: failedCount,
            unconnectedIds: remaining.Select(x => x.Id).ToList(),
            failedIds: failedIds,
            status: status);

        if (failedIds.Any())
            UiDocument!.Selection.SetElementIds(failedIds);
    }

    public string GetName() => nameof(SprinklerAutoConnectHandler);
}