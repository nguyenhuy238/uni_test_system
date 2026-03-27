using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using UniTestSystem.AdminApp.Models;

namespace UniTestSystem.AdminApp.Views;

public partial class CreateQuestionWindow : Window
{
    public Question? CreatedQuestion { get; private set; }

    public CreateQuestionWindow(
        IEnumerable<string> typeOptions,
        IEnumerable<QuestionMetadataItem> skills,
        IEnumerable<QuestionMetadataItem> difficulties,
        IEnumerable<QuestionMetadataItem> subjects,
        IEnumerable<QuestionMetadataItem> questionBanks)
    {
        InitializeComponent();

        var types = typeOptions?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ?? new List<string>();
        if (types.Count == 0)
        {
            types.Add("MCQ");
        }

        TypeComboBox.ItemsSource = types;
        TypeComboBox.SelectedItem = types.FirstOrDefault(x => string.Equals(x, "MCQ", StringComparison.OrdinalIgnoreCase)) ?? types[0];

        SkillComboBox.ItemsSource = skills?.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList() ?? new List<QuestionMetadataItem>();
        DifficultyComboBox.ItemsSource = difficulties?.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList() ?? new List<QuestionMetadataItem>();
        SubjectComboBox.ItemsSource = subjects?.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList() ?? new List<QuestionMetadataItem>();
        QuestionBankComboBox.ItemsSource = questionBanks?.Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList() ?? new List<QuestionMetadataItem>();

        if (SkillComboBox.Items.Count > 0)
        {
            SkillComboBox.SelectedIndex = 0;
        }

        if (DifficultyComboBox.Items.Count > 0)
        {
            var easy = DifficultyComboBox.Items.Cast<QuestionMetadataItem>()
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains("easy", StringComparison.OrdinalIgnoreCase));
            DifficultyComboBox.SelectedItem = easy ?? DifficultyComboBox.Items[0];
        }

        if (SubjectComboBox.Items.Count > 0)
        {
            SubjectComboBox.SelectedIndex = 0;
        }

        if (QuestionBankComboBox.Items.Count > 0)
        {
            QuestionBankComboBox.SelectedIndex = 0;
        }

        OptionsTextBox.Text = "Option A" + Environment.NewLine + "Option B";
        CorrectKeysTextBox.Text = "Option A";
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var content = (ContentTextBox.Text ?? string.Empty).Trim();
        var type = (TypeComboBox.SelectedItem?.ToString() ?? "MCQ").Trim();
        var skillId = (SkillComboBox.SelectedValue?.ToString() ?? string.Empty).Trim();
        var difficultyId = (DifficultyComboBox.SelectedValue?.ToString() ?? string.Empty).Trim();
        var subjectId = (SubjectComboBox.SelectedValue?.ToString() ?? string.Empty).Trim();
        var questionBankId = (QuestionBankComboBox.SelectedValue?.ToString() ?? string.Empty).Trim();
        var mediaUrl = (MediaUrlTextBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            MessageBox.Show("Content is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            ContentTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(subjectId))
        {
            MessageBox.Show("Subject is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            SubjectComboBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(questionBankId))
        {
            MessageBox.Show("Question Bank is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            QuestionBankComboBox.Focus();
            return;
        }

        var optionLines = SplitLines(OptionsTextBox.Text);
        var correctKeys = SplitKeys(CorrectKeysTextBox.Text);
        var correctSet = correctKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var question = new Question
        {
            Content = content,
            Type = NormalizeQuestionType(type),
            Status = "Draft",
            SkillId = skillId,
            DifficultyLevelId = difficultyId,
            SubjectId = subjectId,
            QuestionBankId = questionBankId,
            MediaUrl = string.IsNullOrWhiteSpace(mediaUrl) ? null : mediaUrl,
            CorrectKeys = correctKeys,
            Options = new List<Option>(),
            MatchingPairs = new List<MatchPair>(),
            DragDrop = null
        };

        if (string.Equals(question.Type, "TrueFalse", StringComparison.OrdinalIgnoreCase))
        {
            var trueFalseOptions = new[] { "True", "False" };
            question.Options = trueFalseOptions
                .Select(value => new Option { Content = value, IsCorrect = correctSet.Contains(value) })
                .ToList();
        }
        else if (string.Equals(question.Type, "Matching", StringComparison.OrdinalIgnoreCase))
        {
            var pairs = optionLines
                .Select(ParseMatchingPair)
                .Where(pair => pair != null)
                .Cast<MatchPair>()
                .ToList();

            if (pairs.Count == 0)
            {
                MessageBox.Show("Matching requires at least one line in format left|right.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                OptionsTextBox.Focus();
                return;
            }

            question.MatchingPairs = pairs;
            question.Options = pairs
                .Select(pair => new Option
                {
                    Content = pair.Left + "|" + pair.Right,
                    IsCorrect = correctSet.Contains(pair.Right)
                })
                .ToList();
            question.CorrectKeys = pairs.Select(pair => pair.Right)
                .Where(value => correctSet.Contains(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else if (string.Equals(question.Type, "DragDrop", StringComparison.OrdinalIgnoreCase))
        {
            var tokens = optionLines.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var slots = correctKeys.Select((key, index) => new DragSlot { Name = (index + 1).ToString(), Answer = key }).ToList();

            question.DragDrop = new DragDropConfig
            {
                Tokens = tokens,
                Slots = slots
            };
            question.Options = tokens
                .Select(token => new Option { Content = token, IsCorrect = correctSet.Contains(token) })
                .ToList();
        }
        else if (string.Equals(question.Type, "Essay", StringComparison.OrdinalIgnoreCase))
        {
            question.Options = new List<Option>();
            question.CorrectKeys = new List<string>();
        }
        else
        {
            question.Options = optionLines
                .Select(line => new Option { Content = line, IsCorrect = correctSet.Contains(line) })
                .ToList();
        }

        CreatedQuestion = question;
        DialogResult = true;
        Close();
    }

    private static List<string> SplitLines(string? raw)
    {
        return (raw ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> SplitKeys(string? raw)
    {
        return (raw ?? string.Empty)
            .Split(new[] { '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static MatchPair? ParseMatchingPair(string raw)
    {
        var parts = raw.Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return null;
        }

        return new MatchPair
        {
            Left = parts[0],
            Right = parts[1]
        };
    }

    private static string NormalizeQuestionType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return "MCQ";
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "mcq" => "MCQ",
            "truefalse" => "TrueFalse",
            "true/false" => "TrueFalse",
            "essay" => "Essay",
            "matching" => "Matching",
            "dragdrop" => "DragDrop",
            "drag and drop" => "DragDrop",
            _ => type.Trim()
        };
    }
}
