using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using System.Security.Claims;

namespace Employee_Survey.Application
{
    public interface IPermissionService
    {
        Task EnsureDefaultAsync();
        Task<List<RolePermissionMapping>> GetAllAsync();
        Task<RolePermissionMapping?> GetByRoleAsync(Role role);
        Task UpsertAsync(Role role, IEnumerable<string> permissions, string actor);
        Task<bool> HasAsync(ClaimsPrincipal user, string permissionCode);
    }

    public class PermissionService : IPermissionService
    {
        private readonly IRepository<RolePermissionMapping> _repo;
        public PermissionService(IRepository<RolePermissionMapping> repo) => _repo = repo;

        public async Task EnsureDefaultAsync()
        {
            var all = await _repo.GetAllAsync();
            if (all.Any()) return;

            var presets = new List<RolePermissionMapping>
            {
                new() { Role = Role.Admin,   Permissions = PermissionCodes.All.ToList() },
                new() { Role = Role.Staff,      Permissions = new(){
                        PermissionCodes.Reports_View, PermissionCodes.Reports_Export,
                        PermissionCodes.Audit_View,
                        PermissionCodes.Org_View, PermissionCodes.Org_Manage
                    }
                },
                new() { Role = Role.User, Permissions = new(){
                        PermissionCodes.Tests_View, PermissionCodes.Tests_Submit
                    }
                },
            };
            foreach (var m in presets) await _repo.InsertAsync(m);
        }

        public Task<List<RolePermissionMapping>> GetAllAsync() => _repo.GetAllAsync();
        public Task<RolePermissionMapping?> GetByRoleAsync(Role role) => _repo.FirstOrDefaultAsync(x => x.Role == role);

        public async Task UpsertAsync(Role role, IEnumerable<string> permissions, string actor)
        {
            var now = DateTime.UtcNow;
            var existing = await GetByRoleAsync(role);
            if (existing == null)
            {
                existing = new RolePermissionMapping
                {
                    Role = role,
                    Permissions = permissions.Distinct().ToList(),
                    UpdatedAt = now,
                    UpdatedBy = actor
                };
                await _repo.InsertAsync(existing);
            }
            else
            {
                existing.Permissions = permissions.Distinct().ToList();
                existing.UpdatedAt = now; existing.UpdatedBy = actor;
                await _repo.UpsertAsync(x => x.Id == existing.Id, existing);
            }
        }

        public async Task<bool> HasAsync(ClaimsPrincipal user, string permissionCode)
        {
            if (user?.Identity?.IsAuthenticated != true) return false;
            var roleClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            if (!Enum.TryParse<Role>(roleClaim, out var role)) return false;
            var map = await GetByRoleAsync(role);
            return map?.Permissions?.Contains(permissionCode) == true;
        }
    }
}
