using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private ObservableObject? currentViewModel;

    [ObservableProperty]
    private string statusMessage = "Ready";

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // Default to Dashboard
        NavigateToDashboard();
    }

    [RelayCommand]
    private void NavigateToDashboard() => CurrentViewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();

    [RelayCommand]
    private void NavigateToSessions() => CurrentViewModel = _serviceProvider.GetRequiredService<SessionsViewModel>();

    [RelayCommand]
    private void NavigateToReports() => CurrentViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();

    [RelayCommand]
    private void NavigateToDrafts() => CurrentViewModel = _serviceProvider.GetRequiredService<DraftsViewModel>();

    [RelayCommand]
    private void NavigateToQuestions() => CurrentViewModel = _serviceProvider.GetRequiredService<QuestionsViewModel>();

    [RelayCommand]
    private void NavigateToTests() => CurrentViewModel = _serviceProvider.GetRequiredService<TestsViewModel>();

    [RelayCommand]
    private void NavigateToUsers() => CurrentViewModel = _serviceProvider.GetRequiredService<UsersViewModel>();

    public void NavigateToQuestionEditor(Question? existing = null)
    {
        var vm = _serviceProvider.GetRequiredService<QuestionEditorViewModel>();
        if (existing != null) vm.Initialize(existing);
        CurrentViewModel = vm;
    }

    public void NavigateToTestEditor(Test? existing = null)
    {
        var vm = _serviceProvider.GetRequiredService<TestEditorViewModel>();
        if (existing != null) vm.Initialize(existing);
        CurrentViewModel = vm;
    }

    public void NavigateToAutoGenerate()
    {
        CurrentViewModel = _serviceProvider.GetRequiredService<AutoGenerateViewModel>();
    }
}
