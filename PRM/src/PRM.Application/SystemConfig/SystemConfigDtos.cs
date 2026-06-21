using PRM.Domain.Enums;

namespace PRM.Application.SystemConfig;

public sealed record SystemConfigDto(
    string LlmProvider,
    string LlmApiKeyMasked,
    int SchedulerIntervalMinutes,
    int MaxWeeklyHours);

public sealed record UpdateSystemConfigDto(
    AiProviderType LlmProvider,
    string? LlmApiKey,
    int SchedulerIntervalMinutes,
    int MaxWeeklyHours);
