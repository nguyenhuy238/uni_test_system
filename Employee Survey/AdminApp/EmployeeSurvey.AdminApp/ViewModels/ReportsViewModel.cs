using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;
using Microsoft.Win32;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private ObservableCollection<TestResult> results = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public ReportsViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadResults();
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            StatusMessage = "Loading results...";
            var list = await _apiService.GetAsync<TestResult>("api/admin/results");
            Results = new ObservableCollection<TestResult>(list);
            StatusMessage = $"Found {Results.Count} total submissions.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await DownloadFile("api/admin/results/export/excel", "Excel Reports|*.xlsx", "Results.xlsx");
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        await DownloadFile("api/admin/results/export/pdf", "PDF Reports|*.pdf", "Results.pdf");
    }

    private async Task DownloadFile(string endpoint, string filter, string defaultName)
    {
        try
        {
            StatusMessage = "Generating report...";
            var data = await _apiService.DownloadFileAsync(endpoint);
            
            var saveFileDialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllBytes(saveFileDialog.FileName, data);
                StatusMessage = "Report saved successfully!";
            }
            else
            {
                StatusMessage = string.Empty;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }
}
