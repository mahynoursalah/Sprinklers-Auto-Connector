using Nice3point.Revit.Toolkit.External;
using Sprinklers_Connectors.Commands;

namespace Sprinklers_Connectors;

[UsedImplicitly]
public class Application : AsyncExternalApplication
{
    public override async Task OnStartupAsync()
    {
        await Host.StartAsync();
        CreateRibbon();
    }

    public override async Task OnShutdownAsync()
    {
        await Host.StopAsync();
    }

    private void CreateRibbon()
    {
        var panel = Application.CreatePanel("Commands", "Sprinklers Connectors");

        panel.AddPushButton<StartupCommand>("Execute")
            .SetImage("/Sprinklers Connectors;component/Resources/Icons/RibbonIcon16.png")
            .SetLargeImage("/Sprinklers Connectors;component/Resources/Icons/RibbonIcon32.png");
    }
}