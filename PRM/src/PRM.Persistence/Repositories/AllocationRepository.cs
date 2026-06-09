using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;

namespace PRM.Persistence.Repositories;

public class AllocationRepository(PrmDbContext context) : IAllocationRepository
{
    public async Task<Allocation?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Allocations
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.Project)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Allocation>> ListActiveForEmployeeAsync(
        long employeeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await context.Allocations
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.Project)
            .Where(a => a.EmployeeId == employeeId
                        && a.EndedAt == null
                        && a.FromDate <= to
                        && a.ToDate >= from)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> ListActiveOnProjectAsync(long projectId, CancellationToken cancellationToken = default) =>
        await context.Allocations
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.Project)
            .Where(a => a.ProjectId == projectId && a.EndedAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Allocation>> ListAllAsync(long? employeeId, long? projectId, CancellationToken cancellationToken = default)
    {
        var query = context.Allocations
            .Include(a => a.Employee).ThenInclude(e => e.User)
            .Include(a => a.Project)
            .Where(a => a.EndedAt == null)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        return await query.OrderBy(a => a.Id).ToListAsync(cancellationToken);
    }

    public void Add(Allocation allocation) => context.Allocations.Add(allocation);
}
