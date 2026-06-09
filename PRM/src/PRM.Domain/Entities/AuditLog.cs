namespace PRM.Domain.Entities;

public class AuditLog
{
    private AuditLog() { }

    public long Id { get; private set; }
    public long? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public long? EntityId { get; private set; }
    public string? Details { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static AuditLog Create(
        long? userId,
        string action,
        string entityType,
        long? entityId,
        string? details,
        DateTime utcNow) =>
        new()
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAt = utcNow
        };
}
