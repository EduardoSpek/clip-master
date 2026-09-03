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

        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow && mainWindow.DataContext is MainViewModel vm && vm.IsDarkMode)
        {
            var darkTheme = new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
            };
            Resources.MergedDictionaries.Add(darkTheme);
        }
        else
        {
            var lightTheme = new System.Windows.ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Themes/LightTheme.xaml")
            };
            Resources.MergedDictionaries.Add(lightTheme);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
