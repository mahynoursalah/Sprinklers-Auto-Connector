using System.Windows;
using Sprinklers_Connectors.ViewModels;

namespace Sprinklers_Connectors.Views;

public partial class SprinklerAutoConnectView : Window
{
    public SprinklerAutoConnectView(SprinklerAutoConnectViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.AttachUiDispatcher(Dispatcher);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SprinklerAutoConnectViewModel viewModel)
            viewModel.OnViewLoaded();
    }
}
