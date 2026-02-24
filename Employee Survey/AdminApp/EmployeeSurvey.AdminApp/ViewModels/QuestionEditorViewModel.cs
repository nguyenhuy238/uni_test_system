using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class QuestionEditorViewModel : ObservableObject
{
    private readonly ApiService _apiService;
    private readonly DraftService _draftService;
    private readonly string? _existingId;

    [ObservableProperty]
    private string title = "Create New Question";

    [ObservableProperty]
    private string content = string.Empty;

    [ObservableProperty]
    private QType type = QType.MCQ;

    [ObservableProperty]
    private string skill = "C#";

    [ObservableProperty]
    private string difficulty = "Junior";

    [ObservableProperty]
    private ObservableCollection<string> options = new() { "Option A", "Option B", "Option C", "Option D" };

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public QuestionEditorViewModel(ApiService apiService)
    {
        _apiService = apiService;
    }

    public void Initialize(Question existing)
    {
        _existingId = existing.Id;
        Content = existing.Content;
        Type = existing.Type;
        Skill = existing.Skill;
        Difficulty = existing.Difficulty;
        Title = "Edit Question";
    }

    [RelayCommand]
    private void GoBack()
    {
        var mainVm = App.Current.Services.GetRequiredService<MainViewModel>();
        mainVm.NavigateToQuestions();
    }

    public QuestionEditorViewModel(ApiService apiService, Question existing) : this(apiService)
    {
        _existingId = existing.Id;
        Content = existing.Content;
        Type = existing.Type;
        Skill = existing.Skill;
        Difficulty = existing.Difficulty;
        Title = "Edit Question";
    }

    [RelayCommand]
    private void SaveDraft()
    {
        var question = new Question
        {
            Id = _existingId ?? Guid.NewGuid().ToString("N"),
            Content = Content,
            Type = Type,
            Skill = Skill,
            Difficulty = Difficulty
        };

        var draft = new Draft
        {
            Type = "Question",
            Title = string.IsNullOrWhiteSpace(Content) ? "Untitled Question" : (Content.Length > 30 ? Content.Substring(0, 30) + "..." : Content),
            ContentJson = Newtonsoft.Json.JsonConvert.SerializeObject(question)
        };

        _draftService.SaveDraft(draft);
        StatusMessage = "Draft saved locally!";
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            StatusMessage = "Content is required";
            return;
        }

        try
        {
            StatusMessage = "Saving...";
            
            var request = new
            {
                Content = Content,
                Type = (int)Type,
                Skill = Skill,
                Difficulty = Difficulty,
                Options = Options.Select(o => new { Content = o, IsCorrect = false }).ToList() // Simplified for now
            };

            if (_existingId == null)
            {
                await _apiService.PostAsync("api/admin/questions", request);
                StatusMessage = "Question created successfully";
            }
            else
            {
                await _apiService.PutAsync("api/admin/questions", _existingId, request);
                StatusMessage = "Question updated successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
    }
}
