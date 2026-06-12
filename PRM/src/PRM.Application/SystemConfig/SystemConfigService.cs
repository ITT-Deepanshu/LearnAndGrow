using PRM.Application.SystemConfig;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Interfaces.Scheduling;
using PRM.Application.Interfaces.Security;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.SystemConfig;

public sealed class SystemConfigService(
    ISystemConfigRepository systemConfigRepository,
    IApiKeyProtector apiKeyProtector,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IPrmJobScheduleManager jobScheduleManager,
    IClock clock) : ISystemConfigService
{
    public async Task<SystemConfigDto> GetSystemConfigAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        return SystemConfigMappings.ToDto(config, apiKeyProtector);
    }

    public async Task<SystemConfigDto> UpdateSystemConfigAsync(UpdateSystemConfigDto dto, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();

        if (!string.IsNullOrWhiteSpace(dto.LlmApiKey) && !IsMaskedApiKey(dto.LlmApiKey))
            await UpdateLlmApiKeyAsync(dto.LlmApiKey, cancellationToken);

        await UpdateLlmProviderAsync(dto.LlmProvider, cancellationToken);
        await UpdateSchedulerIntervalAsync(dto.SchedulerIntervalMinutes, cancellationToken);
        await UpdateMaxWeeklyHoursAsync(dto.MaxWeeklyHours, cancellationToken);

        return await GetSystemConfigAsync(cancellationToken);
    }

    private async Task UpdateLlmApiKeyAsync(string apiKey, CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var encrypted = apiKeyProtector.Protect(apiKey.Trim());
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

    private async Task UpdateLlmProviderAsync(AiProviderType provider, CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetAsync(cancellationToken);
        config.UpdateLlmProvider(provider, currentUser.UserId!.Value, clock.UtcNow);

        auditLogRepository.Add(AuditLog.Create(
            currentUser.UserId,
            "SYSTEM_CONFIG_LLM_PROVIDER_UPDATED",
            nameof(SystemConfiguration),
            config.Id,
            provider.ToString(),
            clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateSchedulerIntervalAsync(int minutes, CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetAsync(cancellationToken);
        config.UpdateSchedulerInterval(minutes, currentUser.UserId!.Value, clock.UtcNow);

        auditLogRepository.Add(AuditLog.Create(
            currentUser.UserId,
            "SYSTEM_CONFIG_SCHEDULER_INTERVAL_UPDATED",
            nameof(SystemConfiguration),
            config.Id,
            minutes.ToString(),
            clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        jobScheduleManager.ScheduleRecurringJob(minutes);
    }

    private async Task UpdateMaxWeeklyHoursAsync(int hours, CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetAsync(cancellationToken);
        config.UpdateMaxWeeklyHours(hours, currentUser.UserId!.Value, clock.UtcNow);

        auditLogRepository.Add(AuditLog.Create(
            currentUser.UserId,
            "SYSTEM_CONFIG_MAX_WEEKLY_HOURS_UPDATED",
            nameof(SystemConfiguration),
            config.Id,
            hours.ToString(),
            clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool IsMaskedApiKey(string apiKey) =>
        apiKey.All(c => c == '*');

    private void EnsureAdmin()
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");
    }
}
