using PRM.Domain.Enums;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class SystemConfiguration : AuditableEntity
{
    private SystemConfiguration() { }

    public AiProviderType LlmProvider { get; private set; } = AiProviderType.Gemma;
    public string LlmApiKeyEncrypted { get; private set; } = string.Empty;
    public int SchedulerIntervalMinutes { get; private set; } = 240;
    public int MaxWeeklyHours { get; private set; } = 40;

    public static SystemConfiguration CreateDefault(long actorId, DateTime utcNow)
    {
        var config = new SystemConfiguration();
        config.SetCreated(actorId, utcNow);
        return config;
    }

    public void UpdateLlmApiKey(string encryptedKey, long actorId, DateTime utcNow)
    {
        LlmApiKeyEncrypted = encryptedKey;
        SetModified(actorId, utcNow);
    }

    public void UpdateLlmProvider(AiProviderType provider, long actorId, DateTime utcNow)
    {
        LlmProvider = provider;
        SetModified(actorId, utcNow);
    }

    public void UpdateSchedulerInterval(int minutes, long actorId, DateTime utcNow)
    {
        if (minutes < 1)
            throw new Exceptions.ValidationException("Scheduler interval must be at least 1 minute.");
        SchedulerIntervalMinutes = minutes;
        SetModified(actorId, utcNow);
    }

    public void UpdateMaxWeeklyHours(int hours, long actorId, DateTime utcNow)
    {
        if (hours < 1 || hours > 168)
            throw new Exceptions.ValidationException("Max weekly hours must be between 1 and 168.");
        MaxWeeklyHours = hours;
        SetModified(actorId, utcNow);
    }
}
