using MediatR;
using PRM.Application.Interfaces.Auth;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Users.Commands;

public sealed class ResetUserPasswordCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAuditLogRepository auditLogRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<ResetUserPasswordCommand>
{
    public async Task Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        user.ChangePassword(passwordHasher.Hash(request.NewPassword), actorId, utcNow);
        user.RequirePasswordChange(actorId, utcNow);
        refreshTokenRepository.RevokeAllForUser(user.Id, utcNow);
        auditLogRepository.Add(AuditLog.Create(actorId, "PASSWORD_RESET", nameof(User), user.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
