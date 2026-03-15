using System.IO;
using System.Text;
using System.Windows;
using NativeHymnsApp.Services;
using NativeHymnsApp.ViewModels;

namespace NativeHymnsApp;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var catalogService = new SongCatalogService(Path.Combine(AppContext.BaseDirectory, "Data", "Structured"));
        var stateService = new AppStateService();
        MainWindowViewModel? viewModel = null;

        try
        {
            viewModel = new MainWindowViewModel(catalogService, stateService);
        }
        catch (Exception exception)
        {
            System.Windows.MessageBox.Show(
                $"The hymn app could not start.\n\n{exception.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
            return;
        }

        if (e.Args.Any(arg => string.Equals(arg, "--smoke-test", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var summaryPath = Path.Combine(AppContext.BaseDirectory, "smoke-test.txt");
            File.WriteAllText(summaryPath, viewModel.RunSmokeTest(), Encoding.UTF8);
            Shutdown(0);
            return;
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();
    }
}
