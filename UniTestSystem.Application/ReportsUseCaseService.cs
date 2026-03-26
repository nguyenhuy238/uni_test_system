using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application;

public sealed class ReportsUseCaseService : IReportsUseCaseService
{
    private readonly IFacultyReportService _facultyReportService;
    private readonly IDashboardReportService _dashboardReportService;
    private readonly IQuestionAnalyticsService _questionAnalyticsService;
    private readonly ILecturerPerformanceService _lecturerPerformanceService;
    private readonly IReportExportService _exportService;
    private readonly ISettingsService _settingsService;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Course> _courseRepo;

    public ReportsUseCaseService(
        IFacultyReportService facultyReportService,
        IDashboardReportService dashboardReportService,
        IQuestionAnalyticsService questionAnalyticsService,
        ILecturerPerformanceService lecturerPerformanceService,
        IReportExportService exportService,
        ISettingsService settingsService,
        IRepository<User> userRepo,
        IRepository<Course> courseRepo)
    {
        _facultyReportService = facultyReportService;
        _dashboardReportService = dashboardReportService;
        _questionAnalyticsService = questionAnalyticsService;
        _lecturerPerformanceService = lecturerPerformanceService;
        _exportService = exportService;
        _settingsService = settingsService;
        _userRepo = userRepo;
        _courseRepo = courseRepo;
    }

    public async Task<ReportsIndexVm> GetIndexVmAsync(DateTime fromUtc, DateTime toUtc, Role actorRole, string? actorUserId)
    {
        var facultyVm = await _facultyReportService.GetFacultyReportAsync(fromUtc, toUtc);
        var yearVm = await _facultyReportService.GetAcademicYearReportAsync(fromUtc, toUtc);
        var dashboardVm = await _dashboardReportService.GetWidgetDashboardAsync(fromUtc, toUtc, actorRole, actorUserId);

        return new ReportsIndexVm
        {
            Faculty = facultyVm,
            AcademicYear = yearVm,
            Dashboard = dashboardVm
        };
    }

    public Task<QuestionAnalyticsVm> GetQuestionAnalyticsVmAsync(DateTime fromUtc, DateTime toUtc, string? courseId, int minAttempts)
    {
        return _questionAnalyticsService.GetQuestionAnalyticsAsync(fromUtc, toUtc, courseId, minAttempts);
    }

    public Task<LecturerPerformanceVm> GetLecturerPerformanceVmAsync(DateTime fromUtc, DateTime toUtc, string? lecturerId)
    {
        return _lecturerPerformanceService.GetLecturerPerformanceReportAsync(fromUtc, toUtc, lecturerId);
    }

    public Task<StudentSubjectReportVm> GetStudentSubjectVmAsync(string userId, DateTime fromUtc, DateTime toUtc)
    {
        return _facultyReportService.GetStudentSubjectReportAsync(userId, fromUtc, toUtc);
    }

    public async Task<List<Course>> GetActiveCoursesAsync()
    {
        return (await _courseRepo.GetAllAsync(c => !c.IsDeleted))
            .OrderBy(c => c.Name)
            .ToList();
    }

    public async Task<List<User>> GetActiveLecturersAsync()
    {
        return (await _userRepo.GetAllAsync(x => x.Role == Role.Lecturer && x.IsActive))
            .OrderBy(x => x.Name)
            .ToList();
    }

    public Task<User?> GetUserByIdAsync(string userId)
    {
        return _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
    }

    public async Task<byte[]> ExportFacultyYearExcelAsync(DateTime fromUtc, DateTime toUtc)
    {
        var facultyVm = await _facultyReportService.GetFacultyReportAsync(fromUtc, toUtc);
        var yearVm = await _facultyReportService.GetAcademicYearReportAsync(fromUtc, toUtc);
        return _exportService.ExportFacultyYearExcel(facultyVm, yearVm, fromUtc, toUtc);
    }

    public async Task<byte[]> ExportFacultyYearPdfAsync(DateTime fromUtc, DateTime toUtc)
    {
        var facultyVm = await _facultyReportService.GetFacultyReportAsync(fromUtc, toUtc);
        var yearVm = await _facultyReportService.GetAcademicYearReportAsync(fromUtc, toUtc);
        var settings = await _settingsService.GetAsync();
        return _exportService.ExportFacultyYearPdf(facultyVm, yearVm, settings, fromUtc, toUtc);
    }

    public async Task<byte[]> ExportStudentSubjectExcelAsync(string userId, DateTime fromUtc, DateTime toUtc)
    {
        var vm = await _facultyReportService.GetStudentSubjectReportAsync(userId, fromUtc, toUtc);
        var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
        return _exportService.ExportStudentSubjectExcel(vm, user?.Name ?? userId, fromUtc, toUtc);
    }

    public async Task<byte[]> ExportStudentSubjectPdfAsync(string userId, DateTime fromUtc, DateTime toUtc)
    {
        var vm = await _facultyReportService.GetStudentSubjectReportAsync(userId, fromUtc, toUtc);
        var user = await _userRepo.FirstOrDefaultAsync(x => x.Id == userId);
        var settings = await _settingsService.GetAsync();
        return _exportService.ExportStudentSubjectPdf(vm, user?.Name ?? userId, settings, fromUtc, toUtc);
    }
}
