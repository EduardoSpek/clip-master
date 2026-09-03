using System.Windows;
using ClipMaster.Presentation.Views;

namespace ClipMaster.Presentation;

public partial class App : System.Windows.Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
