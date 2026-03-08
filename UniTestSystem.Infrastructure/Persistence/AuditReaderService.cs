using Microsoft.EntityFrameworkCore;
using UniTestSystem.Application.Interfaces;

namespace UniTestSystem.Infrastructure.Persistence
{
    public class AuditReaderService : IAuditReaderService
    {
        private readonly AppDbContext _db;
        public AuditReaderService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<AuditEntryDto>> GetAllAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string? keyword = null, string? actor = null)
        {
            var query = _db.AuditEntries.AsQueryable();

            if (fromUtc.HasValue) query = query.Where(x => x.At >= fromUtc.Value);
            if (toUtc.HasValue) query = query.Where(x => x.At <= toUtc.Value);
            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.Action.Contains(keyword) || x.EntityId.Contains(keyword));
            if (!string.IsNullOrWhiteSpace(actor))
                query = query.Where(x => x.Actor == actor);

            return await query.OrderByDescending(x => x.At)
                .Select(x => new AuditEntryDto
                {
                    Id = x.Id,
                    At = x.At,
                    Actor = x.Actor,
                    EntityName = x.EntityName,
                    EntityId = x.EntityId,
                    Before = x.Before,
                    After = x.After
                })
                .ToListAsync();
        }
    }
}
