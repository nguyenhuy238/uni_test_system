using System.Threading.Tasks;

namespace UniTestSystem.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string actor, string action, string entityName, string entityId, object? before, object? after);
    }
}
