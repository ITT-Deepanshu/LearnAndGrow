namespace PRM.Application.Features.Users.Dtos;

public sealed record CreateUserDto(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    string Role);

public sealed record UserDto(
    long Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    bool ForcePasswordChange);

public sealed record UserListItemDto(
    long Id,
    string Username,
    string Role,
    bool IsActive);

public sealed record ResetUserPasswordDto(string NewPassword, string ConfirmPassword);
