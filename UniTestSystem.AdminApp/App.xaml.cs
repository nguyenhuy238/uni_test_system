using System.Configuration;
using System.Data;
using System.Windows;
using UniTestSystem.AdminApp.Services;
using UniTestSystem.AdminApp.ViewModels;
using UniTestSystem.AdminApp.Views;

namespace UniTestSystem.AdminApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ApiService _apiService;

    public App()
    {
        _apiService = new ApiService("https://localhost:7158"); // Updated to match API launchSettings.json
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Login first (or restore prior session)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var hasSession = _apiService.TryRestoreSessionAsync().GetAwaiter().GetResult();
        var loginSucceeded = hasSession;

        if (!hasSession)
        {
            var loginWindow = new LoginWindow(_apiService);
            loginSucceeded = loginWindow.ShowDialog() == true;
        }

        if (loginSucceeded)
        {
            var viewModel = new MainViewModel(_apiService);
            var mainWindow = new MainWindow(viewModel);
            
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            Application.Current.MainWindow = mainWindow;
            
            mainWindow.Show();
        }
        else
        {
            Shutdown();
        }
    }
}

