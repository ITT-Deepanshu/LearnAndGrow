namespace PRM.SharedKernel;

public abstract class AuditableEntity
{
    public long Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public long CreatedBy { get; protected set; }
    public DateTime? ModifiedAt { get; protected set; }
    public long? ModifiedBy { get; protected set; }
    public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

    protected void SetCreated(long actorId, DateTime utcNow)
    {
        CreatedAt = utcNow;
        CreatedBy = actorId;
        ModifiedAt = utcNow;
        ModifiedBy = actorId;
    }

    protected void SetModified(long actorId, DateTime utcNow)
    {
        ModifiedAt = utcNow;
        ModifiedBy = actorId;
    }
}
