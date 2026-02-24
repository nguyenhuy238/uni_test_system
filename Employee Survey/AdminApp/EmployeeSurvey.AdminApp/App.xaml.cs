using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using EmployeeSurvey.AdminApp.ViewModels;
using EmployeeSurvey.AdminApp.Views;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    public IServiceProvider Services => _serviceProvider;
    public static new App Current => (App)Application.Current;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ApiService>();
        services.AddSingleton<DraftService>();
        services.AddTransient<LoginViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SessionsViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<DraftsViewModel>();
        services.AddTransient<QuestionsViewModel>();
        services.AddTransient<QuestionEditorViewModel>();
        services.AddTransient<TestsViewModel>();
        services.AddTransient<TestEditorViewModel>();
        services.AddTransient<AutoGenerateViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        ShowLogin(mainWindow);
        mainWindow.Show();
    }

    private void ShowLogin(MainWindow window)
    {
        var vm = _serviceProvider.GetRequiredService<LoginViewModel>();
        var view = new LoginView { DataContext = vm };
        
        vm.OnLoginSuccess += (s, e) => ShowMain(window);
        window.MainGrid.Children.Clear();
        window.MainGrid.Children.Add(view);
    }

    private void ShowMain(MainWindow window)
    {
        var vm = _serviceProvider.GetRequiredService<MainViewModel>();
        var view = new AdminMainView { DataContext = vm };
        
        window.MainGrid.Children.Clear();
        window.MainGrid.Children.Add(view);
    }
}
