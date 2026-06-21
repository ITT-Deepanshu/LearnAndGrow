using Microsoft.AspNetCore.Authorization;
using PRM.Domain.Constants;

namespace PRM.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var granted = context.User
            .FindAll(PrmClaimTypes.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var satisfied = requirement.RequireAll
            ? requirement.Permissions.All(granted.Contains)
            : requirement.Permissions.Any(granted.Contains);

        if (satisfied)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
