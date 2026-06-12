using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Persistence.Repositories;

public class ProjectRepository(PrmDbContext context) : IProjectRepository
{
    public async Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Project?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Projects
            .Include(p => p.Milestones)
            .Include(p => p.Allocations).ThenInclude(a => a.ResourceProfile).ThenInclude(e => e.User)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        await context.Projects.AnyAsync(p => p.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Project>> ListAsync(long? managerId, CancellationToken cancellationToken = default)
    {
        var query = context.Projects.Include(p => p.Manager).AsQueryable();
        if (managerId.HasValue)
            query = query.Where(p => p.ManagerId == managerId.Value);
        return await query.OrderBy(p => p.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListActiveWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await context.Projects
            .Include(p => p.Milestones)
            .Include(p => p.Allocations).ThenInclude(a => a.ResourceProfile).ThenInclude(e => e.User)
            .Where(p => p.IsActive && p.Status == ProjectStatus.Active)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public void Add(Project project) => context.Projects.Add(project);
}
