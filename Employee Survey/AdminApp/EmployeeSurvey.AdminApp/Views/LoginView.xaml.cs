using System.Windows;
using System.Windows.Controls;
using EmployeeSurvey.AdminApp.ViewModels;

namespace EmployeeSurvey.AdminApp.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
        this.Loaded += (s, e) =>
        {
            if (DataContext is LoginViewModel vm)
            {
                PasswordBox.PasswordChanged += (ps, pe) => vm.Password = PasswordBox.Password;
            }
        };
    }
}
