using UniTestSystem.Domain;
using Microsoft.EntityFrameworkCore;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Infrastructure.Persistence
{
    public class SettingsService : ISettingsService
    {
        private readonly AppDbContext _db;

        public SettingsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<SystemSettings> GetAsync()
        {
            var settings = await _db.SystemSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SystemSettings();
                _db.SystemSettings.Add(settings);
                await _db.SaveChangesAsync();
            }
            return settings;
        }

        public async Task SaveAsync(SystemSettings s)
        {
            var existing = await _db.SystemSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (existing == null)
            {
                s.UpdatedAt = DateTime.UtcNow;
                _db.SystemSettings.Add(s);
            }
            else
            {
                // Simple approach: replace values
                _db.Entry(existing).CurrentValues.SetValues(s);
                existing.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
        }
    }
}
