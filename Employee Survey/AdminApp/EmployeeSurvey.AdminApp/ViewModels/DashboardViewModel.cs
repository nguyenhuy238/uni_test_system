using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string title = "Dashboard Overview";

    [ObservableProperty]
    private DashboardSummary summary = new();

    public DashboardViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadSummary();
    }

    [RelayCommand]
    private async Task LoadSummary()
    {
        try
        {
            var data = await _apiService.GetByIdAsync<DashboardSummary>("api/admin/dashboard", "summary");
            if (data != null) Summary = data;
        }
        catch { /* handle error */ }
    }
}
