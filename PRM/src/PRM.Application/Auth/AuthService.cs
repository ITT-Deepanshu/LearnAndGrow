using PRM.Application.Common;
using PRM.Application.Auth;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Application.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IAuthService
{
    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByUsernameAsync(dto.Username, cancellationToken);
        if (user is null)
            return Result.Failure<LoginResultDto>(AuthErrors.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResultDto>(AuthErrors.AccountDisabled);

        if (!passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            await TryRecordLoginFailureAsync(user, cancellationToken);
            return Result.Failure<LoginResultDto>(AuthErrors.InvalidCredentials);
        }

        user.RecordLogin(clock.UtcNow);
        var accessToken = tokenService.GenerateAccessToken(user);
        auditLogRepository.Add(AuditLog.Create(user.Id, "LOGIN_SUCCESS", nameof(User), user.Id, null, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResultDto(
            accessToken,
            user.RequiresPasswordChange,
            user.Role.RoleName.ToUpperInvariant(),
            user.ResourceProfile?.FullName ?? user.Username,
            user.ResourceProfile?.Id));
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return;

        auditLogRepository.Add(AuditLog.Create(
            currentUser.UserId,
            "LOGOUT",
            nameof(User),
            currentUser.UserId,
            currentUser.Username,
            clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<LoginResultDto> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!user.RequiresPasswordChange)
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword) || !passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new ConflictException("Current password is incorrect.");
        }

        user.ChangePassword(passwordHasher.Hash(dto.NewPassword), user.Id, clock.UtcNow);
        auditLogRepository.Add(AuditLog.Create(user.Id, "PASSWORD_CHANGED", nameof(User), user.Id, null, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await userRepository.GetByIdWithDetailsAsync(user.Id, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return new LoginResultDto(
            tokenService.GenerateAccessToken(refreshed),
            refreshed.RequiresPasswordChange,
            refreshed.Role.RoleName.ToUpperInvariant(),
            refreshed.ResourceProfile?.FullName ?? refreshed.Username,
            refreshed.ResourceProfile?.Id);
    }

    public async Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdWithDetailsAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return new MeDto(
            user.Id,
            user.Username,
            user.Email,
            UserDisplayHelper.GetDisplayName(user),
            user.Role.RoleName.ToUpperInvariant(),
            user.RequiresPasswordChange,
            user.ResourceProfile?.Id);
    }

    private async Task TryRecordLoginFailureAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            auditLogRepository.Add(AuditLog.Create(null, "LOGIN_FAILED", nameof(User), user.Id, user.Username, clock.UtcNow));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Best-effort audit; login failure must still be returned to the caller.
        }
    }
}
