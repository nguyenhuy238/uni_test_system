using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Infrastructure.Persistence;

public sealed class SystemMaintenanceService : ISystemMaintenanceService
{
    private readonly IServiceProvider _serviceProvider;

    public SystemMaintenanceService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ResetDatabaseAsync(bool reseed = true)
    {
        await Seeder.ResetDatabaseAsync(_serviceProvider, reseed);
    }
}
