using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PRM.Application.Interfaces.Common;
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

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

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
