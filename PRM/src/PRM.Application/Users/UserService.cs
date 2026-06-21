using PRM.Application.Common;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Users;

public sealed class UserService(
    IUserRepository userRepository,
    IResourceProfileRepository resourceProfileRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IUserService
{
    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, UserRole role, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var username = dto.Username.Trim().ToLowerInvariant();
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await userRepository.ExistsByUsernameAsync(username, cancellationToken))
            throw new ConflictException("Username is already taken.");

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;
        var user = User.Create(
            username,
            email,
            passwordHasher.Hash(dto.TemporaryPassword),
            role,
            actorId,
            utcNow,
            requiresPasswordChange: true);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            userRepository.Add(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var profile = ResourceProfile.Create(
                user.Id,
                dto.FullName,
                string.Empty,
                string.Empty,
                actorId,
                utcNow);
            resourceProfileRepository.Add(profile);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            auditLogRepository.Add(AuditLog.Create(actorId, "USER_CREATED", nameof(User), user.Id, user.Username, utcNow));
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var created = await userRepository.GetByIdWithDetailsAsync(user.Id, cancellationToken)
            ?? throw new NotFoundException("Created user could not be loaded.");

        return MapToDto(created);
    }

    public async Task<IReadOnlyList<UserListItemDto>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        return users
            .Select(u => new UserListItemDto(
                u.Id,
                u.Username,
                u.Role.RoleName.ToUpperInvariant(),
                u.IsActive))
            .ToList();
    }

    public async Task<UserDto> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdWithDetailsAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return MapToDto(user);
    }

    public async Task ResetUserPasswordAsync(long userId, ResetUserPasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        user.ChangePassword(passwordHasher.Hash(dto.NewPassword), actorId, utcNow);
        user.RequirePasswordChange(actorId, utcNow);
        auditLogRepository.Add(AuditLog.Create(actorId, "PASSWORD_RESET", nameof(User), user.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        if (currentUser.UserId.Value == userId)
            throw new ConflictException("You cannot deactivate your own account.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!user.IsActive)
            return;

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        user.Deactivate(actorId, utcNow);

        var resourceProfile = await resourceProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        resourceProfile?.Deactivate(actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "USER_DEACTIVATED", nameof(User), user.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReactivateUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.IsActive)
            return;

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        user.Reactivate(actorId, utcNow);

        var resourceProfile = await resourceProfileRepository.GetByUserIdAsync(user.Id, cancellationToken);
        resourceProfile?.Reactivate(actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "USER_REACTIVATED", nameof(User), user.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static UserDto MapToDto(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            UserDisplayHelper.GetDisplayName(user),
            user.Role.RoleName.ToUpperInvariant(),
            user.IsActive,
            user.RequiresPasswordChange);
}
