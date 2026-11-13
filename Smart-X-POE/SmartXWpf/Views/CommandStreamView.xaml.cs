using System.Windows;
using System.Windows.Controls;
using SmartXWpf.ViewModels;

namespace SmartXWpf.Views;

public partial class CommandStreamView : Page
{
    private readonly CommandStreamViewModel _viewModel;

    public CommandStreamView(CommandStreamViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void PumpOn_Click(object sender, RoutedEventArgs e) => await _viewModel.PumpOnAsync();

    private async void PumpOff_Click(object sender, RoutedEventArgs e) => await _viewModel.PumpOffAsync();

    private async void RelayToggle_Click(object sender, RoutedEventArgs e) => await _viewModel.RelayToggleAsync();

    private async void Undo_Click(object sender, RoutedEventArgs e) => await _viewModel.UndoAsync();

    private async void Search_Click(object sender, RoutedEventArgs e) => await _viewModel.SearchAsync();
}
