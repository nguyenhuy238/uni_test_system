using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class TestEditorViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private string? _existingId;

    [ObservableProperty]
    private string title = "Create New Test";

    [ObservableProperty]
    private string testTitle = string.Empty;

    [ObservableProperty]
    private TestType type = TestType.Test;

    [ObservableProperty]
    private int durationMinutes = 30;

    [ObservableProperty]
    private bool isPublished = false;

    [ObservableProperty]
    private ObservableCollection<Question> availableQuestions = new();

    [ObservableProperty]
    private ObservableCollection<Question> selectedQuestions = new();

    [ObservableProperty]
    private Question? selectedAvailable;

    [ObservableProperty]
    private Question? selectedSelected;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public TestEditorViewModel(ApiService apiService, DraftService draftService) // Modified constructor
    {
        _apiService = apiService;
        _draftService = draftService; // Added
        _ = LoadAvailableQuestions();
    }

    private async Task LoadAvailableQuestions()
    {
        try
        {
            var list = await _apiService.GetAsync<Question>("api/admin/questions");
            AvailableQuestions = new ObservableCollection<Question>(list);
        }
        catch { /* handle error */ }
    }

    public void Initialize(Test existing)
    {
        _existingId = existing.Id;
        TestTitle = existing.Title;
        Type = existing.Type;
        DurationMinutes = existing.DurationMinutes;
        IsPublished = existing.IsPublished;
        Title = "Edit Test";
        
        // In a real app, we'd load the selected questions for this test here
    }

    [RelayCommand]
    private void AddQuestion(Question? q)
    {
        if (q != null && !SelectedQuestions.Contains(q))
            SelectedQuestions.Add(q);
    }

    [RelayCommand]
    private void RemoveQuestion(Question? q)
    {
        if (q != null)
            SelectedQuestions.Remove(q);
    }

    [RelayCommand]
    private void SaveDraft()
    {
        var test = new Test
        {
            Id = _existingId ?? Guid.NewGuid().ToString("N"),
            Title = TestTitle,
            Type = Type,
            DurationMinutes = DurationMinutes,
            IsPublished = IsPublished
        };

        var draft = new Draft
        {
            Type = "Test",
            Title = string.IsNullOrWhiteSpace(TestTitle) ? "Untitled Test" : TestTitle,
            ContentJson = Newtonsoft.Json.JsonConvert.SerializeObject(test)
        };

        _draftService.SaveDraft(draft);
        StatusMessage = "Draft saved locally!";
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(TestTitle))
        {
            StatusMessage = "Title is required";
            return;
        }

        try
        {
            StatusMessage = "Saving...";
            var request = new
            {
                Title = TestTitle,
                Type = (int)Type,
                DurationMinutes = DurationMinutes,
                IsPublished = IsPublished,
                QuestionIds = SelectedQuestions.Select(q => q.Id).ToList()
            };

            if (_existingId == null)
            {
                await _apiService.PostAsync("api/admin/tests", request);
                StatusMessage = "Test created successfully";
            }
            else
            {
                await _apiService.PutAsync("api/admin/tests", _existingId, request);
                StatusMessage = "Test updated successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        App.Current.Services.GetRequiredService<MainViewModel>().NavigateToTests();
    }
}
