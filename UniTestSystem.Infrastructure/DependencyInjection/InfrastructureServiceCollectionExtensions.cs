using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Infrastructure.Persistence;
using UniTestSystem.Infrastructure.Services;

namespace UniTestSystem.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditReaderService, AuditReaderService>();
        services.AddScoped<IQuestionExcelService, QuestionExcelService>();
        services.AddScoped<ISettingsService, SettingsService>();

        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<INotificationService, NotificationService>();

        return services;
    }
}
