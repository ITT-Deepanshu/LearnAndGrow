using PRM.Domain.Enums;

namespace PRM.Application.Interfaces.Common;

public interface ICurrentUser
{
    long? UserId { get; }
    string? Username { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
