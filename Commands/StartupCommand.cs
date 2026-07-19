using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Nice3point.Revit.Toolkit.External;
using Sprinklers_Connectors.ViewModels;
using Sprinklers_Connectors.Views;

namespace Sprinklers_Connectors.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        var viewModel = new SprinklerAutoConnectViewModel(Application.ActiveUIDocument);
        var window = new SprinklerAutoConnectView(viewModel);
        window.Show();
    }
}
