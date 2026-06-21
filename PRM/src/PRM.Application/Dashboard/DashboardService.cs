using PRM.Application.Dashboard;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Dashboard;

public sealed class DashboardService(
    IResourceProfileRepository employeeRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository,
    ICurrentUser currentUser,
    IClock clock) : IDashboardService
{
    public async Task<ResourceDashboardDto> GetResourceDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        var today = clock.Today;
        var managerId = currentUser.UserId.Value;
        var employees = (await employeeRepository.ListAsync(null, null, managerId, cancellationToken))
            .Where(e => e.Status != ResourceProfileStatus.Inactive)
            .ToList();

        var utilisationByEmployee = new Dictionary<long, decimal>();
        var activeAllocationsByEmployee = new Dictionary<long, List<Allocation>>();

        foreach (var resourceProfile in employees)
        {
            var allocations = (await allocationRepository.ListActiveForEmployeeAsync(
                resourceProfile.Id,
                today,
                today,
                cancellationToken)).ToList();

            var utilisation = AllocationCapacityHelper.CalculateTotalUtilisation(allocations, today, today);
            utilisationByEmployee[resourceProfile.Id] = utilisation;
            activeAllocationsByEmployee[resourceProfile.Id] = allocations;
        }

        var benchEmployees = employees
            .Where(e => e.Status == ResourceProfileStatus.Bench)
            .ToList();

        var onBench = benchEmployees
            .Select(ToBenchRow)
            .ToList();

        var partiallyAllocated = employees
            .Where(e => e.Status == ResourceProfileStatus.PartiallyAllocated)
            .Select(e => ToActiveRow(e, utilisationByEmployee[e.Id]))
            .ToList();

        var fullyAllocated = employees
            .Where(e => e.Status == ResourceProfileStatus.Allocated)
            .Select(e => ToActiveRow(e, utilisationByEmployee[e.Id]))
            .ToList();

        var skillsSummary = BuildSkillsSummary(benchEmployees);
        var drillDown = new List<EmployeeDrillDownDto>();

        foreach (var resourceProfile in employees)
        {
            var utilisation = utilisationByEmployee[resourceProfile.Id];
            var recentTags = await GetRecentActivityTagsAsync(resourceProfile.Id, today, cancellationToken);
            var allocations = activeAllocationsByEmployee[resourceProfile.Id];

            drillDown.Add(new EmployeeDrillDownDto(
                resourceProfile.Id,
                resourceProfile.FullName,
                resourceProfile.Department,
                resourceProfile.Designation,
                resourceProfile.Status.ToString(),
                utilisation,
                Math.Max(0, 100 - utilisation),
                resourceProfile.Skills.Select(s => s.Name).ToList(),
                allocations.Select(a => new EmployeeAllocationRowDto(
                    a.Project?.Name ?? string.Empty,
                    a.UtilisationPercentage,
                    a.FromDate,
                    a.ToDate)).ToList(),
                recentTags));
        }

        return new ResourceDashboardDto(
            today,
            new DashboardCountsDto(onBench.Count, partiallyAllocated.Count, fullyAllocated.Count),
            onBench,
            partiallyAllocated,
            fullyAllocated,
            skillsSummary,
            drillDown);
    }

    private static BenchEmployeeRowDto ToBenchRow(ResourceProfile resourceProfile) =>
        new(
            resourceProfile.Id,
            resourceProfile.FullName,
            resourceProfile.Department,
            resourceProfile.Skills.Select(s => s.Name).ToList());

    private static ActiveEmployeeRowDto ToActiveRow(ResourceProfile resourceProfile, decimal utilisation) =>
        new(
            resourceProfile.Id,
            resourceProfile.FullName,
            resourceProfile.Department,
            utilisation,
            Math.Max(0, 100 - utilisation),
            resourceProfile.Skills.Select(s => s.Name).ToList());

    private static IReadOnlyList<SkillCategorySummaryDto> BuildSkillsSummary(IEnumerable<ResourceProfile> benchEmployees) =>
        benchEmployees
            .SelectMany(e => e.Skills)
            .GroupBy(s => s.Category.ToString())
            .OrderBy(g => g.Key)
            .Select(g => new SkillCategorySummaryDto(
                g.Key,
                g.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(sg => new SkillCountDto(sg.Key, sg.Count()))
                    .OrderBy(s => s.SkillName)
                    .ToList()))
            .ToList();

    private async Task<IReadOnlyList<string>> GetRecentActivityTagsAsync(
        long employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);

        for (var i = 0; i < 4; i++)
        {
            var timesheet = await timesheetRepository.GetByEmployeeWeekAsync(
                employeeId,
                weekStart.AddDays(-7 * i),
                cancellationToken);

            if (timesheet is null)
                continue;

            foreach (var entry in timesheet.Entries)
            {
                foreach (var tag in entry.ActivityTags)
                    tags.Add(tag.Name);
            }
        }

        return tags.OrderBy(t => t).ToList();
    }
}
