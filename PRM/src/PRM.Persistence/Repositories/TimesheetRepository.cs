using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;

namespace PRM.Persistence.Repositories;

public class TimesheetRepository(PrmDbContext context) : ITimesheetRepository
{
    public async Task<Timesheet?> GetByEmployeeWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        await context.Timesheets
            .Include(t => t.Entries).ThenInclude(e => e.Project)
            .Include(t => t.Entries).ThenInclude(e => e.ActivityTags)
            .FirstOrDefaultAsync(t => t.ResourceProfileId == employeeId && t.WeekStart == weekStart, cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> ListByEmployeeAsync(long employeeId, CancellationToken cancellationToken = default) =>
        await context.Timesheets
            .Where(t => t.ResourceProfileId == employeeId)
            .OrderByDescending(t => t.WeekStart)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> ListForTeamWeekAsync(
        IReadOnlyList<long> employeeIds, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        await context.Timesheets
            .Include(t => t.ResourceProfile).ThenInclude(e => e.User)
            .Include(t => t.Entries).ThenInclude(e => e.Project)
            .Where(t => employeeIds.Contains(t.ResourceProfileId) && t.WeekStart == weekStart)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetRecentActivityTagsForEmployeeAsync(
        long employeeId,
        DateOnly sinceWeekStart,
        CancellationToken cancellationToken = default) =>
        await context.TimesheetEntries
            .Where(e => e.Timesheet.ResourceProfileId == employeeId && e.Timesheet.WeekStart >= sinceWeekStart)
            .SelectMany(e => e.ActivityTags.Select(t => t.Name))
            .Distinct()
            .OrderBy(name => name)
            .Take(10)
            .ToListAsync(cancellationToken);

    public void Add(Timesheet timesheet) => context.Timesheets.Add(timesheet);

    public void Remove(Timesheet timesheet) => context.Timesheets.Remove(timesheet);
}
