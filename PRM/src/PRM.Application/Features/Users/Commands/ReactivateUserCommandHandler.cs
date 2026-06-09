using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Users.Commands;

public sealed class ReactivateUserCommandHandler(
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<ReactivateUserCommand>
{
    public async Task Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.IsActive)
            return;

        var actorId = currentUser.UserId.Value;
        var utcNow = clock.UtcNow;

        user.Reactivate(actorId, utcNow);

        var employee = await employeeRepository.GetByUserIdAsync(user.Id, cancellationToken);
        employee?.Reactivate(actorId, utcNow);

        auditLogRepository.Add(AuditLog.Create(actorId, "USER_REACTIVATED", nameof(User), user.Id, user.Username, utcNow));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
