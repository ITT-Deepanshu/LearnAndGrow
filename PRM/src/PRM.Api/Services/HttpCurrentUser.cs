using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PRM.Application.Interfaces.Common;
using PRM.Domain.Constants;
using PRM.Domain.Enums;

namespace PRM.Api.Services;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public long? UserId
    {
        get
        {
            var id = FindClaimValue(ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub);
            return long.TryParse(id, out var userId) ? userId : null;
        }
    }

    public string? Username =>
        FindClaimValue(ClaimTypes.Name, JwtRegisteredClaimNames.UniqueName, "name", "preferred_username");

    public UserRole? Role
    {
        get
        {
            var role = FindClaimValue(ClaimTypes.Role, "role");
            if (string.IsNullOrWhiteSpace(role))
                return null;

            return role.ToLowerInvariant() switch
            {
                "admin" => UserRole.Admin,
                "manager" => UserRole.Manager,
                "resource" => UserRole.Resource,
                _ => Enum.TryParse<UserRole>(role, true, out var parsed) ? parsed : null
            };
        }
    }

    public IReadOnlySet<string> Permissions
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null)
                return EmptyPermissions;

            return user.FindAll(PrmClaimTypes.Permission)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission) =>
        !string.IsNullOrWhiteSpace(permission) && Permissions.Contains(permission);

    private static readonly HashSet<string> EmptyPermissions = new(StringComparer.OrdinalIgnoreCase);

    private string? FindClaimValue(params string[] claimTypes)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null)
            return null;

        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }
}
