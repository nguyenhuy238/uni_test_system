using UniTestSystem.Application.Models;

namespace UniTestSystem.Application.Interfaces;

public interface ILecturerPerformanceService
{
    Task<LecturerPerformanceVm> GetLecturerPerformanceReportAsync(DateTime fromUtc, DateTime toUtc, string? lecturerId = null);
}
