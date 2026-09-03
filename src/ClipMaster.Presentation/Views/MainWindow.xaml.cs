using System.Windows;
using System.Windows.Input;
using ClipMaster.Presentation.ViewModels;

namespace ClipMaster.Presentation.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsDarkMode)
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

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.Dispose();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            ShowInTaskbar = false;
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
