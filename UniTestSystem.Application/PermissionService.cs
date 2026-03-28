using UniTestSystem.Domain;
using UniTestSystem.Application.Interfaces;
using System.Security.Claims;

namespace UniTestSystem.Application
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
            var presets = new List<RolePermissionMapping>
            {
                new() { Role = Role.Admin, Permissions = PermissionCodes.All.ToList() },
                new() { Role = Role.Lecturer, Permissions = new() {
                        PermissionCodes.Reports_View, PermissionCodes.Reports_Export,
                        PermissionCodes.Question_View, PermissionCodes.Question_Create, PermissionCodes.Question_Edit, PermissionCodes.Question_Delete, PermissionCodes.Question_Approve, PermissionCodes.Question_Categorize,
                        PermissionCodes.Tests_View, PermissionCodes.Tests_Create, PermissionCodes.Tests_Publish,
                        PermissionCodes.Exam_Schedule, PermissionCodes.Grading_Manual,
                        PermissionCodes.Analytics_Difficulty
                    }
                },
                new() { Role = Role.Staff, Permissions = new() {
                        PermissionCodes.Users_Manage,
                        PermissionCodes.Reports_View, PermissionCodes.Reports_Export,
                        PermissionCodes.Org_View, PermissionCodes.Org_Manage,
                        PermissionCodes.Courses_Manage, PermissionCodes.Enrollment_Manage,
                        PermissionCodes.Transcript_View, PermissionCodes.Transcript_Manage, PermissionCodes.Transcript_Lock,
                        PermissionCodes.Exam_Schedule, PermissionCodes.Exam_Lock, PermissionCodes.Exam_Session_Reset,
                        PermissionCodes.Question_View, PermissionCodes.Question_Approve,
                        PermissionCodes.Tests_View, PermissionCodes.Tests_Publish,
                        PermissionCodes.Grading_Review,
                        PermissionCodes.Analytics_GPA
                    }
                },
                new() { Role = Role.Student, Permissions = new() {
                        PermissionCodes.Tests_View, PermissionCodes.Tests_Submit,
                        PermissionCodes.Transcript_View
                    }
                },
            };

            foreach (var preset in presets)
            {
                var existing = await GetByRoleAsync(preset.Role);
                if (existing == null)
                {
                    await _repo.InsertAsync(preset);
                }
                else
                {
                    // Nếu preset có permission nào mới mà existing chưa có -> update
                    var missing = preset.Permissions.Except(existing.Permissions).ToList();
                    if (missing.Any())
                    {
                        existing.Permissions.AddRange(missing);
                        existing.Permissions = existing.Permissions.Distinct().ToList();
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.UpdatedBy = "System (Auto Fix)";
                        await _repo.UpsertAsync(x => x.Id == existing.Id, existing);
                    }
                }
            }
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
