namespace UniTestSystem.Application.Interfaces;

public interface ISystemMaintenanceService
{
    Task ResetDatabaseAsync(bool reseed = true);
}
