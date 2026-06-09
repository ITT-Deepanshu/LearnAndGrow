using MediatR;
using PRM.Application.Features.SystemConfig.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Interfaces.Security;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.SystemConfig.Queries;

public sealed class GetSystemConfigQueryHandler(
    ISystemConfigRepository systemConfigRepository,
    IApiKeyProtector apiKeyProtector,
    ICurrentUser currentUser) : IRequestHandler<GetSystemConfigQuery, SystemConfigDto>
{
    public async Task<SystemConfigDto> Handle(GetSystemConfigQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        return SystemConfigMappings.ToDto(config, apiKeyProtector);
    }
}
