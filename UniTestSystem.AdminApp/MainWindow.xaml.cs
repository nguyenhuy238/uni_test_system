using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UniTestSystem.AdminApp.Services;
using UniTestSystem.AdminApp.ViewModels;
using UniTestSystem.AdminApp.Views;

namespace UniTestSystem.AdminApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ApiService _apiService;
    private bool _isSidebarCollapsed;

    public MainWindow(MainViewModel viewModel, ApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _apiService = apiService;
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
        ApplyTheme(_viewModel.IsDarkTheme);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDarkTheme))
        {
            ApplyTheme(_viewModel.IsDarkTheme);
        }
    }

    private void ApplyTheme(bool isDark)
    {
        if (Resources["AppBackgroundBrush"] is not SolidColorBrush appBg ||
            Resources["AppForegroundBrush"] is not SolidColorBrush appFg ||
            Resources["PanelBackgroundBrush"] is not SolidColorBrush panelBg)
        {
            return;
        }

        if (isDark)
        {
            appBg.Color = Color.FromRgb(17, 24, 39);
            appFg.Color = Color.FromRgb(229, 231, 235);
            panelBg.Color = Color.FromRgb(31, 41, 55);
        }
        else
        {
            appBg.Color = Color.FromRgb(245, 248, 252);
            appFg.Color = Color.FromRgb(31, 41, 55);
            panelBg.Color = Color.FromRgb(255, 255, 255);
        }
    }

    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, MainTabControl))
        {
            return;
        }

        if (MainTabControl.SelectedContent is not UIElement content)
        {
            return;
        }

        content.Opacity = 0;
        var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        content.BeginAnimation(OpacityProperty, animation);
    }

    private void SidebarToggle_Click(object sender, RoutedEventArgs e)
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        SidebarColumn.Width = _isSidebarCollapsed ? new GridLength(76) : new GridLength(220);
    }

    private void NavigateTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is null)
        {
            return;
        }

        if (int.TryParse(button.Tag.ToString(), out var index) && index >= 0 && index < MainTabControl.Items.Count)
        {
            MainTabControl.SelectedIndex = index;
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Bạn có chắc muốn đăng xuất?",
            "Xác nhận đăng xuất",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        Hide();

        try
        {
            await _apiService.LogoutAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể đăng xuất qua server: {ex.Message}\nỨng dụng sẽ vẫn xóa phiên cục bộ.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        var loginWindow = new LoginWindow(_apiService);
        var loginResult = loginWindow.ShowDialog() == true;

        if (!loginResult)
        {
            Application.Current.Shutdown();
            return;
        }

        var nextViewModel = new MainViewModel(_apiService);
        var nextWindow = new MainWindow(nextViewModel, _apiService);

        var previousShutdownMode = Application.Current.ShutdownMode;
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Application.Current.MainWindow = nextWindow;
        nextWindow.Show();
        Close();

        Application.Current.ShutdownMode = previousShutdownMode;
    }
}
