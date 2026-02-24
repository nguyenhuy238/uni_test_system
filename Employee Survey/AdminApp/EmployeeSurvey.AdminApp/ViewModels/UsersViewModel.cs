using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string title = "User Management";

    [ObservableProperty]
    private ObservableCollection<User> users = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public UsersViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadUsers();
    }

    [RelayCommand]
    private async Task LoadUsers()
    {
        try
        {
            StatusMessage = "Loading users...";
            var list = await _apiService.GetAsync<User>("api/admin/users");
            Users = new ObservableCollection<User>(list);
            StatusMessage = $"Loaded {Users.Count} users";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}
