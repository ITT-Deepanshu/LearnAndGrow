using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;

namespace PRM.Persistence.Repositories;

public class ActivityTagRepository(PrmDbContext context) : IActivityTagRepository
{
    public async Task<IReadOnlyList<ActivityTag>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await context.ActivityTags.OrderBy(t => t.Id).ToListAsync(cancellationToken);

    public async Task<ActivityTag?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.ActivityTags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
}
