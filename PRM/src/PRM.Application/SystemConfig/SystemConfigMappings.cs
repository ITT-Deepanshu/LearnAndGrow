using PRM.Application.SystemConfig;
using PRM.Application.Interfaces.Security;
using PRM.Domain.Entities;

namespace PRM.Application.SystemConfig;

internal static class SystemConfigMappings
{
    internal static SystemConfigDto ToDto(SystemConfiguration config, IApiKeyProtector apiKeyProtector) =>
        new(
            config.LlmProvider.ToString(),
            apiKeyProtector.Mask(config.LlmApiKeyEncrypted),
            config.SchedulerIntervalMinutes,
            config.MaxWeeklyHours);
}
