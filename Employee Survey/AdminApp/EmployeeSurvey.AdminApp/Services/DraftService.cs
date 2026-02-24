using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmployeeSurvey.AdminApp.Models;
using Newtonsoft.Json;

namespace EmployeeSurvey.AdminApp.Services;

public class DraftService
{
    private readonly string _draftPath;

    public DraftService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _draftPath = Path.Combine(appData, "EmployeeSurveyAdmin", "Drafts.json");
        
        var dir = Path.GetDirectoryName(_draftPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
    }

    public List<Draft> GetAllDrafts()
    {
        if (!File.Exists(_draftPath)) return new List<Draft>();
        var json = File.ReadAllText(_draftPath);
        return JsonConvert.DeserializeObject<List<Draft>>(json) ?? new List<Draft>();
    }

    public void SaveDraft(Draft draft)
    {
        var drafts = GetAllDrafts();
        var existing = drafts.FirstOrDefault(d => d.Id == draft.Id);
        if (existing != null)
        {
            drafts.Remove(existing);
        }
        drafts.Add(draft);
        SaveToFile(drafts);
    }

    public void DeleteDraft(string id)
    {
        var drafts = GetAllDrafts();
        var draft = drafts.FirstOrDefault(d => d.Id == id);
        if (draft != null)
        {
            drafts.Remove(draft);
            SaveToFile(drafts);
        }
    }

    private void SaveToFile(List<Draft> drafts)
    {
        var json = JsonConvert.SerializeObject(drafts, Formatting.Indented);
        File.WriteAllText(_draftPath, json);
    }
}
