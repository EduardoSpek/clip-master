using System.Windows;
using ClipMaster.Presentation.ViewModels;

namespace ClipMaster.Presentation.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(ClipMaster.Domain.Interfaces.ISettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel(settingsService);
        DataContext = _viewModel;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
