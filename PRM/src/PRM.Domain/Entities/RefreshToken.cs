namespace PRM.Domain.Entities;

public class RefreshToken
{
    private RefreshToken() { }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public User User { get; private set; } = null!;

    public static RefreshToken Create(long userId, string tokenHash, DateTime expiresAt, DateTime utcNow)
    {
        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = utcNow,
            IsRevoked = false
        };
    }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    public void Revoke(DateTime utcNow)
    {
        IsRevoked = true;
        RevokedAt = utcNow;
    }
}
