using System.Windows;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sprinklers_Connectors.Handlers;

namespace Sprinklers_Connectors.ViewModels;

public partial class SprinklerAutoConnectViewModel : ObservableObject
{
    private readonly SprinklerAutoConnectHandler _handler;
    private readonly ExternalEvent _externalEvent;
    private Dispatcher? _uiDispatcher;
    private string _statusMessage = "Ready";

    public SprinklerAutoConnectViewModel(UIDocument uiDocument)
    {
        UiDocument = uiDocument;

        _handler = new SprinklerAutoConnectHandler
        {
            ViewModel = this,
            UiDocument = uiDocument
        };

        _externalEvent = ExternalEvent.Create(_handler);
    }

    public UIDocument UiDocument { get; }

    public List<ElementId> UnconnectedIds { get; private set; } = [];
    public List<ElementId> FailedIds { get; private set; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    [ObservableProperty]
    private int totalSprinklers;

    [ObservableProperty]
    private int unconnectedSprinklers;

    [ObservableProperty]
    private int totalPipes;

    [ObservableProperty]
    private int connected;

    [ObservableProperty]
    private int failed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanModelCommand))]
    [NotifyCanExecuteChangedFor(nameof(AutoConnectCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectUnconnectedCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectFailedCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReScanCommand))]
    private bool isBusy;

    public void AttachUiDispatcher(Dispatcher dispatcher) =>
        _uiDispatcher = dispatcher;

    public void OnViewLoaded() =>
        ScanModel();

    [RelayCommand(CanExecute = nameof(CanRunRevitAction))]
    private void ScanModel()
    {
        IsBusy = true;
        StatusMessage = "Scanning model...";
        Raise(SprinklerAutoConnectAction.ScanModel);
    }

    [RelayCommand(CanExecute = nameof(CanAutoConnect))]
    private void AutoConnect()
    {
        IsBusy = true;
        StatusMessage = "Connecting sprinklers...";
        Raise(SprinklerAutoConnectAction.AutoConnect);
    }

    [RelayCommand(CanExecute = nameof(CanSelectUnconnected))]
    private void SelectUnconnected() =>
        Raise(SprinklerAutoConnectAction.SelectUnconnected);

    [RelayCommand(CanExecute = nameof(CanSelectFailed))]
    private void SelectFailed() =>
        Raise(SprinklerAutoConnectAction.SelectFailed);

    [RelayCommand(CanExecute = nameof(CanRunRevitAction))]
    private void ReScan()
    {
        IsBusy = true;
        StatusMessage = "Re-scanning model...";
        Raise(SprinklerAutoConnectAction.ReScan);
    }

    private bool CanRunRevitAction() => !IsBusy;
    private bool CanAutoConnect() => !IsBusy && UnconnectedSprinklers > 0;
    private bool CanSelectUnconnected() => !IsBusy && UnconnectedIds.Count > 0;
    private bool CanSelectFailed() => !IsBusy && FailedIds.Count > 0;

    public void UpdateScanResults(
        int totalSprinklers,
        int unconnectedSprinklers,
        int totalPipes,
        List<ElementId> unconnectedIds,
        string status)
    {
        RunOnUiThread(() =>
        {
            TotalSprinklers = totalSprinklers;
            UnconnectedSprinklers = unconnectedSprinklers;
            TotalPipes = totalPipes;
            UnconnectedIds = unconnectedIds;
            StatusMessage = status;
            IsBusy = false;
        });
    }

    public void UpdateAfterConnect(
        int totalSprinklers,
        int unconnectedSprinklers,
        int totalPipes,
        int connected,
        int failed,
        List<ElementId> unconnectedIds,
        List<ElementId> failedIds,
        string status)
    {
        RunOnUiThread(() =>
        {
            TotalSprinklers = totalSprinklers;
            UnconnectedSprinklers = unconnectedSprinklers;
            TotalPipes = totalPipes;
            Connected = connected;
            Failed = failed;
            UnconnectedIds = unconnectedIds;
            FailedIds = failedIds;
            StatusMessage = status;
            IsBusy = false;
        });
    }

    public void SetStatus(string message) =>
        RunOnUiThread(() =>
        {
            StatusMessage = message;
            IsBusy = false;
        });

    private void Raise(SprinklerAutoConnectAction action)
    {
        _handler.Action = action;
        _externalEvent.Raise();
    }

    private void RunOnUiThread(Action action)
    {
        var dispatcher = _uiDispatcher ?? Dispatcher.CurrentDispatcher;

        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }
}
