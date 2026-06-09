using PRM.Application.Features.SystemConfig.Dtos;
using PRM.Application.Interfaces.Security;
using PRM.Domain.Entities;

namespace PRM.Application.Features.SystemConfig;

internal static class SystemConfigMappings
{
    internal static SystemConfigDto ToDto(SystemConfiguration config, IApiKeyProtector apiKeyProtector) =>
        new(
            config.LlmProvider.ToString(),
            apiKeyProtector.Mask(config.LlmApiKeyEncrypted),
            config.SchedulerIntervalMinutes,
            config.MaxWeeklyHours);
}
