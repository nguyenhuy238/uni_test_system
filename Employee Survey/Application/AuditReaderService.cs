using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Employee_Survey.Application
{
    public class AuditEntryDto
    {
        public DateTime At { get; set; }
        public string Actor { get; set; } = "";
        public string Action { get; set; } = "";
        public string EntityId { get; set; } = "";
        public JsonElement? Before { get; set; }
        public JsonElement? After { get; set; }
    }

    public interface IAuditReaderService
    {
        Task<List<AuditEntryDto>> GetAllAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string? keyword = null, string? actor = null);
    }

    public class AuditReaderService : IAuditReaderService
    {
        private readonly string _path;
        public AuditReaderService(IConfiguration cfg)
        {
            var folder = cfg["DataFolder"] ?? "App_Data";
            Directory.CreateDirectory(folder);
            _path = Path.Combine(folder, "AuditLog.json");
            if (!File.Exists(_path)) File.WriteAllText(_path, "[]");
        }

        public async Task<List<AuditEntryDto>> GetAllAsync(DateTime? fromUtc = null, DateTime? toUtc = null, string? keyword = null, string? actor = null)
        {
            var json = await File.ReadAllTextAsync(_path);
            var list = JsonSerializer.Deserialize<List<AuditEntryDto>>(json) ?? new();

            if (fromUtc.HasValue) list = list.Where(x => x.At >= fromUtc.Value).ToList();
            if (toUtc.HasValue) list = list.Where(x => x.At <= toUtc.Value).ToList();
            if (!string.IsNullOrWhiteSpace(keyword))
                list = list.Where(x => (x.Action?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                                     || (x.EntityId?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            if (!string.IsNullOrWhiteSpace(actor))
                list = list.Where(x => string.Equals(x.Actor, actor, StringComparison.OrdinalIgnoreCase)).ToList();

            return list.OrderByDescending(x => x.At).ToList();
        }
    }
}
