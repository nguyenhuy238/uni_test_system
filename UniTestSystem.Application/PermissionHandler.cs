using Microsoft.AspNetCore.Authorization;
using UniTestSystem.Domain;
using System.Security.Claims;

namespace UniTestSystem.Application
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }

    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;

        public PermissionHandler(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User == null) return;

            // Admin bypass (optional, but common in enterprise systems)
            if (context.User.IsInRole(Role.Admin.ToString()))
            {
                context.Succeed(requirement);
                return;
            }

            if (await _permissionService.HasAsync(context.User, requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }
}
