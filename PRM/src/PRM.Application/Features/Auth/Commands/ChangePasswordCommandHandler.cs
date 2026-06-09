using MediatR;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Auth.Commands;

public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!user.ForcePasswordChange)
        {
            if (string.IsNullOrEmpty(request.CurrentPassword) || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
                throw new ConflictException("Current password is incorrect.");
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), user.Id, clock.UtcNow);
        refreshTokenRepository.RevokeAllForUser(user.Id, clock.UtcNow);
        auditLogRepository.Add(AuditLog.Create(user.Id, "PASSWORD_CHANGED", nameof(User), user.Id, null, clock.UtcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
