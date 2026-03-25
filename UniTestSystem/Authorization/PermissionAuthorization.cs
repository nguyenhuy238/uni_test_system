using Microsoft.AspNetCore.Authorization;
using UniTestSystem.Application;
using UniTestSystem.Domain;

namespace UniTestSystem.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
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
        if (context.User == null)
        {
            return;
        }

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
