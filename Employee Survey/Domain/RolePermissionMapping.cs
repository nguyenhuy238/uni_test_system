using System;
using System.Collections.Generic;

namespace Employee_Survey.Domain
{
    public class RolePermissionMapping
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public Role Role { get; set; }
        public List<string> Permissions { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
    }

    public static class PermissionCodes
    {
        public const string Reports_View = "Reports.View";
        public const string Reports_Export = "Reports.Export";
        public const string Settings_Edit = "Settings.Edit";
        public const string Perms_Manage = "Permissions.Manage";
        public const string Audit_View = "Audit.View";

        // NEW – quản lý tổ chức
        public const string Org_View = "Org.View";
        public const string Org_Manage = "Org.Manage";

        // NEW – tests
        public const string Tests_View = "Tests.View";
        public const string Tests_Submit = "Tests.Submit";

        public static readonly string[] All =
        {
            Reports_View, Reports_Export, Settings_Edit, Perms_Manage, Audit_View,
            Org_View, Org_Manage, Tests_View, Tests_Submit
        };
    }
}
