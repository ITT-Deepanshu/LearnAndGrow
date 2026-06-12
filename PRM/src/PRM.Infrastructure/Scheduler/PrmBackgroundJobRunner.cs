using Microsoft.Extensions.Logging;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Interfaces.Scheduling;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Helpers;
using PRM.Domain.Services;

namespace PRM.Infrastructure.Scheduler;

public sealed class PrmBackgroundJobRunner(
    IClock clock,
    IUnitOfWork unitOfWork,
    IResourceProfileRepository employeeRepository,
    IAllocationRepository allocationRepository,
    IProjectRepository projectRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigRepository systemConfigRepository,
    ILogger<PrmBackgroundJobRunner> logger) : IPrmBackgroundJobRunner
{
    private const long SystemActorId = 0;
    private readonly ProjectHealthDomainService _healthService = new();

    public async Task RunScheduledJobsAsync(CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var utcNow = clock.UtcNow;
        var config = await systemConfigRepository.GetAsync(cancellationToken);

        await RecomputeUtilisationAsync(today, cancellationToken);
        await RecomputeProjectHealthAsync(config.MaxWeeklyHours, today, utcNow, cancellationToken);
        await DetectMissedTimesheetsAsync(today, utcNow, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("PRM scheduled background jobs completed at {UtcNow}", utcNow);
    }

    private async Task RecomputeUtilisationAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var employees = await employeeRepository.ListAsync(status: null, department: null, managerId: null, cancellationToken);

        foreach (var employee in employees.Where(e => e.Status != ResourceProfileStatus.Inactive))
        {
            var allocations = await allocationRepository.ListActiveForEmployeeAsync(
                employee.Id,
                today,
                today,
                cancellationToken);

            var total = AllocationCapacityHelper.CalculateTotalUtilisation(allocations, today, today);
            employee.RecomputeStatus(total);
        }
    }

    private async Task RecomputeProjectHealthAsync(
        int maxWeeklyHours,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var projects = await projectRepository.ListActiveWithDetailsAsync(cancellationToken);
        var lastWeekStart = WeekHelper.GetLastCompletedWeekMonday(today);

        foreach (var project in projects)
        {
            var activeAllocations = project.Allocations
                .Where(a => a.IsActiveOn(today))
                .ToList();

            var employeeIds = activeAllocations.Select(a => a.ResourceProfileId).Distinct().ToList();
            var timesheets = employeeIds.Count == 0
                ? []
                : await timesheetRepository.ListForTeamWeekAsync(employeeIds, lastWeekStart, cancellationToken);
            var timesheetByEmployee = timesheets.ToDictionary(t => t.ResourceProfileId);

            var effortData = activeAllocations
                .Select(allocation =>
                {
                    var expected = allocation.UtilisationPercentage / 100m * maxWeeklyHours;
                    var logged = 0m;
                    if (timesheetByEmployee.TryGetValue(allocation.ResourceProfileId, out var timesheet))
                    {
                        logged = timesheet.Entries
                            .Where(e => e.ProjectId == project.Id)
                            .Sum(e => e.Hours);
                    }

                    return (allocation.ResourceProfile.FullName, expected, logged);
                })
                .ToList();

            var health = _healthService.Evaluate(project, today, effortData);
            var reason = health.RiskFlags.Count == 0
                ? health.DisplayLabel
                : string.Join("; ", health.RiskFlags);

            project.SetHealth(health.Status, reason, SystemActorId, utcNow);
        }
    }

    private async Task DetectMissedTimesheetsAsync(
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        var employees = await employeeRepository.ListAsync(status: null, department: null, managerId: null, cancellationToken);

        foreach (var employee in employees.Where(e => e.Status != ResourceProfileStatus.Inactive && e.User.IsActive))
        {
            var allocations = await allocationRepository.ListActiveForEmployeeAsync(
                employee.Id,
                weekStart,
                weekStart.AddDays(6),
                cancellationToken);

            if (allocations.Count == 0)
                continue;

            var existing = await timesheetRepository.GetByEmployeeWeekAsync(employee.Id, weekStart, cancellationToken);
            if (existing is not null)
                continue;

            timesheetRepository.Add(Timesheet.CreateMissed(employee.Id, weekStart, SystemActorId, utcNow));
        }
    }

}
