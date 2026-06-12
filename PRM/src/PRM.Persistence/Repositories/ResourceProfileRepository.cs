using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Persistence.Repositories;

public class ResourceProfileRepository(PrmDbContext context) : IResourceProfileRepository
{
    public async Task<ResourceProfile?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.ResourceProfiles
            .Include(e => e.User).ThenInclude(u => u.Role)
            .Include(e => e.Manager).ThenInclude(m => m!.ResourceProfile)
            .Include(e => e.Allocations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<ResourceProfile?> GetByIdWithSkillsAsync(long id, CancellationToken cancellationToken = default) =>
        await context.ResourceProfiles
            .Include(e => e.User).ThenInclude(u => u.Role)
            .Include(e => e.Manager).ThenInclude(m => m!.ResourceProfile)
            .Include(e => e.Skills)
            .Include(e => e.Allocations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<ResourceProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.ResourceProfiles
            .Include(e => e.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<ResourceProfile>> ListAsync(
        ResourceProfileStatus? status,
        string? department,
        long? managerId,
        CancellationToken cancellationToken = default)
    {
        var query = context.ResourceProfiles
            .Include(e => e.User).ThenInclude(u => u.Role)
            .Include(e => e.Manager).ThenInclude(m => m!.ResourceProfile)
            .Include(e => e.Skills)
            .Include(e => e.Allocations)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(e => e.Department == department);
        if (managerId.HasValue)
            query = query.Where(e => e.ManagerId == managerId.Value);

        return await query.OrderBy(e => e.Id).ToListAsync(cancellationToken);
    }

    public void Add(ResourceProfile resourceProfile) => context.ResourceProfiles.Add(resourceProfile);
}
