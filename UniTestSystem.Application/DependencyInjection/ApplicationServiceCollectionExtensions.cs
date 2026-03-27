using Microsoft.Extensions.DependencyInjection;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<AuthService>();
        services.AddScoped<TestService>();
        services.AddScoped<AssessmentService>();
        services.AddScoped<IFacultyReportService, FacultyReportService>();
        services.AddScoped<IDashboardReportService, DashboardReportService>();
        services.AddScoped<IQuestionAnalyticsService, QuestionAnalyticsService>();
        services.AddScoped<ILecturerPerformanceService, LecturerPerformanceService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<EmailVerificationService>();
        services.AddScoped<ExamAccessTokenService>();
        services.AddScoped<SessionDeviceGuardService>();
        services.AddScoped<ISessionService, SessionService>();

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IGradingService, GradingService>();
        services.AddScoped<IExamScheduleService, ExamScheduleService>();
        services.AddScoped<ITranscriptService, TranscriptService>();
        services.AddScoped<ITestGenerationService, TestGenerationService>();
        services.AddScoped<IAcademicService, AcademicService>();
        services.AddScoped<IBulkImportService, BulkImportService>();
        services.AddScoped<ITestAdministrationService, TestAdministrationService>();
        services.AddScoped<IUserTestService, UserTestService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IResultsService, ResultsService>();
        services.AddScoped<IReportsUseCaseService, ReportsUseCaseService>();
        services.AddScoped(typeof(IEntityStore<>), typeof(EntityStore<>));

        return services;
    }
}
