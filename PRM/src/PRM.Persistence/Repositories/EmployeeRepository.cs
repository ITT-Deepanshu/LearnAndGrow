using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Persistence.Repositories;

public class EmployeeRepository(PrmDbContext context) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Employees
            .Include(e => e.User)
            .Include(e => e.Manager)
            .Include(e => e.Allocations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<Employee?> GetByIdWithSkillsAsync(long id, CancellationToken cancellationToken = default) =>
        await context.Employees
            .Include(e => e.User)
            .Include(e => e.Manager)
            .Include(e => e.Skills)
            .Include(e => e.Allocations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.Employees
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Employee>> ListAsync(
        EmployeeStatus? status,
        string? department,
        long? managerId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Employees
            .Include(e => e.User)
            .Include(e => e.Manager)
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

    public void Add(Employee employee) => context.Employees.Add(employee);
}
