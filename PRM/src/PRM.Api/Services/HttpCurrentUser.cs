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
            var id = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(id, out var userId) ? userId : null;
        }
    }

    public string? Username => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public UserRole? Role
    {
        get
        {
            var role = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return role is not null && Enum.TryParse<UserRole>(role, true, out var parsed) ? parsed : null;
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
