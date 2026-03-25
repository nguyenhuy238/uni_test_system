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
        services.AddScoped<ReportService>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<EmailVerificationService>();
        services.AddScoped<ExamAccessTokenService>();
        services.AddScoped<SessionDeviceGuardService>();

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IGradingService, GradingService>();
        services.AddScoped<IExamScheduleService, ExamScheduleService>();
        services.AddScoped<ITranscriptService, TranscriptService>();
        services.AddScoped<ITestGenerationService, TestGenerationService>();
        services.AddScoped<IAcademicService, AcademicService>();
        services.AddScoped<IBulkImportService, BulkImportService>();

        return services;
    }
}
