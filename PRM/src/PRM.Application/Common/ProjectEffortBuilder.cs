using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;

namespace PRM.Application.Common;

/// <summary>
/// Builds expected vs logged hours for project health and AI risk analysis.
/// </summary>
public sealed class ProjectEffortBuilder(ITimesheetRepository timesheetRepository)
{
    public async Task<IReadOnlyList<(string EmployeeName, decimal ExpectedHours, decimal LoggedHours)>> BuildAsync(
        IReadOnlyList<Allocation> allocations,
        long projectId,
        DateOnly weekStart,
        int maxWeeklyHours,
        CancellationToken cancellationToken = default)
    {
        if (allocations.Count == 0)
            return [];

        var employeeIds = allocations.Select(a => a.ResourceProfileId).Distinct().ToList();
        var timesheets = await timesheetRepository.ListForTeamWeekAsync(employeeIds, weekStart, cancellationToken);
        var timesheetByEmployee = timesheets.ToDictionary(t => t.ResourceProfileId);

        return allocations.Select(allocation =>
        {
            var expected = allocation.UtilisationPercentage / 100m * maxWeeklyHours;
            var logged = 0m;

            if (timesheetByEmployee.TryGetValue(allocation.ResourceProfileId, out var timesheet))
            {
                logged = timesheet.Entries
                    .Where(e => e.ProjectId == projectId)
                    .Sum(e => e.Hours);
            }

            return (allocation.ResourceProfile.FullName, expected, logged);
        }).ToList();
    }
}
