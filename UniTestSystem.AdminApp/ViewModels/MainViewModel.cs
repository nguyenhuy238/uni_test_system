using UniTestSystem.AdminApp.Models;
using UniTestSystem.AdminApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Net.Mail;
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
        private Course? _selectedCourse;
        private Enrollment? _selectedEnrollment;
        private SystemSettingsModel _systemSettings = new();
        private ExamSchedule? _selectedExamSchedule;
        private ExamScheduleDraft? _selectedScheduleDraft;
        private string _auditFrom = "";
        private string _auditTo = "";
        private string _auditKeyword = "";
        private string _auditActor = "";
        private string _selectedScheduleTestId = "";
        private string _selectedScheduleCourseId = "";
        private string _selectedScheduleRoom = "";
        private string _selectedScheduleExamType = "Final";
        private DateTime _selectedScheduleStartTime = DateTime.Now.AddDays(1).Date.AddHours(8);
        private DateTime _selectedScheduleEndTime = DateTime.Now.AddDays(1).Date.AddHours(10);
        private string _selectedExportPeriod = "Monthly";
        private string _selectedExportFormat = "xlsx";
        private bool _isDarkTheme;
        private string _yearEndAcademicYear = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}";
        private string _yearEndFacultyId = "";
        private YearEndPreviewModel _yearEndPreview = new();

        // UI Helpers for nested collections
        private ObservableCollection<OptionWrapper> _selectedQuestionOptions = new();
        private ObservableCollection<string> _selectedQuestionCorrectKeys = new();
        private string _selectedQuestionCorrectKeysText = string.Empty;
        private bool _syncingCorrectKeysText;
        private ObservableCollection<TestItem> _selectedTestItems = new();
        private const string DefaultNewUserPassword = "123456";
        public string CurrentUserName => _apiService.CurrentUser?.Name ?? "Admin";
        public string CurrentUserRole => _apiService.CurrentUser?.Role ?? "Unknown";
        public bool IsAdmin => _apiService.CurrentUser?.Role == "Admin";
        public bool IsStaff => _apiService.CurrentUser?.Role == "Staff";
        public bool CanManageAcademic => IsAdmin || IsStaff;
        public bool CanManageSystem => IsAdmin;
        public bool CanManageUsersRoles => IsAdmin || IsStaff;
        public bool CanViewAudit => IsAdmin;
        public bool CanViewSettings => IsAdmin;
        public bool CanViewMaintenance => IsAdmin;
        public bool CanManageScheduling => IsAdmin || IsStaff;
        public bool CanExportReports => IsAdmin || IsStaff;
        public bool CanEditRoles => IsAdmin;
        public IEnumerable<string> EditableRoles => IsAdmin
            ? new[] { "Admin", "Staff", "Lecturer", "Student" }
            : new[] { "Lecturer", "Student" };

        public MainViewModel(ApiService apiService)
        {
            _apiService = apiService;
            Tests = new ObservableCollection<Test>();
            Users = new ObservableCollection<User>();
            Questions = new ObservableCollection<Question>();
            Sessions = new ObservableCollection<Session>();
            Faculties = new ObservableCollection<Faculty>();
            Classes = new ObservableCollection<StudentClass>();
            Courses = new ObservableCollection<Course>();
            Enrollments = new ObservableCollection<Enrollment>();
            Lecturers = new ObservableCollection<User>(); // Filtered from Users
            AuditLogs = new ObservableCollection<AuditLogEntry>();
            ExamSchedules = new ObservableCollection<ExamSchedule>();
            ScheduleDrafts = new ObservableCollection<ExamScheduleDraft>();
            BackupFiles = new ObservableCollection<BackupFileInfo>();
            PeriodOptions = new ObservableCollection<string> { "Weekly", "Monthly", "Quarterly" };
            ExportFormatOptions = new ObservableCollection<string> { "xlsx", "pdf" };
            QuestionTypeOptions = new ObservableCollection<string> { "MCQ", "TrueFalse", "Essay", "Matching", "DragDrop" };
            QuestionSkillOptions = new ObservableCollection<QuestionMetadataItem>();
            QuestionDifficultyOptions = new ObservableCollection<QuestionMetadataItem>();
            QuestionSubjectOptions = new ObservableCollection<QuestionMetadataItem>();
            QuestionBankOptions = new ObservableCollection<QuestionMetadataItem>();
            YearEndStudents = new ObservableCollection<YearEndStudentSummaryModel>();
            YearEndWarningStudents = new ObservableCollection<YearEndStudentSummaryModel>();

            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            
            AddTestCommand = new RelayCommand(StartNewTest, () => IsAdmin);
            SaveTestCommand = new RelayCommand(async () => await SaveTestAsync(), () => IsAdmin && SelectedTest != null);
            DeleteTestCommand = new RelayCommand(async () => await DeleteTestAsync(), () => IsAdmin && SelectedTest != null && !string.IsNullOrEmpty(SelectedTest.Id));
            
            AddUserCommand = new RelayCommand(StartNewUser, () => CanManageUsersRoles);
            SaveUserCommand = new RelayCommand(async () => await SaveUserAsync(), () => CanManageUsersRoles && SelectedUser != null);
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => CanManageUsersRoles && SelectedUser != null && !string.IsNullOrEmpty(SelectedUser.Id));

            AddQuestionCommand = new RelayCommand(StartNewQuestion, () => IsAdmin);
            SaveQuestionCommand = new RelayCommand(async () => await SaveQuestionAsync(), () => IsAdmin && SelectedQuestion != null);
            DeleteQuestionCommand = new RelayCommand(async () => await DeleteQuestionAsync(), () => IsAdmin && SelectedQuestion != null && !string.IsNullOrEmpty(SelectedQuestion.Id));

            DeleteSessionCommand = new RelayCommand(async () => await DeleteSessionAsync(), () => CanManageAcademic && SelectedSession != null);

            ExportXlsxCommand = new RelayCommand(async () => await ExportAsync("xlsx"), () => CanExportReports);
            ExportPdfCommand = new RelayCommand(async () => await ExportAsync("pdf"), () => CanExportReports);
            ExportPeriodicReportCommand = new RelayCommand(async () => await ExportPeriodicReportAsync(), () => CanExportReports);

            SubmitQuestionCommand = new RelayCommand(async () => await SubmitQuestionAsync(), () => IsAdmin && SelectedQuestion != null && SelectedQuestion.Status == "Draft");
            ApproveQuestionCommand = new RelayCommand(async () => await ApproveQuestionAsync(), () => IsAdmin && SelectedQuestion != null && SelectedQuestion.Status == "Pending");
            RejectQuestionCommand = new RelayCommand(async () => await RejectQuestionAsync(), () => IsAdmin && SelectedQuestion != null && SelectedQuestion.Status == "Pending");

            // Question Option Commands
            AddOptionCommand = new RelayCommand(AddOption, () => IsAdmin);
            RemoveOptionCommand = new RelayCommand(RemoveOption, () => IsAdmin && SelectedOption != null);
            ToggleCorrectKeyCommand = new RelayCommand(ToggleCorrectKey, () => IsAdmin && SelectedOption != null);

            // Test Item Commands
            AddTestItemCommand = new RelayCommand(AddTestItem, () => IsAdmin);
            RemoveTestItemCommand = new RelayCommand(RemoveTestItem, () => IsAdmin && SelectedTestItem != null);

            AddFacultyCommand = new RelayCommand(() => { SelectedFaculty = new Faculty { Name = "New Faculty" }; }, () => CanManageAcademic);
            SaveFacultyCommand = new RelayCommand(async () => await SaveFacultyAsync(), () => CanManageAcademic);
            DeleteFacultyCommand = new RelayCommand(async () => await DeleteFacultyAsync(), () => CanManageAcademic);

            AddClassCommand = new RelayCommand(() => { SelectedClass = new StudentClass { Name = "New Class" }; }, () => CanManageAcademic);
            SaveClassCommand = new RelayCommand(async () => await SaveClassAsync(), () => CanManageAcademic);
            DeleteClassCommand = new RelayCommand(async () => await DeleteClassAsync(), () => CanManageAcademic);

            AddCourseCommand = new RelayCommand(() => { SelectedCourse = new Course { Name = "New Course" }; }, () => CanManageAcademic);
            SaveCourseCommand = new RelayCommand(async () => await SaveCourseAsync(), () => CanManageAcademic);
            DeleteCourseCommand = new RelayCommand(async () => await DeleteCourseAsync(), () => CanManageAcademic);

            EnrollStudentCommand = new RelayCommand(async () => await EnrollStudentAsync(), () => CanManageAcademic);
            UnenrollStudentCommand = new RelayCommand(async () => await UnenrollStudentAsync(), () => CanManageAcademic);

            ImportStudentsCommand = new RelayCommand(async () => await ImportAsync("students"), () => CanManageAcademic);
            ImportCoursesCommand = new RelayCommand(async () => await ImportAsync("courses"), () => CanManageAcademic);

            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync(), () => CanViewSettings);
            LoadAuditCommand = new RelayCommand(async () => await LoadAuditAsync(), () => CanViewAudit);
            CreateBackupCommand = new RelayCommand(async () => await CreateBackupAsync(), () => CanViewMaintenance);
            RestoreBackupCommand = new RelayCommand(async () => await RestoreBackupAsync(), () => CanViewMaintenance);
            AddScheduleDraftCommand = new RelayCommand(AddScheduleDraft, () => CanManageScheduling);
            RemoveScheduleDraftCommand = new RelayCommand(RemoveScheduleDraft, () => CanManageScheduling && SelectedScheduleDraft != null);
            CreateSingleScheduleCommand = new RelayCommand(async () => await CreateSingleScheduleAsync(), () => CanManageScheduling);
            CreateBulkScheduleCommand = new RelayCommand(async () => await CreateBulkSchedulesAsync(), () => CanManageScheduling && ScheduleDrafts.Count > 0);
            DeleteScheduleCommand = new RelayCommand(async () => await DeleteScheduleAsync(), () => CanManageScheduling && SelectedExamSchedule != null);
            ExportScheduleCsvCommand = new RelayCommand(async () => await ExportScheduleCsvAsync(), () => CanManageScheduling);
            PreviewYearEndCommand = new RelayCommand(async () => await PreviewYearEndAsync(), () => CanManageAcademic);
            FinalizeYearEndCommand = new RelayCommand(async () => await FinalizeYearEndAsync(), () => CanManageAcademic && YearEndPreview.Prerequisites.IsReady && YearEndStudents.Count > 0);
            ToggleThemeCommand = new RelayCommand(() => { IsDarkTheme = !IsDarkTheme; StatusMessage = IsDarkTheme ? "Dark mode enabled." : "Light mode enabled."; });

            // Load data automatically on startup
            _ = LoadDataAsync();
        }

        public ObservableCollection<Test> Tests { get; }
        public ObservableCollection<User> Users { get; }
        public ObservableCollection<Question> Questions { get; }
        public ObservableCollection<Session> Sessions { get; }
        public ObservableCollection<Faculty> Faculties { get; }
        public ObservableCollection<StudentClass> Classes { get; }
        public ObservableCollection<Course> Courses { get; }
        public ObservableCollection<Enrollment> Enrollments { get; }
        public ObservableCollection<User> Lecturers { get; }
        public ObservableCollection<AuditLogEntry> AuditLogs { get; }
        public ObservableCollection<ExamSchedule> ExamSchedules { get; }
        public ObservableCollection<ExamScheduleDraft> ScheduleDrafts { get; }
        public ObservableCollection<BackupFileInfo> BackupFiles { get; }
        public ObservableCollection<YearEndStudentSummaryModel> YearEndStudents { get; }
        public ObservableCollection<YearEndStudentSummaryModel> YearEndWarningStudents { get; }
        public ObservableCollection<string> PeriodOptions { get; }
        public ObservableCollection<string> ExportFormatOptions { get; }
        public ObservableCollection<string> QuestionTypeOptions { get; }
        public ObservableCollection<QuestionMetadataItem> QuestionSkillOptions { get; }
        public ObservableCollection<QuestionMetadataItem> QuestionDifficultyOptions { get; }
        public ObservableCollection<QuestionMetadataItem> QuestionSubjectOptions { get; }
        public ObservableCollection<QuestionMetadataItem> QuestionBankOptions { get; }

        public ObservableCollection<OptionWrapper> SelectedQuestionOptions => _selectedQuestionOptions;
        public ObservableCollection<string> SelectedQuestionCorrectKeys => _selectedQuestionCorrectKeys;
        public string SelectedQuestionCorrectKeysText
        {
            get => _selectedQuestionCorrectKeysText;
            set
            {
                var newValue = value ?? string.Empty;
                if (string.Equals(_selectedQuestionCorrectKeysText, newValue, StringComparison.Ordinal))
                {
                    return;
                }

                _selectedQuestionCorrectKeysText = newValue;
                OnPropertyChanged();

                if (_syncingCorrectKeysText)
                {
                    return;
                }

                SyncCorrectKeysCollectionFromText();
                UpdateQuestionModelFromUI();
            }
        }
        public ObservableCollection<TestItem> SelectedTestItems => _selectedTestItems;

        private OptionWrapper? _selectedOption;
        public OptionWrapper? SelectedOption 
        { 
            get => _selectedOption; 
            set
            {
                _selectedOption = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
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
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Question? SelectedQuestion
        {
            get => _selectedQuestion;
            set 
            { 
                _selectedQuestion = value; 
                OnPropertyChanged(); 
                EnsureQuestionMetadataValues();
                SyncQuestionDetails();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Session? SelectedSession
        {
            get => _selectedSession;
            set { _selectedSession = value; OnPropertyChanged(); }
        }

        public Course? SelectedCourse
        {
            get => _selectedCourse;
            set 
            { 
                _selectedCourse = value; 
                OnPropertyChanged(); 
                _ = LoadEnrollmentsAsync();
            }
        }

        public Enrollment? SelectedEnrollment
        {
            get => _selectedEnrollment;
            set { _selectedEnrollment = value; OnPropertyChanged(); }
        }

        public ExamSchedule? SelectedExamSchedule
        {
            get => _selectedExamSchedule;
            set { _selectedExamSchedule = value; OnPropertyChanged(); }
        }

        public ExamScheduleDraft? SelectedScheduleDraft
        {
            get => _selectedScheduleDraft;
            set { _selectedScheduleDraft = value; OnPropertyChanged(); }
        }

        public DashboardStats? Stats
        {
            get => _stats;
            set { _stats = value; OnPropertyChanged(); }
        }

        public SystemSettingsModel SystemSettings
        {
            get => _systemSettings;
            set { _systemSettings = value; OnPropertyChanged(); }
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

        public string AuditFrom
        {
            get => _auditFrom;
            set { _auditFrom = value; OnPropertyChanged(); }
        }

        public string AuditTo
        {
            get => _auditTo;
            set { _auditTo = value; OnPropertyChanged(); }
        }

        public string AuditKeyword
        {
            get => _auditKeyword;
            set { _auditKeyword = value; OnPropertyChanged(); }
        }

        public string AuditActor
        {
            get => _auditActor;
            set { _auditActor = value; OnPropertyChanged(); }
        }

        public string SelectedScheduleTestId
        {
            get => _selectedScheduleTestId;
            set { _selectedScheduleTestId = value; OnPropertyChanged(); }
        }

        public string SelectedScheduleCourseId
        {
            get => _selectedScheduleCourseId;
            set { _selectedScheduleCourseId = value; OnPropertyChanged(); }
        }

        public string SelectedScheduleRoom
        {
            get => _selectedScheduleRoom;
            set { _selectedScheduleRoom = value; OnPropertyChanged(); }
        }

        public string SelectedScheduleExamType
        {
            get => _selectedScheduleExamType;
            set { _selectedScheduleExamType = value; OnPropertyChanged(); }
        }

        public DateTime SelectedScheduleStartTime
        {
            get => _selectedScheduleStartTime;
            set { _selectedScheduleStartTime = value; OnPropertyChanged(); }
        }

        public DateTime SelectedScheduleEndTime
        {
            get => _selectedScheduleEndTime;
            set { _selectedScheduleEndTime = value; OnPropertyChanged(); }
        }

        public string SelectedExportPeriod
        {
            get => _selectedExportPeriod;
            set { _selectedExportPeriod = value; OnPropertyChanged(); }
        }

        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set { _selectedExportFormat = value; OnPropertyChanged(); }
        }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set { _isDarkTheme = value; OnPropertyChanged(); }
        }

        public string YearEndAcademicYear
        {
            get => _yearEndAcademicYear;
            set { _yearEndAcademicYear = value; OnPropertyChanged(); }
        }

        public string YearEndFacultyId
        {
            get => _yearEndFacultyId;
            set { _yearEndFacultyId = value; OnPropertyChanged(); }
        }

        public YearEndPreviewModel YearEndPreview
        {
            get => _yearEndPreview;
            set { _yearEndPreview = value; OnPropertyChanged(); }
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
        public RelayCommand ExportPeriodicReportCommand { get; }
        public RelayCommand SubmitQuestionCommand { get; }
        public RelayCommand ApproveQuestionCommand { get; }
        public RelayCommand RejectQuestionCommand { get; }

        public RelayCommand AddCourseCommand { get; }
        public RelayCommand SaveCourseCommand { get; }
        public RelayCommand DeleteCourseCommand { get; }
        
        public RelayCommand EnrollStudentCommand { get; }
        public RelayCommand UnenrollStudentCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand LoadAuditCommand { get; }
        public RelayCommand CreateBackupCommand { get; }
        public RelayCommand RestoreBackupCommand { get; }
        public RelayCommand AddScheduleDraftCommand { get; }
        public RelayCommand RemoveScheduleDraftCommand { get; }
        public RelayCommand CreateSingleScheduleCommand { get; }
        public RelayCommand CreateBulkScheduleCommand { get; }
        public RelayCommand DeleteScheduleCommand { get; }
        public RelayCommand ExportScheduleCsvCommand { get; }
        public RelayCommand PreviewYearEndCommand { get; }
        public RelayCommand FinalizeYearEndCommand { get; }
        public RelayCommand ToggleThemeCommand { get; }

        public RelayCommand ImportStudentsCommand { get; }
        public RelayCommand ImportCoursesCommand { get; }

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
                var questionMetadataTask = _apiService.GetQuestionMetadataAsync();
                var sessionsTask = _apiService.GetSessionsAsync();
                var statsTask = _apiService.GetDashboardStatsAsync();
                var facultiesTask = _apiService.GetFacultiesAsync();
                var classesTask = _apiService.GetClassesAsync();
                var coursesTask = _apiService.GetCoursesAsync();

                await Task.WhenAll(testsTask, usersTask, questionsTask, questionMetadataTask, sessionsTask, statsTask, facultiesTask, classesTask, coursesTask);

                List<string> failures = new();
                if (testsTask.Result != null) { Tests.Clear(); foreach (var t in testsTask.Result) Tests.Add(t); } else failures.Add("Tests");
                if (questionsTask.Result != null) { Questions.Clear(); foreach (var q in questionsTask.Result) Questions.Add(q); } else failures.Add("Questions");
                RefreshQuestionMetadataOptions(questionMetadataTask.Result);
                if (sessionsTask.Result != null) { Sessions.Clear(); foreach (var s in sessionsTask.Result) Sessions.Add(s); } else failures.Add("Sessions");
                if (facultiesTask.Result != null) { Faculties.Clear(); foreach (var f in facultiesTask.Result) Faculties.Add(f); } else failures.Add("Faculties");
                if (classesTask.Result != null) { Classes.Clear(); foreach (var c in classesTask.Result) Classes.Add(c); } else failures.Add("Classes");
                if (coursesTask.Result != null) { Courses.Clear(); foreach (var crs in coursesTask.Result) Courses.Add(crs); } else failures.Add("Courses");

                if (usersTask.Result != null)
                {
                    Users.Clear();

                    var facultyById = Faculties
                        .Where(f => !string.IsNullOrWhiteSpace(f.Id))
                        .ToDictionary(f => f.Id, f => f.Name ?? string.Empty);

                    foreach (var u in usersTask.Result)
                    {
                        if (!string.IsNullOrWhiteSpace(u.FacultyId) && facultyById.TryGetValue(u.FacultyId, out var facultyName))
                        {
                            u.Department = facultyName;
                        }

                        Users.Add(u);
                    }
                }
                else failures.Add("Users");
                
                if (Users.Any())
                {
                    Lecturers.Clear();
                    foreach (var u in Users.Where(u => u.Role == "Lecturer")) Lecturers.Add(u);
                }
                else
                {
                    Lecturers.Clear();
                }
                
                if (statsTask.Result != null) Stats = statsTask.Result;
                else failures.Add("Stats");

                if (CanViewSettings)
                {
                    await LoadSettingsAsync();
                }

                if (CanViewAudit)
                {
                    await LoadAuditAsync();
                }

                if (CanManageScheduling)
                {
                    await LoadExamSchedulesAsync();
                }

                if (CanViewMaintenance)
                {
                    await LoadBackupsAsync();
                }

                if (failures.Count > 0)
                {
                    StatusMessage = $"Warning: Failed to load {string.Join(", ", failures)}. Other data loaded.";
                }
                else
                {
                    StatusMessage = $"Loaded {Tests.Count} tests, {Users.Count} users, {Questions.Count} questions, {Sessions.Count} sessions.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Critical Error: {ex.Message}";
            }
        }

        private async Task SaveTestAsync()
        {
            if (SelectedTest == null) return;

            UpdateTestModelFromUI();
            var normalizedTitle = (SelectedTest.Title ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedTitle))
            {
                StatusMessage = "Test title is required.";
                return;
            }

            if (SelectedTest.DurationMinutes <= 0)
            {
                StatusMessage = "Duration must be greater than 0 minutes.";
                return;
            }

            SelectedTest.Title = normalizedTitle;
            SelectedTest.Type = string.Equals(SelectedTest.Type, "Exam", StringComparison.OrdinalIgnoreCase) ? "Exam" : "Test";
            SelectedTest.TotalMaxScore = 10m;

            var normalizedItems = (SelectedTest.Items ?? new List<TestItem>())
                .Where(item => !string.IsNullOrWhiteSpace(item.QuestionId))
                .Select((item, index) => new TestItem
                {
                    TestId = SelectedTest.Id,
                    QuestionId = item.QuestionId.Trim(),
                    Points = item.Points <= 0 ? 1m : item.Points,
                    Order = index
                })
                .ToList();

            var validQuestionIds = Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Id))
                .Select(q => q.Id)
                .ToHashSet(StringComparer.Ordinal);

            var invalidQuestion = normalizedItems
                .Select(item => item.QuestionId)
                .FirstOrDefault(id => !validQuestionIds.Contains(id));
            if (!string.IsNullOrWhiteSpace(invalidQuestion))
            {
                StatusMessage = $"Question ID '{invalidQuestion}' does not exist.";
                return;
            }

            SelectedTest.Items = normalizedItems;
            SelectedTest.QuestionIds = normalizedItems.Select(x => x.QuestionId).ToList();
            if (string.IsNullOrWhiteSpace(SelectedTest.CreatedBy))
            {
                SelectedTest.CreatedBy = CurrentUserName;
            }

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
            if (!IsAdmin &&
                (string.Equals(SelectedUser.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(SelectedUser.Role, "Staff", StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = "Staff cannot assign Admin/Staff roles.";
                return;
            }
            if (string.IsNullOrWhiteSpace(SelectedUser.Role))
            {
                SelectedUser.Role = "Student";
            }

            SelectedUser.Name = (SelectedUser.Name ?? string.Empty).Trim();
            SelectedUser.Email = (SelectedUser.Email ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(SelectedUser.Name))
            {
                StatusMessage = "User name is required.";
                return;
            }

            if (!IsValidEmail(SelectedUser.Email))
            {
                StatusMessage = "A valid email address is required.";
                return;
            }

            try
            {
                StatusMessage = "Saving user...";
                var isCreate = string.IsNullOrEmpty(SelectedUser.Id);
                bool success = isCreate
                    ? await _apiService.CreateUserAsync(SelectedUser)
                    : await _apiService.UpdateUserAsync(SelectedUser.Id, SelectedUser);
                
                if (success)
                {
                    StatusMessage = isCreate
                        ? $"User created successfully. Default password: {DefaultNewUserPassword}"
                        : "User saved successfully";
                    await LoadDataAsync();
                }
                else StatusMessage = "Failed to save user";
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
                StatusMessage = "Deleting user...";
                if (await _apiService.DeleteUserAsync(SelectedUser.Id)) { StatusMessage = "User deleted"; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete user";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task SaveQuestionAsync()
        {
            if (SelectedQuestion == null) return;
            try
            {
                UpdateQuestionModelFromUI();
                var questionValidation = ValidateQuestionForSave(SelectedQuestion);
                if (!string.IsNullOrWhiteSpace(questionValidation))
                {
                    StatusMessage = questionValidation;
                    return;
                }

                if (string.IsNullOrEmpty(SelectedQuestion.Id) && string.IsNullOrWhiteSpace(SelectedQuestion.Status))
                {
                    SelectedQuestion.Status = "Draft";
                }

                StatusMessage = "Saving question...";
                bool success = string.IsNullOrEmpty(SelectedQuestion.Id)
                    ? await _apiService.CreateQuestionAsync(SelectedQuestion)
                    : await _apiService.UpdateQuestionAsync(SelectedQuestion.Id, SelectedQuestion);

                if (success) { StatusMessage = "Question saved successfully"; await LoadDataAsync(); }
                else StatusMessage = "Failed to save question";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task SubmitQuestionAsync()
        {
            if (SelectedQuestion == null) return;
            try
            {
                StatusMessage = "Submitting question...";
                if (await _apiService.SubmitQuestionAsync(SelectedQuestion.Id)) { StatusMessage = "Question submitted"; await LoadDataAsync(); }
                else StatusMessage = "Failed to submit question";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task ApproveQuestionAsync()
        {
            if (SelectedQuestion == null) return;
            try
            {
                StatusMessage = "Approving question...";
                if (await _apiService.ApproveQuestionAsync(SelectedQuestion.Id)) { StatusMessage = "Question approved"; await LoadDataAsync(); }
                else StatusMessage = "Failed to approve question";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task RejectQuestionAsync()
        {
            if (SelectedQuestion == null) return;
            // Note: In a real WPF app, you'd use a dialog. For simplicity, we'll use a placeholder or try to get input.
            // Using a simple InputBox if available or just a default string.
            string reason = "Rejected by Admin"; 
            try
            {
                StatusMessage = "Rejecting question...";
                if (await _apiService.RejectQuestionAsync(SelectedQuestion.Id, reason)) { StatusMessage = "Question rejected"; await LoadDataAsync(); }
                else StatusMessage = "Failed to reject question";
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
            UnsubscribeOptionHandlers();
            _selectedQuestionOptions.Clear();
            _selectedQuestionCorrectKeys.Clear();
            if (SelectedQuestion == null)
            {
                UpdateCorrectKeysTextFromCollection();
                return;
            }

            var questionType = NormalizeQuestionType(SelectedQuestion.Type);

            if (string.Equals(questionType, "Matching", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedQuestion.MatchingPairs != null)
                {
                    foreach (var pair in SelectedQuestion.MatchingPairs)
                    {
                        if (string.IsNullOrWhiteSpace(pair.Left) && string.IsNullOrWhiteSpace(pair.Right))
                        {
                            continue;
                        }

                        AddQuestionOptionFromText($"{pair.Left}|{pair.Right}");
                        if (!string.IsNullOrWhiteSpace(pair.Right) && !_selectedQuestionCorrectKeys.Contains(pair.Right))
                        {
                            _selectedQuestionCorrectKeys.Add(pair.Right);
                        }
                    }
                }

                if (_selectedQuestionOptions.Count == 0 && SelectedQuestion.Options != null)
                {
                    foreach (var opt in SelectedQuestion.Options.Where(opt => !string.IsNullOrWhiteSpace(opt.Content)))
                    {
                        AddQuestionOptionFromText(opt.Content);
                        if (opt.IsCorrect)
                        {
                            _selectedQuestionCorrectKeys.Add(opt.Content);
                        }
                    }
                }
            }
            else if (string.Equals(questionType, "DragDrop", StringComparison.OrdinalIgnoreCase))
            {
                if (SelectedQuestion.DragDrop?.Tokens != null)
                {
                    foreach (var token in SelectedQuestion.DragDrop.Tokens.Where(token => !string.IsNullOrWhiteSpace(token)))
                    {
                        AddQuestionOptionFromText(token);
                    }
                }

                if (SelectedQuestion.DragDrop?.Slots != null)
                {
                    foreach (var answer in SelectedQuestion.DragDrop.Slots
                        .Select(slot => slot.Answer)
                        .Where(answer => !string.IsNullOrWhiteSpace(answer)))
                    {
                        if (!_selectedQuestionCorrectKeys.Contains(answer))
                        {
                            _selectedQuestionCorrectKeys.Add(answer);
                        }
                    }
                }

                if (_selectedQuestionOptions.Count == 0 && SelectedQuestion.Options != null)
                {
                    foreach (var opt in SelectedQuestion.Options.Where(opt => !string.IsNullOrWhiteSpace(opt.Content)))
                    {
                        AddQuestionOptionFromText(opt.Content);
                        if (opt.IsCorrect && !_selectedQuestionCorrectKeys.Contains(opt.Content))
                        {
                            _selectedQuestionCorrectKeys.Add(opt.Content);
                        }
                    }
                }
            }
            else if (SelectedQuestion.Options != null)
            {
                foreach (var opt in SelectedQuestion.Options)
                {
                    if (string.IsNullOrWhiteSpace(opt.Content))
                    {
                        continue;
                    }

                    AddQuestionOptionFromText(opt.Content);
                    if (opt.IsCorrect)
                    {
                        _selectedQuestionCorrectKeys.Add(opt.Content);
                    }
                }
            }

            if (SelectedQuestion.CorrectKeys != null)
            {
                foreach (var key in SelectedQuestion.CorrectKeys.Where(key => !_selectedQuestionCorrectKeys.Contains(key)))
                {
                    _selectedQuestionCorrectKeys.Add(key);
                }
            }

            UpdateCorrectKeysTextFromCollection();
        }

        private void AddOption()
        {
            if (SelectedQuestion == null) return;

            var questionType = NormalizeQuestionType(SelectedQuestion.Type);
            var defaultOptionText = string.Equals(questionType, "Matching", StringComparison.OrdinalIgnoreCase)
                ? "Left|Right"
                : "New Option";
            if (string.Equals(questionType, "TrueFalse", StringComparison.OrdinalIgnoreCase))
            {
                if (_selectedQuestionOptions.Any(option => string.Equals(option.Text, "True", StringComparison.OrdinalIgnoreCase))
                    && _selectedQuestionOptions.Any(option => string.Equals(option.Text, "False", StringComparison.OrdinalIgnoreCase)))
                {
                    StatusMessage = "True/False only supports two options: True and False.";
                    return;
                }

                defaultOptionText = _selectedQuestionOptions.Any(option => string.Equals(option.Text, "True", StringComparison.OrdinalIgnoreCase))
                    ? "False"
                    : "True";
            }

            var option = new OptionWrapper { Text = defaultOptionText };
            SubscribeOptionHandler(option);
            _selectedQuestionOptions.Add(option);
            SelectedOption = option;
            UpdateQuestionModelFromUI();
        }

        private void RemoveOption()
        {
            if (SelectedQuestion == null || SelectedOption == null) return;

            var key = ResolveCorrectKeyFromOptionText(SelectedQuestion.Type, SelectedOption.Text);
            if (!string.IsNullOrWhiteSpace(key))
            {
                _selectedQuestionCorrectKeys.Remove(key);
            }

            UnsubscribeOptionHandler(SelectedOption);
            _selectedQuestionOptions.Remove(SelectedOption);
            SelectedOption = null;
            UpdateCorrectKeysTextFromCollection();
            UpdateQuestionModelFromUI();
        }

        private void ToggleCorrectKey()
        {
            if (SelectedQuestion == null || SelectedOption == null) return;

            var key = ResolveCorrectKeyFromOptionText(SelectedQuestion.Type, SelectedOption.Text);
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (_selectedQuestionCorrectKeys.Contains(key))
            {
                _selectedQuestionCorrectKeys.Remove(key);
            }
            else
            {
                _selectedQuestionCorrectKeys.Add(key);
            }

            UpdateCorrectKeysTextFromCollection();
            UpdateQuestionModelFromUI();
        }

        private void UpdateQuestionModelFromUI()
        {
            if (SelectedQuestion == null) return;

            var questionType = NormalizeQuestionType(SelectedQuestion.Type);

            var normalizedCorrectKeys = _selectedQuestionCorrectKeys
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var correctSet = normalizedCorrectKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (string.Equals(questionType, "Matching", StringComparison.OrdinalIgnoreCase))
            {
                var pairs = new List<MatchPair>();
                foreach (var raw in _selectedQuestionOptions
                    .Select(x => x.Text?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    if (!TryParseMatchingPair(raw!, out var left, out var right))
                    {
                        continue;
                    }

                    pairs.Add(new MatchPair { Left = left, Right = right });
                }

                SelectedQuestion.MatchingPairs = pairs;
                SelectedQuestion.DragDrop = null;
                SelectedQuestion.Options = pairs
                    .Select(pair => new Option
                    {
                        Content = $"{pair.Left}|{pair.Right}",
                        IsCorrect = correctSet.Contains(pair.Right)
                    })
                    .ToList();
                SelectedQuestion.CorrectKeys = normalizedCorrectKeys;
                return;
            }

            if (string.Equals(questionType, "DragDrop", StringComparison.OrdinalIgnoreCase))
            {
                var tokens = _selectedQuestionOptions
                    .Select(x => x.Text?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existingSlotNames = SelectedQuestion.DragDrop?.Slots?
                    .Select(slot => slot.Name?.Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!)
                    .ToList() ?? new List<string>();

                var slots = new List<DragSlot>();
                for (int i = 0; i < normalizedCorrectKeys.Count; i++)
                {
                    var slotName = i < existingSlotNames.Count
                        ? existingSlotNames[i]
                        : (i + 1).ToString();
                    slots.Add(new DragSlot
                    {
                        Name = slotName,
                        Answer = normalizedCorrectKeys[i]
                    });
                }

                SelectedQuestion.DragDrop = new DragDropConfig
                {
                    Tokens = tokens,
                    Slots = slots
                };
                SelectedQuestion.MatchingPairs = new List<MatchPair>();
                SelectedQuestion.Options = tokens
                    .Select(token => new Option
                    {
                        Content = token,
                        IsCorrect = correctSet.Contains(token)
                    })
                    .ToList();
                SelectedQuestion.CorrectKeys = normalizedCorrectKeys;
                return;
            }

            if (string.Equals(questionType, "TrueFalse", StringComparison.OrdinalIgnoreCase))
            {
                var trueFalseOptions = new[] { "True", "False" };
                SelectedQuestion.Options = trueFalseOptions
                    .Select(value => new Option
                    {
                        Content = value,
                        IsCorrect = correctSet.Contains(value)
                    })
                    .ToList();
                SelectedQuestion.CorrectKeys = normalizedCorrectKeys;
                SelectedQuestion.MatchingPairs = new List<MatchPair>();
                SelectedQuestion.DragDrop = null;
                return;
            }

            SelectedQuestion.Options = _selectedQuestionOptions
                .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                .Select(x => new Option
                {
                    Content = x.Text.Trim(),
                    IsCorrect = correctSet.Contains(x.Text.Trim())
                })
                .ToList();
            SelectedQuestion.CorrectKeys = normalizedCorrectKeys;
            SelectedQuestion.MatchingPairs = new List<MatchPair>();
            SelectedQuestion.DragDrop = null;
        }

        private void SubscribeOptionHandler(OptionWrapper option)
        {
            option.TextChanged += OnQuestionOptionTextChanged;
        }

        private void UnsubscribeOptionHandler(OptionWrapper option)
        {
            option.TextChanged -= OnQuestionOptionTextChanged;
        }

        private void UnsubscribeOptionHandlers()
        {
            foreach (var option in _selectedQuestionOptions)
            {
                UnsubscribeOptionHandler(option);
            }
        }

        private void StartNewTest()
        {
            SelectedTest = new Test
            {
                Type = "Test",
                DurationMinutes = 30,
                TotalMaxScore = 10m,
                IsPublished = false,
                CreatedBy = CurrentUserName
            };

            StatusMessage = "Creating new test...";
        }

        private void StartNewUser()
        {
            SelectedUser = new User
            {
                Role = "Student",
                Level = "Junior"
            };

            StatusMessage = "Creating new user...";
        }

        private void StartNewQuestion()
        {
            SelectedQuestion = new Question
            {
                Type = "MCQ",
                Status = "Draft",
                DifficultyLevelId = GetDefaultMetadataId(QuestionDifficultyOptions, "Easy"),
                SkillId = GetDefaultMetadataId(QuestionSkillOptions),
                SubjectId = GetDefaultMetadataId(QuestionSubjectOptions),
                QuestionBankId = GetDefaultMetadataId(QuestionBankOptions),
                Options = new List<Option>
                {
                    new Option { Content = "Option A", IsCorrect = true },
                    new Option { Content = "Option B", IsCorrect = false }
                },
                CorrectKeys = new List<string> { "Option A" }
            };

            StatusMessage = string.IsNullOrWhiteSpace(SelectedQuestion.SubjectId) || string.IsNullOrWhiteSpace(SelectedQuestion.QuestionBankId)
                ? "Creating new question... Please select Subject and Question Bank."
                : "Creating new question...";
        }

        private void AddQuestionOptionFromText(string text)
        {
            var option = new OptionWrapper { Text = text };
            SubscribeOptionHandler(option);
            _selectedQuestionOptions.Add(option);
        }

        private void OnQuestionOptionTextChanged(object? sender, OptionTextChangedEventArgs e)
        {
            if (SelectedQuestion == null)
            {
                return;
            }

            var oldKey = ResolveCorrectKeyFromOptionText(SelectedQuestion.Type, e.OldText);
            var newKey = ResolveCorrectKeyFromOptionText(SelectedQuestion.Type, e.NewText);
            var index = IndexOfCorrectKey(oldKey);

            if (index >= 0)
            {
                if (string.IsNullOrWhiteSpace(newKey))
                {
                    _selectedQuestionCorrectKeys.RemoveAt(index);
                }
                else
                {
                    _selectedQuestionCorrectKeys[index] = newKey;
                }
            }

            UpdateCorrectKeysTextFromCollection();
            UpdateQuestionModelFromUI();
        }

        private int IndexOfCorrectKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return -1;
            }

            for (int i = 0; i < _selectedQuestionCorrectKeys.Count; i++)
            {
                if (string.Equals(_selectedQuestionCorrectKeys[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private void SyncCorrectKeysCollectionFromText()
        {
            var parsed = _selectedQuestionCorrectKeysText
                .Split(new[] { '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _selectedQuestionCorrectKeys.Clear();
            foreach (var key in parsed)
            {
                _selectedQuestionCorrectKeys.Add(key);
            }
        }

        private void UpdateCorrectKeysTextFromCollection()
        {
            _syncingCorrectKeysText = true;
            try
            {
                var joined = string.Join(" | ", _selectedQuestionCorrectKeys.Where(x => !string.IsNullOrWhiteSpace(x)));
                if (!string.Equals(_selectedQuestionCorrectKeysText, joined, StringComparison.Ordinal))
                {
                    _selectedQuestionCorrectKeysText = joined;
                    OnPropertyChanged(nameof(SelectedQuestionCorrectKeysText));
                }
            }
            finally
            {
                _syncingCorrectKeysText = false;
            }
        }

        private static string ResolveCorrectKeyFromOptionText(string? questionType, string? optionText)
        {
            if (string.IsNullOrWhiteSpace(optionText))
            {
                return string.Empty;
            }

            var normalizedType = NormalizeQuestionType(questionType);
            var trimmed = optionText.Trim();
            if (string.Equals(normalizedType, "Matching", StringComparison.OrdinalIgnoreCase)
                && TryParseMatchingPair(trimmed, out _, out var right))
            {
                return right;
            }

            return trimmed;
        }

        private static bool TryParseMatchingPair(string raw, out string left, out string right)
        {
            left = string.Empty;
            right = string.Empty;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var parts = raw.Split('|', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            {
                return false;
            }

            left = parts[0];
            right = parts[1];
            return true;
        }

        private static string NormalizeQuestionType(string? type)
            => string.IsNullOrWhiteSpace(type) ? "MCQ" : type.Trim();

        private static bool IsValidEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                _ = new MailAddress(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string? ValidateQuestionForSave(Question question)
        {
            question.Content = (question.Content ?? string.Empty).Trim();
            question.Type = NormalizeQuestionType(question.Type);
            question.SubjectId = (question.SubjectId ?? string.Empty).Trim();
            question.QuestionBankId = (question.QuestionBankId ?? string.Empty).Trim();
            question.SkillId = (question.SkillId ?? string.Empty).Trim();
            question.DifficultyLevelId = (question.DifficultyLevelId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(question.DifficultyLevelId))
            {
                question.DifficultyLevelId = GetDefaultMetadataId(QuestionDifficultyOptions, "Easy");
            }

            if (string.IsNullOrWhiteSpace(question.Content))
            {
                return "Question content is required.";
            }

            if (string.IsNullOrWhiteSpace(question.SubjectId))
            {
                return "Subject is required.";
            }

            if (string.IsNullOrWhiteSpace(question.QuestionBankId))
            {
                return "Question Bank is required.";
            }

            var questionType = NormalizeQuestionType(question.Type);
            var options = question.Options ?? new List<Option>();
            if (string.Equals(questionType, "MCQ", StringComparison.OrdinalIgnoreCase))
            {
                if (options.Count(o => !string.IsNullOrWhiteSpace(o.Content)) < 2)
                {
                    return "MCQ requires at least 2 options.";
                }

                if (!options.Any(o => o.IsCorrect))
                {
                    return "MCQ requires at least 1 correct option.";
                }
            }
            else if (string.Equals(questionType, "TrueFalse", StringComparison.OrdinalIgnoreCase))
            {
                if (options.Count(o => o.IsCorrect) != 1)
                {
                    return "True/False requires exactly 1 correct option.";
                }
            }
            else if (string.Equals(questionType, "Matching", StringComparison.OrdinalIgnoreCase))
            {
                if (question.MatchingPairs == null || question.MatchingPairs.Count == 0)
                {
                    return "Matching question requires at least 1 pair.";
                }
            }
            else if (string.Equals(questionType, "DragDrop", StringComparison.OrdinalIgnoreCase))
            {
                var hasTokens = question.DragDrop?.Tokens?.Any(token => !string.IsNullOrWhiteSpace(token)) == true;
                var hasSlots = question.DragDrop?.Slots?.Any(slot => !string.IsNullOrWhiteSpace(slot.Answer)) == true;
                if (!hasTokens || !hasSlots)
                {
                    return "DragDrop requires tokens and slots.";
                }
            }

            return null;
        }

        private static string GetDefaultMetadataId(IEnumerable<QuestionMetadataItem> items, string? preferredContains = null)
        {
            if (!string.IsNullOrWhiteSpace(preferredContains))
            {
                var match = items.FirstOrDefault(item =>
                    !string.IsNullOrWhiteSpace(item.Id) &&
                    ((!string.IsNullOrWhiteSpace(item.Name) && item.Name.Contains(preferredContains, StringComparison.OrdinalIgnoreCase))
                     || item.Id.Contains(preferredContains, StringComparison.OrdinalIgnoreCase)));
                if (match != null)
                {
                    return match.Id;
                }
            }

            return items.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Id))?.Id ?? string.Empty;
        }

        private void RefreshQuestionMetadataOptions(QuestionMetadataResponse? metadata = null)
        {
            var skillValues = metadata?.Skills?
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(item => new QuestionMetadataItem
                {
                    Id = item.Id.Trim(),
                    Name = string.IsNullOrWhiteSpace(item.Name) ? item.Id.Trim() : item.Name.Trim()
                })
                .ToList();
            if (skillValues == null || skillValues.Count == 0)
            {
                skillValues = Questions
                    .Where(q => !string.IsNullOrWhiteSpace(q.SkillId))
                    .Select(q => new QuestionMetadataItem
                    {
                        Id = q.SkillId.Trim(),
                        Name = string.IsNullOrWhiteSpace(q.Skill) ? q.SkillId.Trim() : q.Skill.Trim()
                    })
                    .ToList();
            }

            var difficultyValues = metadata?.Difficulties?
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(item => new QuestionMetadataItem
                {
                    Id = item.Id.Trim(),
                    Name = string.IsNullOrWhiteSpace(item.Name) ? item.Id.Trim() : item.Name.Trim()
                })
                .ToList();
            if (difficultyValues == null || difficultyValues.Count == 0)
            {
                difficultyValues = Questions
                    .Where(q => !string.IsNullOrWhiteSpace(q.DifficultyLevelId))
                    .Select(q => new QuestionMetadataItem
                    {
                        Id = q.DifficultyLevelId.Trim(),
                        Name = string.IsNullOrWhiteSpace(q.Difficulty) ? q.DifficultyLevelId.Trim() : q.Difficulty.Trim()
                    })
                    .ToList();
            }

            var subjectValues = metadata?.Subjects?
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(item => new QuestionMetadataItem
                {
                    Id = item.Id.Trim(),
                    Name = string.IsNullOrWhiteSpace(item.Name) ? item.Id.Trim() : item.Name.Trim()
                })
                .ToList();
            if (subjectValues == null || subjectValues.Count == 0)
            {
                subjectValues = Questions
                    .Where(q => !string.IsNullOrWhiteSpace(q.SubjectId))
                    .Select(q => new QuestionMetadataItem
                    {
                        Id = q.SubjectId.Trim(),
                        Name = string.IsNullOrWhiteSpace(q.Subject) ? q.SubjectId.Trim() : q.Subject.Trim()
                    })
                    .ToList();
            }

            var questionBankValues = metadata?.QuestionBanks?
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(item => new QuestionMetadataItem
                {
                    Id = item.Id.Trim(),
                    Name = string.IsNullOrWhiteSpace(item.Name) ? item.Id.Trim() : item.Name.Trim()
                })
                .ToList();
            if (questionBankValues == null || questionBankValues.Count == 0)
            {
                questionBankValues = Questions
                    .Where(q => !string.IsNullOrWhiteSpace(q.QuestionBankId))
                    .Select(q => new QuestionMetadataItem
                    {
                        Id = q.QuestionBankId.Trim(),
                        Name = q.QuestionBankId.Trim()
                    })
                    .ToList();
            }

            RefillMetadataCollection(QuestionSkillOptions, skillValues);
            RefillMetadataCollection(QuestionDifficultyOptions, difficultyValues);
            RefillMetadataCollection(QuestionSubjectOptions, subjectValues);
            RefillMetadataCollection(QuestionBankOptions, questionBankValues);

            EnsureQuestionMetadataValues();
        }

        private void EnsureQuestionMetadataValues()
        {
            if (SelectedQuestion == null)
            {
                return;
            }

            EnsureOptionExists(QuestionSkillOptions, SelectedQuestion.SkillId, SelectedQuestion.Skill);
            EnsureOptionExists(QuestionDifficultyOptions, SelectedQuestion.DifficultyLevelId, SelectedQuestion.Difficulty);
            EnsureOptionExists(QuestionSubjectOptions, SelectedQuestion.SubjectId, SelectedQuestion.Subject);
            EnsureOptionExists(QuestionBankOptions, SelectedQuestion.QuestionBankId);
        }

        private static void EnsureOptionExists(ObservableCollection<QuestionMetadataItem> items, string? id, string? name = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            var normalizedId = id.Trim();
            var existing = items.FirstOrDefault(item => string.Equals(item.Id, normalizedId, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                items.Add(new QuestionMetadataItem
                {
                    Id = normalizedId,
                    Name = string.IsNullOrWhiteSpace(name) ? normalizedId : name.Trim()
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(existing.Name) && !string.IsNullOrWhiteSpace(name))
            {
                existing.Name = name.Trim();
            }
        }

        private static void RefillMetadataCollection(ObservableCollection<QuestionMetadataItem> target, IEnumerable<QuestionMetadataItem> values)
        {
            var list = values
                .Where(v => !string.IsNullOrWhiteSpace(v.Id))
                .GroupBy(v => v.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var preferred = group.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Name))
                        ?? group.First();
                    var normalizedId = group.Key.Trim();
                    var normalizedName = string.IsNullOrWhiteSpace(preferred.Name)
                        ? normalizedId
                        : preferred.Name.Trim();
                    return new QuestionMetadataItem
                    {
                        Id = normalizedId,
                        Name = normalizedName
                    };
                })
                .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            target.Clear();
            foreach (var item in list)
            {
                target.Add(item);
            }
        }

        private void SyncTestItems()
        {
            _selectedTestItems.Clear();
            if (SelectedTest != null && SelectedTest.Items != null)
            {
                foreach (var item in SelectedTest.Items.OrderBy(item => item.Order))
                {
                    _selectedTestItems.Add(new TestItem
                    {
                        TestId = item.TestId,
                        QuestionId = item.QuestionId,
                        Points = item.Points,
                        Order = item.Order
                    });
                }
            }
        }

        private void AddTestItem()
        {
            if (SelectedTest == null) return;

            var usedQuestionIds = _selectedTestItems
                .Where(item => !string.IsNullOrWhiteSpace(item.QuestionId))
                .Select(item => item.QuestionId.Trim())
                .ToHashSet(StringComparer.Ordinal);

            var nextQuestion = Questions
                .FirstOrDefault(question =>
                    !string.IsNullOrWhiteSpace(question.Id) &&
                    string.Equals(question.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
                    !usedQuestionIds.Contains(question.Id));
            nextQuestion ??= Questions.FirstOrDefault(question =>
                !string.IsNullOrWhiteSpace(question.Id) &&
                !usedQuestionIds.Contains(question.Id));

            if (nextQuestion == null)
            {
                StatusMessage = "No more questions available to add.";
                return;
            }

            var itm = new TestItem
            {
                TestId = SelectedTest.Id,
                QuestionId = nextQuestion.Id,
                Points = 1m,
                Order = _selectedTestItems.Count
            };
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

            var normalizedItems = _selectedTestItems
                .Where(item => !string.IsNullOrWhiteSpace(item.QuestionId))
                .Select((item, index) => new TestItem
                {
                    TestId = SelectedTest.Id,
                    QuestionId = item.QuestionId.Trim(),
                    Points = item.Points <= 0 ? 1m : item.Points,
                    Order = index
                })
                .ToList();

            SelectedTest.Items = normalizedItems;
            SelectedTest.QuestionIds = normalizedItems.Select(x => x.QuestionId).ToList();
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

        private async Task ExportPeriodicReportAsync()
        {
            try
            {
                var now = DateTime.Now;
                var from = SelectedExportPeriod switch
                {
                    "Weekly" => now.Date.AddDays(-7),
                    "Quarterly" => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
                    _ => new DateTime(now.Year, now.Month, 1)
                };

                var to = now.Date;
                StatusMessage = $"Exporting {SelectedExportPeriod.ToLowerInvariant()} report ({SelectedExportFormat})...";

                byte[]? data = string.Equals(SelectedExportFormat, "pdf", StringComparison.OrdinalIgnoreCase)
                    ? await _apiService.DownloadReportPdfAsync(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"))
                    : await _apiService.DownloadReportXlsxAsync(from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"));

                if (data == null)
                {
                    StatusMessage = "Periodic export failed.";
                    return;
                }

                var ext = string.Equals(SelectedExportFormat, "pdf", StringComparison.OrdinalIgnoreCase) ? "pdf" : "xlsx";
                var fileName = $"PeriodicReport_{SelectedExportPeriod}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                await File.WriteAllBytesAsync(filePath, data);
                StatusMessage = $"Periodic report saved: {fileName}";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var settings = await _apiService.GetSystemSettingsAsync();
                if (settings != null)
                {
                    SystemSettings = settings;
                }
            }
            catch { }
        }

        private async Task LoadAuditAsync()
        {
            try
            {
                var logs = await _apiService.GetAuditLogsAsync(
                    string.IsNullOrWhiteSpace(AuditFrom) ? null : AuditFrom,
                    string.IsNullOrWhiteSpace(AuditTo) ? null : AuditTo,
                    string.IsNullOrWhiteSpace(AuditKeyword) ? null : AuditKeyword,
                    string.IsNullOrWhiteSpace(AuditActor) ? null : AuditActor,
                    500);

                AuditLogs.Clear();
                if (logs != null)
                {
                    foreach (var log in logs) AuditLogs.Add(log);
                }
            }
            catch { }
        }

        private async Task LoadExamSchedulesAsync()
        {
            try
            {
                var list = await _apiService.GetExamSchedulesAsync();
                ExamSchedules.Clear();
                if (list != null)
                {
                    foreach (var item in list) ExamSchedules.Add(item);
                }
            }
            catch { }
        }

        private void AddScheduleDraft()
        {
            if (string.IsNullOrWhiteSpace(SelectedScheduleTestId) ||
                string.IsNullOrWhiteSpace(SelectedScheduleCourseId) ||
                string.IsNullOrWhiteSpace(SelectedScheduleRoom))
            {
                StatusMessage = "Select test/course/room before adding schedule.";
                return;
            }

            if (SelectedScheduleEndTime <= SelectedScheduleStartTime)
            {
                StatusMessage = "End time must be greater than start time.";
                return;
            }

            ScheduleDrafts.Add(new ExamScheduleDraft
            {
                TestId = SelectedScheduleTestId,
                CourseId = SelectedScheduleCourseId,
                Room = SelectedScheduleRoom.Trim(),
                StartTime = SelectedScheduleStartTime,
                EndTime = SelectedScheduleEndTime,
                ExamType = string.IsNullOrWhiteSpace(SelectedScheduleExamType) ? "Final" : SelectedScheduleExamType.Trim()
            });

            StatusMessage = $"Added draft. Queue: {ScheduleDrafts.Count}";
        }

        private void RemoveScheduleDraft()
        {
            if (SelectedScheduleDraft == null) return;
            ScheduleDrafts.Remove(SelectedScheduleDraft);
            StatusMessage = $"Removed draft. Queue: {ScheduleDrafts.Count}";
        }

        private async Task CreateSingleScheduleAsync()
        {
            var draft = new ExamScheduleDraft
            {
                TestId = SelectedScheduleTestId,
                CourseId = SelectedScheduleCourseId,
                Room = SelectedScheduleRoom,
                StartTime = SelectedScheduleStartTime,
                EndTime = SelectedScheduleEndTime,
                ExamType = SelectedScheduleExamType
            };

            try
            {
                var ok = await _apiService.CreateExamScheduleAsync(draft);
                StatusMessage = ok ? "Schedule created." : "Failed to create schedule.";
                if (ok) await LoadExamSchedulesAsync();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task CreateBulkSchedulesAsync()
        {
            if (ScheduleDrafts.Count == 0)
            {
                StatusMessage = "Bulk queue is empty.";
                return;
            }

            try
            {
                var ok = await _apiService.BulkCreateExamSchedulesAsync(ScheduleDrafts.ToList());
                StatusMessage = ok ? "Bulk schedule operation completed." : "Bulk schedule failed.";
                if (ok)
                {
                    ScheduleDrafts.Clear();
                    await LoadExamSchedulesAsync();
                }
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteScheduleAsync()
        {
            if (SelectedExamSchedule == null || string.IsNullOrEmpty(SelectedExamSchedule.Id)) return;
            try
            {
                var ok = await _apiService.DeleteExamScheduleAsync(SelectedExamSchedule.Id);
                StatusMessage = ok ? "Schedule deleted." : "Failed to delete schedule.";
                if (ok) await LoadExamSchedulesAsync();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task ExportScheduleCsvAsync()
        {
            try
            {
                var data = await _apiService.DownloadExamScheduleCsvAsync();
                if (data == null)
                {
                    StatusMessage = "Failed to export exam schedules.";
                    return;
                }

                var fileName = $"exam-schedules-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                await File.WriteAllBytesAsync(filePath, data);
                StatusMessage = $"Exam schedules exported: {fileName}";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task PreviewYearEndAsync()
        {
            if (string.IsNullOrWhiteSpace(YearEndAcademicYear))
            {
                StatusMessage = "Academic year is required for preview.";
                return;
            }

            try
            {
                StatusMessage = "Loading year-end preview...";
                var preview = await _apiService.GetYearEndPreviewAsync(
                    YearEndAcademicYear.Trim(),
                    string.IsNullOrWhiteSpace(YearEndFacultyId) ? null : YearEndFacultyId);

                if (preview == null)
                {
                    StatusMessage = "Failed to load year-end preview.";
                    return;
                }

                YearEndPreview = preview;
                YearEndStudents.Clear();
                foreach (var row in preview.Students)
                {
                    YearEndStudents.Add(row);
                }

                YearEndWarningStudents.Clear();
                foreach (var row in preview.WarningStudents)
                {
                    YearEndWarningStudents.Add(row);
                }

                CommandManager.InvalidateRequerySuggested();
                StatusMessage = $"Year-end preview loaded. Students: {preview.TotalStudents}. Warning/Fail: {preview.WarningStudents.Count}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task FinalizeYearEndAsync()
        {
            if (string.IsNullOrWhiteSpace(YearEndAcademicYear))
            {
                StatusMessage = "Academic year is required for finalization.";
                return;
            }

            try
            {
                StatusMessage = "Finalizing year-end...";
                var result = await _apiService.FinalizeYearEndAsync(
                    YearEndAcademicYear.Trim(),
                    string.IsNullOrWhiteSpace(YearEndFacultyId) ? null : YearEndFacultyId);

                if (result == null)
                {
                    StatusMessage = "Year-end finalization failed.";
                    return;
                }

                StatusMessage = result.Success
                    ? $"Year-end finalized for {result.AcademicYear}. Students: {result.FinalizedStudents}."
                    : (result.Messages.FirstOrDefault() ?? "Year-end finalization rejected.");

                await PreviewYearEndAsync();
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task LoadBackupsAsync()
        {
            try
            {
                var backups = await _apiService.GetBackupsAsync();
                BackupFiles.Clear();
                if (backups != null)
                {
                    foreach (var backup in backups) BackupFiles.Add(backup);
                }
            }
            catch { }
        }

        private async Task CreateBackupAsync()
        {
            try
            {
                StatusMessage = "Creating database backup...";
                var result = await _apiService.CreateBackupAsync();
                StatusMessage = result?.Message ?? "Backup completed.";
                await LoadBackupsAsync();
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task RestoreBackupAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "SQL Backup (*.bak)|*.bak",
                Title = "Choose backup file"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                StatusMessage = "Restoring backup...";
                var result = await _apiService.RestoreBackupAsync(dialog.FileName);
                StatusMessage = result?.Message ?? "Restore completed.";
                await LoadBackupsAsync();
                await LoadDataAsync();
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

        private async Task SaveCourseAsync()
        {
            if (SelectedCourse == null) return;
            try
            {
                StatusMessage = "Saving course...";
                bool success = string.IsNullOrEmpty(SelectedCourse.Id)
                    ? await _apiService.CreateCourseAsync(SelectedCourse)
                    : await _apiService.UpdateCourseAsync(SelectedCourse.Id, SelectedCourse);
                
                if (success) { StatusMessage = "Course saved."; await LoadDataAsync(); }
                else StatusMessage = "Failed to save course.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task DeleteCourseAsync()
        {
            if (SelectedCourse == null || string.IsNullOrEmpty(SelectedCourse.Id)) return;
            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete course '{SelectedCourse.Name}'?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                StatusMessage = "Deleting course...";
                if (await _apiService.DeleteCourseAsync(SelectedCourse.Id)) { StatusMessage = "Course deleted."; await LoadDataAsync(); }
                else StatusMessage = "Failed to delete course.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task LoadEnrollmentsAsync()
        {
            if (SelectedCourse == null || string.IsNullOrEmpty(SelectedCourse.Id))
            {
                Enrollments.Clear();
                return;
            }
            try
            {
                var list = await _apiService.GetEnrollmentsAsync(SelectedCourse.Id);
                Enrollments.Clear();
                if (list != null) foreach (var e in list) Enrollments.Add(e);
            }
            catch { }
        }

        private async Task EnrollStudentAsync()
        {
            if (SelectedCourse == null) return;
            StatusMessage = "Use 'Import Students' to enroll many students at once.";
            await Task.Yield();
        }

        private async Task UnenrollStudentAsync()
        {
            if (SelectedEnrollment == null || SelectedCourse == null) return;
            try
            {
                StatusMessage = "Unenrolling student...";
                if (await _apiService.UnenrollStudentAsync(SelectedEnrollment.StudentId, SelectedCourse.Id))
                {
                    StatusMessage = "Student unenrolled.";
                    await LoadEnrollmentsAsync();
                }
                else StatusMessage = "Failed to unenroll.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
        }

        private async Task ImportAsync(string type)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = $"Import {type}"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    StatusMessage = $"Importing {type}...";
                    bool success = type == "students" 
                        ? await _apiService.ImportStudentsAsync(openFileDialog.FileName)
                        : await _apiService.ImportCoursesAsync(openFileDialog.FileName);
                    
                    if (success) { StatusMessage = $"Import {type} successful."; await LoadDataAsync(); }
                    else StatusMessage = $"Import {type} failed.";
                }
                catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                StatusMessage = "Saving settings...";
                var ok = await _apiService.SaveSystemSettingsAsync(SystemSettings);
                StatusMessage = ok ? "Settings saved." : "Failed to save settings.";
                if (ok) await LoadSettingsAsync();
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
            set
            {
                if (string.Equals(_text, value, StringComparison.Ordinal))
                {
                    return;
                }

                var oldText = _text;
                _text = value;
                TextChanged?.Invoke(this, new OptionTextChangedEventArgs(oldText, value ?? string.Empty));
                OnPropertyChanged();
            }
        }

        public event EventHandler<OptionTextChangedEventArgs>? TextChanged;
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class OptionTextChangedEventArgs : EventArgs
    {
        public OptionTextChangedEventArgs(string oldText, string newText)
        {
            OldText = oldText;
            NewText = newText;
        }

        public string OldText { get; }
        public string NewText { get; }
    }
}
