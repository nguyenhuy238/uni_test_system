using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ApiService _api;

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    public LoginViewModel(ApiService api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        var (ok, token, user) = await _api.LoginAsync(Email, Password);
        if (ok)
        {
            // Successfully logged in
            // Navigate to Main window
            OnLoginSuccess?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            ErrorMessage = "Đăng nhập thất bại. Kiểm tra lại email/mật khẩu.";
        }
    }

    public event EventHandler? OnLoginSuccess;
}
