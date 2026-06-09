using MediatR;
using PRM.Application.Features.Dashboard.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Dashboard.Queries;

public sealed class GetResourceDashboardQueryHandler(
    IEmployeeRepository employeeRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<GetResourceDashboardQuery, ResourceDashboardDto>
{
    public async Task<ResourceDashboardDto> Handle(GetResourceDashboardQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        var today = clock.Today;
        var managerId = currentUser.UserId.Value;
        var employees = (await employeeRepository.ListAsync(null, null, managerId, cancellationToken))
            .Where(e => e.Status != EmployeeStatus.Inactive)
            .ToList();

        var utilisationByEmployee = new Dictionary<long, decimal>();
        var activeAllocationsByEmployee = new Dictionary<long, List<Allocation>>();

        foreach (var employee in employees)
        {
            var allocations = (await allocationRepository.ListActiveForEmployeeAsync(
                employee.Id,
                today,
                today,
                cancellationToken)).ToList();

            var utilisation = AllocationCapacityHelper.CalculateTotalUtilisation(allocations, today, today);
            utilisationByEmployee[employee.Id] = utilisation;
            activeAllocationsByEmployee[employee.Id] = allocations;
        }

        var benchEmployees = employees
            .Where(e => e.Status == EmployeeStatus.Bench)
            .ToList();

        var onBench = benchEmployees
            .Select(ToBenchRow)
            .ToList();

        var partiallyAllocated = employees
            .Where(e => e.Status == EmployeeStatus.PartiallyAllocated)
            .Select(e => ToActiveRow(e, utilisationByEmployee[e.Id]))
            .ToList();

        var fullyAllocated = employees
            .Where(e => e.Status == EmployeeStatus.Allocated)
            .Select(e => ToActiveRow(e, utilisationByEmployee[e.Id]))
            .ToList();

        var skillsSummary = BuildSkillsSummary(benchEmployees);
        var drillDown = new List<EmployeeDrillDownDto>();

        foreach (var employee in employees)
        {
            var utilisation = utilisationByEmployee[employee.Id];
            var recentTags = await GetRecentActivityTagsAsync(employee.Id, today, cancellationToken);
            var allocations = activeAllocationsByEmployee[employee.Id];

            drillDown.Add(new EmployeeDrillDownDto(
                employee.Id,
                employee.User.FullName,
                employee.Department,
                employee.Designation,
                employee.Status.ToString(),
                utilisation,
                Math.Max(0, 100 - utilisation),
                employee.Skills.Select(s => s.Name).ToList(),
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

    private static BenchEmployeeRowDto ToBenchRow(Employee employee) =>
        new(
            employee.Id,
            employee.User.FullName,
            employee.Department,
            employee.Skills.Select(s => s.Name).ToList());

    private static ActiveEmployeeRowDto ToActiveRow(Employee employee, decimal utilisation) =>
        new(
            employee.Id,
            employee.User.FullName,
            employee.Department,
            utilisation,
            Math.Max(0, 100 - utilisation),
            employee.Skills.Select(s => s.Name).ToList());

    private static IReadOnlyList<SkillCategorySummaryDto> BuildSkillsSummary(IEnumerable<Employee> benchEmployees) =>
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
