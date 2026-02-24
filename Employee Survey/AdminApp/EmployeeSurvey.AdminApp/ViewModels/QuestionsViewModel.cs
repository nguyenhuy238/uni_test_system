using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class QuestionsViewModel : ObservableObject
{
    private readonly ApiService _apiService;

    [ObservableProperty]
    private string title = "Question Bank";

    [ObservableProperty]
    private ObservableCollection<Question> questions = new();

    [ObservableProperty]
    private Question? selectedQuestion;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public QuestionsViewModel(ApiService apiService)
    {
        _apiService = apiService;
        _ = LoadData();
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            StatusMessage = "Refreshing question bank...";
            var list = await _apiService.GetAsync<Question>("api/admin/questions");
            Questions = new ObservableCollection<Question>(list);
            StatusMessage = $"Loaded {Questions.Count} questions.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteQuestion(Question? q)
    {
        if (q == null) return;
        
        var result = System.Windows.MessageBox.Show($"Are you sure you want to delete this question?\n\n{q.Content}", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            StatusMessage = "Deleting...";
            await _apiService.DeleteAsync("api/admin/questions", q.Id);
            Questions.Remove(q);
            StatusMessage = "Question deleted successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CreateNewQuestion()
    {
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        mainVm.NavigateToQuestionEditor();
    }

    [RelayCommand]
    private void EditQuestion(Question? q)
    {
        if (q == null) return;
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        mainVm.NavigateToQuestionEditor(q);
    }
}
