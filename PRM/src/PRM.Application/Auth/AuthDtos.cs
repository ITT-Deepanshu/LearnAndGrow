namespace PRM.Application.Auth;

public sealed record LoginDto(string Username, string Password);
public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
public sealed record LoginResultDto(
    string AccessToken,
    bool RequiresPasswordChange,
    string Role,
    string FullName,
    long? ResourceProfileId);
public sealed record MeDto(
    long Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    bool RequiresPasswordChange,
    long? ResourceProfileId);
