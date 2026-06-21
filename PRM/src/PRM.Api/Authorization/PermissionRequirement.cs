using Microsoft.AspNetCore.Authorization;

namespace PRM.Api.Authorization;

public sealed class PermissionRequirement(IReadOnlyList<string> permissions, bool requireAll = true) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
    public bool RequireAll { get; } = requireAll;
}
