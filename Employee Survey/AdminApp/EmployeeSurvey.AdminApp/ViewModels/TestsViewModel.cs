using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class TestsViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string title = "Test Management";

    [ObservableProperty]
    private ObservableCollection<Test> tests = new();

    [ObservableProperty]
    private Test? selectedTest;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public TestsViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadTests();
    }

    [RelayCommand]
    private void NavigateToAutoGenerate()
    {
        var mainVm = App.Current.Services.GetRequiredService<MainViewModel>();
        mainVm.NavigateToAutoGenerate();
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            StatusMessage = "Loading tests...";
            var list = await _apiService.GetAsync<Test>("api/admin/tests");
            Tests = new ObservableCollection<Test>(list);
            StatusMessage = $"Loaded {Tests.Count} tests.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteTest(Test? t)
    {
        if (t == null) return;

        var result = System.Windows.MessageBox.Show($"Are you sure you want to delete this test?\n\n{t.Title}", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            StatusMessage = "Deleting...";
            await _apiService.DeleteAsync("api/admin/tests", t.Id);
            Tests.Remove(t);
            StatusMessage = "Test deleted successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CreateNewTest()
    {
        var mainVm = App.Current.Services.GetRequiredService<MainViewModel>();
        mainVm.NavigateToTestEditor();
    }

    [RelayCommand]
    private void EditTest(Test? t)
    {
        if (t == null) return;
        var mainVm = App.Current.Services.GetRequiredService<MainViewModel>();
        mainVm.NavigateToTestEditor(t);
    }
}
