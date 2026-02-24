using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class AutoGenerateViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private AutoTestOptions options = new();

    [ObservableProperty]
    private ObservableCollection<string> departments = new();

    [ObservableProperty]
    private ObservableCollection<PersonalizedTestResult> results = new();

    [ObservableProperty]
    private bool isPreviewMode = false;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public AutoGenerateViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadDepartments();
    }

    private async Task LoadDepartments()
    {
        try
        {
            var list = await _apiService.GetAsync<string>("api/admin/autotests/departments");
            Departments = new ObservableCollection<string>(list);
            if (Departments.Any()) Options.Department = Departments.First();
        }
        catch { /* handle error */ }
    }

    [RelayCommand]
    private async Task GeneratePreview()
    {
        try
        {
            StatusMessage = "Generating tests...";
            var list = await _apiService.PostAsync<List<PersonalizedTestResult>>("api/admin/autotests/generate", Options);
            Results = new ObservableCollection<PersonalizedTestResult>(list);
            IsPreviewMode = true;
            StatusMessage = $"Generated {Results.Count} personalized tests.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Generation failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AssignAll()
    {
        try
        {
            StatusMessage = "Assigning tests...";
            var req = new AssignBatchRequest
            {
                TestIds = Results.Select(r => r.Test.Id).ToList(),
                UserIds = Results.Select(r => r.User.Id).ToList(),
                StartAtUtc = Options.StartAtUtc,
                EndAtUtc = Options.EndAtUtc
            };
            await _apiService.PostAsync("api/admin/autotests/assign-batch", req);
            StatusMessage = "All tests assigned successfully!";
            // Optional: Go back to list
        }
        catch (Exception ex)
        {
            StatusMessage = $"Assignment failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void BackToOptions() => IsPreviewMode = false;

    [RelayCommand]
    private void Cancel() => App.Current.Services.GetRequiredService<MainViewModel>().NavigateToTests();
}
