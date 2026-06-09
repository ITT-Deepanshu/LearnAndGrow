using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Interfaces.Security;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.SystemConfig.Commands;

public sealed class UpdateLlmApiKeyCommandHandler(
    ISystemConfigRepository systemConfigRepository,
    IApiKeyProtector apiKeyProtector,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateLlmApiKeyCommand>
{
    public async Task Handle(UpdateLlmApiKeyCommand request, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var encrypted = apiKeyProtector.Protect(request.ApiKey.Trim());
        config.UpdateLlmApiKey(encrypted, currentUser.UserId!.Value, clock.UtcNow);

        auditLogRepository.Add(AuditLog.Create(
            currentUser.UserId,
            "SYSTEM_CONFIG_LLM_API_KEY_UPDATED",
            nameof(SystemConfiguration),
            config.Id,
            null,
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
