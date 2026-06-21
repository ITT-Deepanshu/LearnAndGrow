using PRM.Domain.Enums;

namespace PRM.Application.Interfaces.Common;

public interface ICurrentUser
{
    long? UserId { get; }
    string? Username { get; }
    UserRole? Role { get; }
    IReadOnlySet<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}
