using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UniTestSystem.AdminApp.ViewModels;

namespace UniTestSystem.AdminApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
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
        var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
        content.BeginAnimation(OpacityProperty, animation);
    }
}
