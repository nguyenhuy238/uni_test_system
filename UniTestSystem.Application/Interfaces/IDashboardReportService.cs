using UniTestSystem.Application.Models;
using UniTestSystem.Domain;

namespace UniTestSystem.Application.Interfaces;

public interface IDashboardReportService
{
    Task<WidgetDashboardVm> GetWidgetDashboardAsync(DateTime fromUtc, DateTime toUtc, Role actorRole, string? actorUserId);
}
