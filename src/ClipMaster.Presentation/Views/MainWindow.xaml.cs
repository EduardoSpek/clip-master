using System.Windows;
using System.Windows.Input;
using ClipMaster.Presentation.ViewModels;

namespace ClipMaster.Presentation.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Init Error:\n{ex}", "ClipMaster", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _viewModel?.Dispose();
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
