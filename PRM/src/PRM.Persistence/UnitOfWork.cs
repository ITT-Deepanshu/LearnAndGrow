using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PRM.Application.Interfaces.Persistence;

namespace PRM.Persistence;

public class UnitOfWork(PrmDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    private const int MaxConcurrencyRetries = 5;
    private IDbContextTransaction? _transaction;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            try
            {
                return await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (attempt >= MaxConcurrencyRetries - 1)
                {
                    LogConcurrencyFailure(ex, attempt + 1);
                    throw;
                }

                logger.LogWarning(
                    ex,
                    "Concurrency conflict on save (attempt {Attempt}/{MaxAttempts}). Refreshing row versions and retrying.",
                    attempt + 1,
                    MaxConcurrencyRetries);

                await RefreshConcurrencyTokensAsync(ex, cancellationToken);
            }
        }

        throw new InvalidOperationException("SaveChangesAsync exhausted concurrency retries without completing.");
    }

    public void ClearChangeTracker() => context.ChangeTracker.Clear();

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    private async Task RefreshConcurrencyTokensAsync(
        DbUpdateConcurrencyException ex,
        CancellationToken cancellationToken)
    {
        foreach (var entry in ex.Entries)
        {
            var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
            if (databaseValues is null)
            {
                entry.State = EntityState.Detached;
                continue;
            }

            entry.OriginalValues.SetValues(databaseValues);
        }
    }

    private void LogConcurrencyFailure(DbUpdateConcurrencyException ex, int attempts)
    {
        var conflicts = ex.Entries
            .Select(entry => $"{entry.Metadata.ClrType.Name} Id={entry.Property("Id").CurrentValue} State={entry.State}")
            .ToList();

        logger.LogError(
            ex,
            "Save failed after {Attempts} attempts due to concurrency conflicts on: {Entities}",
            attempts,
            conflicts.Count == 0 ? "(none reported)" : string.Join(", ", conflicts));
    }
}
