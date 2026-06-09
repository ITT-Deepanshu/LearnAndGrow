using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.SystemConfig.Commands;

public sealed class UpdateLlmProviderCommandHandler(
    ISystemConfigRepository systemConfigRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateLlmProviderCommand>
{
    public async Task Handle(UpdateLlmProviderCommand request, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        config.UpdateLlmProvider(request.Provider, currentUser.UserId!.Value, clock.UtcNow);

        auditLogRepository.Add(AuditLog.Create(
            currentUser.UserId,
            "SYSTEM_CONFIG_LLM_PROVIDER_UPDATED",
            nameof(SystemConfiguration),
            config.Id,
            request.Provider.ToString(),
            clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void EnsureAdmin()
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");
    }
}
