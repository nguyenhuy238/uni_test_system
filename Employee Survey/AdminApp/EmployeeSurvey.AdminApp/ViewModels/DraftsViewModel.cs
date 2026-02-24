using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EmployeeSurvey.AdminApp.Models;
using EmployeeSurvey.AdminApp.Services;

namespace EmployeeSurvey.AdminApp.ViewModels;

public partial class DraftsViewModel : ObservableObject
{
    private readonly DraftService _draftService;

    [ObservableProperty]
    private ObservableCollection<Draft> drafts = new();

    [ObservableProperty]
    private Draft? selectedDraft;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public DraftsViewModel(DraftService draftService)
    {
        _draftService = draftService;
        _ = LoadDrafts();
    }

    [RelayCommand]
    private async Task LoadData()
    {
        StatusMessage = "Loading local drafts...";
        var list = _draftService.GetAllDrafts();
        Drafts = new ObservableCollection<Draft>(list.OrderByDescending(d => d.CreatedAt));
        StatusMessage = $"You have {Drafts.Count} offline drafts.";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void DeleteDraft(Draft? d)
    {
        if (d == null) return;
        _draftService.DeleteDraft(d.Id);
        Drafts.Remove(d);
        StatusMessage = "Draft deleted.";
    }

    [RelayCommand]
    private void OpenDraft(Draft? d)
    {
        if (d == null) return;
        
        var mainVm = App.Current.Services.GetRequiredService<MainViewModel>();
        if (d.Type == "Question")
        {
            var question = Newtonsoft.Json.JsonConvert.DeserializeObject<Question>(d.ContentJson);
            if (question != null) mainVm.NavigateToQuestionEditor(question);
        }
        else if (d.Type == "Test")
        {
            var test = Newtonsoft.Json.JsonConvert.DeserializeObject<Test>(d.ContentJson);
            if (test != null) mainVm.NavigateToTestEditor(test);
        }
    }
}
