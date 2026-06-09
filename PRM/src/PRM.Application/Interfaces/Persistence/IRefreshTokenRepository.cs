using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(long userId, CancellationToken cancellationToken = default);
    void Add(RefreshToken token);
    void RevokeAllForUser(long userId, DateTime utcNow);
}
