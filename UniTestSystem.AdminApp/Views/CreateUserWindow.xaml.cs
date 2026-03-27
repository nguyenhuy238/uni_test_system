using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Windows;
using UniTestSystem.AdminApp.Models;

namespace UniTestSystem.AdminApp.Views;

public partial class CreateUserWindow : Window
{
    public User? CreatedUser { get; private set; }

    public CreateUserWindow(IEnumerable<string> roles, IEnumerable<Faculty> faculties, IEnumerable<StudentClass> classes)
    {
        InitializeComponent();

        var roleList = roles?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        if (roleList.Count == 0)
        {
            roleList.Add("Student");
        }

        RoleComboBox.ItemsSource = roleList;
        RoleComboBox.SelectedItem = roleList.FirstOrDefault(x => string.Equals(x, "Student", StringComparison.OrdinalIgnoreCase)) ?? roleList[0];

        FacultyComboBox.ItemsSource = faculties?.ToList() ?? new List<Faculty>();
        ClassComboBox.ItemsSource = classes?.ToList() ?? new List<StudentClass>();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var name = (NameTextBox.Text ?? string.Empty).Trim();
        var email = (EmailTextBox.Text ?? string.Empty).Trim();
        var role = (RoleComboBox.SelectedItem?.ToString() ?? "Student").Trim();
        var department = (FacultyComboBox.SelectedValue?.ToString() ?? string.Empty).Trim();
        var classId = (ClassComboBox.SelectedValue?.ToString() ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            NameTextBox.Focus();
            return;
        }

        if (!IsValidEmail(email))
        {
            MessageBox.Show("A valid email is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            EmailTextBox.Focus();
            return;
        }

        CreatedUser = new User
        {
            Name = name,
            Email = email,
            Role = string.IsNullOrWhiteSpace(role) ? "Student" : role,
            Department = department,
            TeamId = classId,
            Level = "Junior"
        };

        DialogResult = true;
        Close();
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
