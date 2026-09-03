using System.Windows;
using ClipMaster.Presentation.Views;

namespace ClipMaster.Presentation;

public partial class App : System.Windows.Application
{
    public App()
    {
        InitializeComponent();
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"UI Error:\n{e.Exception}\n\n{e.Exception.InnerException?.Message}", "ClipMaster Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        MessageBox.Show($"Fatal Error:\n{ex}", "ClipMaster Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        MessageBox.Show($"Task Error:\n{e.Exception}", "ClipMaster Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.SetObserved();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
