using UniTestSystem.AdminApp.Models;
using UniTestSystem.AdminApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UniTestSystem.AdminApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ApiService _apiService;
        private Test? _selectedTest;
        private User? _selectedUser;
        private Question? _selectedQuestion;
        private Session? _selectedSession;
        private DashboardStats? _stats;
        private string _statusMessage = "Ready";
        private Faculty? _selectedFaculty;
        private StudentClass? _selectedClass;

        // UI Helpers for nested collections
        private ObservableCollection<OptionWrapper> _selectedQuestionOptions = new();
        private ObservableCollection<string> _selectedQuestionCorrectKeys = new();
        private ObservableCollection<TestItem> _selectedTestItems = new();
        public string CurrentUserName => _apiService.CurrentUser?.Name ?? "Admin";
        public bool IsAdmin => _apiService.CurrentUser?.Role == "Admin";
        public bool IsStaff => _apiService.CurrentUser?.Role == "Staff";
        public bool CanManageAcademic => IsAdmin || IsStaff;
        public bool CanManageSystem => IsAdmin;

        public MainViewModel(ApiService apiService)
        {
            _apiService = apiService;
            Tests = new ObservableCollection<Test>();
            Users = new ObservableCollection<User>();
            Questions = new ObservableCollection<Question>();
            Sessions = new ObservableCollection<Session>();
            Faculties = new ObservableCollection<Faculty>();
            Classes = new ObservableCollection<StudentClass>();

            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            
            AddTestCommand = new RelayCommand(() => { SelectedTest = new Test(); StatusMessage = "Adding new test..."; });
            SaveTestCommand = new RelayCommand(async () => await SaveTestAsync(), () => SelectedTest != null);
            DeleteTestCommand = new RelayCommand(async () => await DeleteTestAsync(), () => SelectedTest != null && !string.IsNullOrEmpty(SelectedTest.Id));
            
            AddUserCommand = new RelayCommand(() => { SelectedUser = new User(); StatusMessage = "Adding new student..."; });
            SaveUserCommand = new RelayCommand(async () => await SaveUserAsync(), () => SelectedUser != null);
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => SelectedUser != null && !string.IsNullOrEmpty(SelectedUser.Id));

            AddQuestionCommand = new RelayCommand(() => { SelectedQuestion = new Question(); StatusMessage = "Adding new question..."; });
            SaveQuestionCommand = new RelayCommand(async () => await SaveQuestionAsync(), () => SelectedQuestion != null);
            DeleteQuestionCommand = new RelayCommand(async () => await DeleteQuestionAsync(), () => SelectedQuestion != null && !string.IsNullOrEmpty(SelectedQuestion.Id));

            DeleteSessionCommand = new RelayCommand(async () => await DeleteSessionAsync(), () => SelectedSession != null);

            ExportXlsxCommand = new RelayCommand(async () => await ExportAsync("xlsx"));
            ExportPdfCommand = new RelayCommand(async () => await ExportAsync("pdf"));

            // Question Option Commands
            AddOptionCommand = new RelayCommand(AddOption);
            RemoveOptionCommand = new RelayCommand(RemoveOption, () => SelectedOption != null);
            ToggleCorrectKeyCommand = new RelayCommand(ToggleCorrectKey, () => SelectedOption != null);

            // Test Item Commands
            AddTestItemCommand = new RelayCommand(AddTestItem);
            RemoveTestItemCommand = new RelayCommand(RemoveTestItem, () => SelectedTestItem != null);

            AddFacultyCommand = new RelayCommand(() => { SelectedFaculty = new Faculty { Name = "New Faculty" }; });
            SaveFacultyCommand = new RelayCommand(async () => await SaveFacultyAsync());
            DeleteFacultyCommand = new RelayCommand(async () => await DeleteFacultyAsync());

            AddClassCommand = new RelayCommand(() => { SelectedClass = new StudentClass { Name = "New Class" }; });
            SaveClassCommand = new RelayCommand(async () => await SaveClassAsync());
            DeleteClassCommand = new RelayCommand(async () => await DeleteClassAsync());

            // Load data automatically on startup
            _ = LoadDataAsync();
        }

        public ObservableCollection<Test> Tests { get; }
        public ObservableCollection<User> Users { get; }
        public ObservableCollection<Question> Questions { get; }
        public ObservableCollection<Session> Sessions { get; }
        public ObservableCollection<Faculty> Faculties { get; }
        public ObservableCollection<StudentClass> Classes { get; }

        public ObservableCollection<OptionWrapper> SelectedQuestionOptions => _selectedQuestionOptions;
        public ObservableCollection<string> SelectedQuestionCorrectKeys => _selectedQuestionCorrectKeys;
        public ObservableCollection<TestItem> SelectedTestItems => _selectedTestItems;

        private OptionWrapper? _selectedOption;
        public OptionWrapper? SelectedOption 
        { 
            get => _selectedOption; 
            set { _selectedOption = value; OnPropertyChanged(); } 
        }

        private TestItem? _selectedTestItem;
        public TestItem? SelectedTestItem
        {
            get => _selectedTestItem;
            set { _selectedTestItem = value; OnPropertyChanged(); }
        }

        public Test? SelectedTest
        {
            get => _selectedTest;
            set 
            { 
                _selectedTest = value; 
                OnPropertyChanged(); 
                SyncTestItems();
            }
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; OnPropertyChanged(); }
        }

        public Question? SelectedQuestion
        {
            get => _selectedQuestion;
            set 
            { 
                _selectedQuestion = value; 
                OnPropertyChanged(); 
                SyncQuestionDetails();
            }
        }

        public Session? SelectedSession
        {
            get => _selectedSession;
            set { _selectedSession = value; OnPropertyChanged(); }
        }

        public DashboardStats? Stats
        {
            get => _stats;
            set { _stats = value; OnPropertyChanged(); }
        }

        public Faculty? SelectedFaculty
        {
            get => _selectedFaculty;
            set { _selectedFaculty = value; OnPropertyChanged(); }
        }

        public StudentClass? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand LoadDataCommand { get; }
        public RelayCommand AddTestCommand { get; }
        public RelayCommand SaveTestCommand { get; }
        public RelayCommand DeleteTestCommand { get; }
        public RelayCommand AddUserCommand { get; }
        public RelayCommand SaveUserCommand { get; }
        public RelayCommand DeleteUserCommand { get; }
        public RelayCommand AddQuestionCommand { get; }
        public RelayCommand SaveQuestionCommand { get; }
        public RelayCommand DeleteQuestionCommand { get; }
        public RelayCommand DeleteSessionCommand { get; }
        public RelayCommand ExportXlsxCommand { get; }
        public RelayCommand ExportPdfCommand { get; }

        public RelayCommand AddOptionCommand { get; }
        public RelayCommand RemoveOptionCommand { get; }
        public RelayCommand ToggleCorrectKeyCommand { get; }
        public RelayCommand AddTestItemCommand { get; }
        public RelayCommand RemoveTestItemCommand { get; }
        
        public RelayCommand AddFacultyCommand { get; }
        public RelayCommand SaveFacultyCommand { get; }
        public RelayCommand DeleteFacultyCommand { get; }
        public RelayCommand AddClassCommand { get; }
        public RelayCommand SaveClassCommand { get; }
        public RelayCommand DeleteClassCommand { get; }

        private async Task LoadDataAsync()
        {
            try
            {
                StatusMessage = "Loading data...";
                
                var testsTask = _apiService.GetTestsAsync();
                var usersTask = _apiService.GetUsersAsync();
                var questionsTask = _apiService.GetQuestionsAsync();
                var sessionsTask = _apiService.GetSessionsAsync();
                var statsTask = _apiService.GetDashboardStatsAsync();
                var facultiesTask = _apiService.GetFacultiesAsync();
                var classesTask = _apiService.GetClassesAsync();

                await Task.WhenAll(testsTask, usersTask, questionsTask, sessionsTask, statsTask, facultiesTask, classesTask);

                if (testsTask.Result != null) { Tests.Clear(); foreach (var t in testsTask.Result) Tests.Add(t); }
                if (usersTask.Result != null) { Users.Clear(); foreach (var u in usersTask.Result) Users.Add(u); }
                if (questionsTask.Result != null) { Questions.Clear(); foreach (var q in questionsTask.Result) Questions.Add(q); }
                if (sessionsTask.Result != null) { Sessions.Clear(); foreach (var s in sessionsTask.Result) Sessions.Add(s); }
                if (facultiesTask.Result != null) { Faculties.Clear(); foreach (var f in facultiesTask.Result) Faculties.Add(f); }
                if (classesTask.Result != null) { Classes.Clear(); foreach (var c in classesTask.Result) Classes.Add(c); }
                Stats = statsTask.Result;

                StatusMessage = $"Loaded {Tests.Count} tests, {Users.Count} students, {Questions.Count} questions, {Sessions.Count} sessions, {Faculties.Count} faculties, {Classes.Count} classes.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task SaveTestAsync()
        {
            if (SelectedTest == null) return;
            try
            {
                StatusMessage = "Saving test...";
                bool success = string.IsNullOrEmpty(SelectedTest.Id) 
                    ? await _apiService.CreateTestAsync(SelectedTest) 
                    : await _apiService.UpdateTestAsync(SelectedTest.Id, SelectedTest);
                
                if (success) { StatusMessage = "Test saved successfully"; await LoadDataAsync(); }
                else StatusMessage = "Failed to save test";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteTestAsync()
        {
            if (SelectedTest == null || string.IsNullOrEmpty(SelectedTest.Id)) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete test '{SelectedTest.Title}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                StatusMessage = "Deleting test...";
                if (await _apiService.DeleteTestAsync(SelectedTest.Id)) { StatusMessage = "Test deleted"; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete test";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task SaveUserAsync()
        {
            if (SelectedUser == null) return;
            try
            {
                StatusMessage = "Saving student...";
                bool success = string.IsNullOrEmpty(SelectedUser.Id)
                    ? await _apiService.CreateUserAsync(SelectedUser, "Password123!")
                    : await _apiService.UpdateUserAsync(SelectedUser.Id, SelectedUser);
                
                if (success) { StatusMessage = "Student saved successfully"; await LoadDataAsync(); }
                else StatusMessage = "Failed to save student";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null || string.IsNullOrEmpty(SelectedUser.Id)) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete student '{SelectedUser.Name}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                StatusMessage = "Deleting student...";
                if (await _apiService.DeleteUserAsync(SelectedUser.Id)) { StatusMessage = "Student deleted"; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete student";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task SaveQuestionAsync()
        {
            if (SelectedQuestion == null) return;
            try
            {
                StatusMessage = "Saving question...";
                bool success = string.IsNullOrEmpty(SelectedQuestion.Id)
                    ? await _apiService.CreateQuestionAsync(SelectedQuestion)
                    : await _apiService.UpdateQuestionAsync(SelectedQuestion.Id, SelectedQuestion);

                if (success) { StatusMessage = "Question saved successfully"; await LoadDataAsync(); }
                else StatusMessage = "Failed to save question";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteQuestionAsync()
        {
            if (SelectedQuestion == null || string.IsNullOrEmpty(SelectedQuestion.Id)) return;
            try
            {
                StatusMessage = "Deleting question...";
                if (await _apiService.DeleteQuestionAsync(SelectedQuestion.Id)) { StatusMessage = "Question deleted"; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete question";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteSessionAsync()
        {
            if (SelectedSession == null || string.IsNullOrEmpty(SelectedSession.Id)) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete/terminate this session for '{SelectedSession.UserName}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                StatusMessage = "Deleting session...";
                if (await _apiService.DeleteSessionAsync(SelectedSession.Id)) { StatusMessage = "Session deleted"; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete session";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private void SyncQuestionDetails()
        {
            _selectedQuestionOptions.Clear();
            _selectedQuestionCorrectKeys.Clear();
            if (SelectedQuestion != null)
            {
                if (SelectedQuestion.Options != null) foreach (var opt in SelectedQuestion.Options) _selectedQuestionOptions.Add(new OptionWrapper { Text = opt });
                if (SelectedQuestion.CorrectKeys != null) foreach (var key in SelectedQuestion.CorrectKeys) _selectedQuestionCorrectKeys.Add(key);
            }
        }

        private void AddOption()
        {
            if (SelectedQuestion == null) return;
            _selectedQuestionOptions.Add(new OptionWrapper { Text = "New Option" });
            UpdateQuestionModelFromUI();
        }

        private void RemoveOption()
        {
            if (SelectedQuestion == null || SelectedOption == null) return;
            _selectedQuestionOptions.Remove(SelectedOption);
            _selectedQuestionCorrectKeys.Remove(SelectedOption.Text);
            UpdateQuestionModelFromUI();
        }

        private void ToggleCorrectKey()
        {
            if (SelectedQuestion == null || SelectedOption == null) return;
            if (_selectedQuestionCorrectKeys.Contains(SelectedOption.Text)) _selectedQuestionCorrectKeys.Remove(SelectedOption.Text);
            else _selectedQuestionCorrectKeys.Add(SelectedOption.Text);
            UpdateQuestionModelFromUI();
        }

        private void UpdateQuestionModelFromUI()
        {
            if (SelectedQuestion == null) return;
            SelectedQuestion.Options = _selectedQuestionOptions.Select(x => x.Text).ToList();
            SelectedQuestion.CorrectKeys = _selectedQuestionCorrectKeys.ToList();
        }

        private void SyncTestItems()
        {
            _selectedTestItems.Clear();
            if (SelectedTest != null && SelectedTest.Items != null)
            {
                foreach (var item in SelectedTest.Items) _selectedTestItems.Add(item);
            }
        }

        private void AddTestItem()
        {
            if (SelectedTest == null) return;
            // For simplicity, add the first available question not already in test, or just a placeholder
            var itm = new TestItem { QuestionId = "Select Question", Points = 1 };
            _selectedTestItems.Add(itm);
            UpdateTestModelFromUI();
        }

        private void RemoveTestItem()
        {
            if (SelectedTest == null || SelectedTestItem == null) return;
            _selectedTestItems.Remove(SelectedTestItem);
            UpdateTestModelFromUI();
        }

        private void UpdateTestModelFromUI()
        {
            if (SelectedTest == null) return;
            SelectedTest.Items = _selectedTestItems.ToList();
            SelectedTest.QuestionIds = _selectedTestItems.Select(x => x.QuestionId).ToList();
        }

        private async Task ExportAsync(string type)
        {
            try
            {
                StatusMessage = $"Exporting {type}...";
                byte[]? data = type == "xlsx" ? await _apiService.DownloadReportXlsxAsync() : await _apiService.DownloadReportPdfAsync();
                if (data != null)
                {
                    string fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.{type}";
                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                    await File.WriteAllBytesAsync(filePath, data);
                    StatusMessage = $"Report saved to Desktop: {fileName}";
                }
                else StatusMessage = "Export failed";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task SaveFacultyAsync()
        {
            if (SelectedFaculty == null) return;
            try
            {
                StatusMessage = "Saving faculty...";
                bool success = false;
                if (string.IsNullOrEmpty(SelectedFaculty.Id))
                {
                    var res = await _apiService.CreateFacultyAsync(SelectedFaculty);
                    success = res != null;
                }
                else success = await _apiService.UpdateFacultyAsync(SelectedFaculty.Id, SelectedFaculty);

                if (success) { StatusMessage = "Faculty saved."; await LoadDataAsync(); }
                else StatusMessage = "Failed to save faculty.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteFacultyAsync()
        {
            if (SelectedFaculty == null || string.IsNullOrEmpty(SelectedFaculty.Id)) return;
            try
            {
                StatusMessage = "Deleting faculty...";
                if (await _apiService.DeleteFacultyAsync(SelectedFaculty.Id)) { StatusMessage = "Faculty deleted."; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete faculty.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task SaveClassAsync()
        {
            if (SelectedClass == null) return;
            try
            {
                StatusMessage = "Saving class...";
                bool success = false;
                if (string.IsNullOrEmpty(SelectedClass.Id))
                {
                    var res = await _apiService.CreateClassAsync(SelectedClass);
                    success = res != null;
                }
                else success = await _apiService.UpdateClassAsync(SelectedClass.Id, SelectedClass);

                if (success) { StatusMessage = "Class saved."; await LoadDataAsync(); }
                else StatusMessage = "Failed to save class.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteClassAsync()
        {
            if (SelectedClass == null || string.IsNullOrEmpty(SelectedClass.Id)) return;
            try
            {
                StatusMessage = "Deleting class...";
                if (await _apiService.DeleteClassAsync(SelectedClass.Id)) { StatusMessage = "Class deleted."; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete class.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task>? _executeAsync;
        private readonly Action? _executeSync;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public RelayCommand(Action executeSync, Func<bool>? canExecute = null)
        {
            _executeSync = executeSync;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public async void Execute(object? parameter)
        {
            if (_executeAsync != null) await _executeAsync();
            else _executeSync?.Invoke();
        }
    }

    public class OptionWrapper : INotifyPropertyChanged
    {
        private string _text = "";
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
