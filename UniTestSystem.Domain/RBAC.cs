using System.ComponentModel.DataAnnotations;

namespace UniTestSystem.Domain
{
    public class Permission
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        [Required]
        public string Name { get; set; } = ""; // e.g. "Question.Create"
        public string? Description { get; set; }
    }

    public class UserRole
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        public string UserId { get; set; } = "";
        public virtual User? User { get; set; }
        
        public string RoleName { get; set; } = ""; // Student, Lecturer, Admin
    }

    public class RolePermission
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        
        public string RoleName { get; set; } = "";
        
        public string PermissionId { get; set; } = "";
        public virtual Permission? Permission { get; set; }
    }
}
