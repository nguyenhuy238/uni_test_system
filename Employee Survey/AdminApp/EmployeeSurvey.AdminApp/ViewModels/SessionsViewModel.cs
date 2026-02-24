using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class SessionsViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<Session> sessions = new();

    [ObservableProperty]
    private Session? selectedSession;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public SessionsViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadSessions();
    }

    [RelayCommand]
    private async Task LoadSessions()
    {
        try
        {
            StatusMessage = "Refreshing sessions...";
            var list = await _apiService.GetAsync<Session>("api/admin/sessions");
            Sessions = new ObservableCollection<Session>(list);
            StatusMessage = $"Monitoring {Sessions.Count} sessions.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reload failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TerminateSession(Session? s)
    {
        if (s == null) return;

        var result = System.Windows.MessageBox.Show($"Are you sure you want to terminate and delete this session?\n\nUser: {s.UserName}\nTest: {s.TestTitle}", "Confirm Termination", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            StatusMessage = "Terminating...";
            await _apiService.DeleteAsync("api/admin/sessions", s.Id);
            Sessions.Remove(s);
            StatusMessage = "Session terminated successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Termination failed: {ex.Message}";
        }
    }
}
