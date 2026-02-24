using System.Configuration;
using System.Data;
using System.Windows;
using EmployeeSurvey.AdminApp.Services;
using EmployeeSurvey.AdminApp.ViewModels;
using EmployeeSurvey.AdminApp.Views;

namespace EmployeeSurvey.AdminApp;

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

        // Show login window first
        var loginWindow = new LoginWindow(_apiService);
        if (loginWindow.ShowDialog() == true)
        {
            // Login successful, show main window
            var viewModel = new MainViewModel(_apiService);
            var mainWindow = new MainWindow(viewModel);
            mainWindow.Show();
        }
        else
        {
            // Login failed or cancelled, shutdown
            Shutdown();
        }
    }
}

