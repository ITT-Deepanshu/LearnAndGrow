using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;

namespace PRM.Persistence.Repositories;

public class RefreshTokenRepository(PrmDbContext context) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> ListActiveForUserAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);

    public void Add(RefreshToken token) => context.RefreshTokens.Add(token);

    public void RevokeAllForUser(long userId, DateTime utcNow)
    {
        var tokens = context.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked);
        foreach (var token in tokens)
            token.Revoke(utcNow);
    }
}
