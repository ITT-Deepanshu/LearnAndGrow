namespace PRM.Application.Features.Auth.Dtos;

public sealed record LoginDto(string Username, string Password);
public sealed record RefreshDto(string RefreshToken);
public sealed record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmPassword);
public sealed record LoginResultDto(
    string AccessToken,
    string RefreshToken,
    bool ForcePasswordChange,
    string Role,
    string FullName);
public sealed record MeDto(long Id, string Username, string Email, string FullName, string Role, bool ForcePasswordChange);
