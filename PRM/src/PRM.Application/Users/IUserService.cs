using PRM.Application.Users;
using PRM.Domain.Enums;

namespace PRM.Application.Users;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserDto dto, UserRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserListItemDto>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default);
    Task ResetUserPasswordAsync(long userId, ResetUserPasswordDto dto, CancellationToken cancellationToken = default);
    Task DeactivateUserAsync(long userId, CancellationToken cancellationToken = default);
    Task ReactivateUserAsync(long userId, CancellationToken cancellationToken = default);
}
