using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace UniTestSystem.Infrastructure.Persistence
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;
        private readonly JsonSerializerOptions _opt = new() { WriteIndented = true };

        public AuditService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string actor, string action, string entityId, object? before, object? after)
        {
            var entry = new AuditEntry
            {
                At = DateTime.UtcNow,
                Actor = actor,
                Action = action,
                EntityId = entityId,
                Before = before != null ? JsonSerializer.Serialize(before, _opt) : null,
                After = after != null ? JsonSerializer.Serialize(after, _opt) : null
            };

            _db.AuditEntries.Add(entry);
            await _db.SaveChangesAsync();
        }
    }
}
