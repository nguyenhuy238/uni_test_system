using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace UniTestSystem.Infrastructure.Persistence
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _db;
        private readonly JsonSerializerOptions _opt = new()
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AuditService(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(string actor, string action, string entityName, string entityId, object? before, object? after)
        {
            var entry = new AuditEntry
            {
                At = DateTime.UtcNow,
                Actor = actor,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Before = SerializeForAudit(before),
                After = SerializeForAudit(after)
            };

            _db.AuditEntries.Add(entry);
            await _db.SaveChangesAsync();
        }

        private string? SerializeForAudit(object? value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Serialize(value, _opt);
            }
            catch (Exception ex)
            {
                var fallback = new
                {
                    Type = value.GetType().FullName,
                    Error = ex.Message
                };

                return JsonSerializer.Serialize(fallback, _opt);
            }
        }
    }
}
