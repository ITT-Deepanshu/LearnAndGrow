using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Helpers;
using PRM.Domain.Services;

namespace PRM.Infrastructure.Scheduler;

public sealed class PrmBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PrmBackgroundService> logger) : BackgroundService
{
    private const long SystemActorId = 0;
    private readonly ProjectHealthDomainService _healthService = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PRM background scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalMinutes = 240;

            try
            {
                using var scope = scopeFactory.CreateScope();
                var configRepository = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                var config = await configRepository.GetAsync(stoppingToken);
                intervalMinutes = Math.Max(1, config.SchedulerIntervalMinutes);

                await RunJobsAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "PRM background scheduler tick failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("PRM background scheduler stopped.");
    }

    private async Task RunJobsAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var clock = services.GetRequiredService<IClock>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var employeeRepository = services.GetRequiredService<IEmployeeRepository>();
        var allocationRepository = services.GetRequiredService<IAllocationRepository>();
        var projectRepository = services.GetRequiredService<IProjectRepository>();
        var timesheetRepository = services.GetRequiredService<ITimesheetRepository>();
        var refreshTokenRepository = services.GetRequiredService<IRefreshTokenRepository>();
        var systemConfigRepository = services.GetRequiredService<ISystemConfigRepository>();

        var today = clock.Today;
        var utcNow = clock.UtcNow;
        var config = await systemConfigRepository.GetAsync(cancellationToken);

        await RecomputeUtilisationAsync(employeeRepository, allocationRepository, today, cancellationToken);
        await RecomputeProjectHealthAsync(
            projectRepository,
            timesheetRepository,
            config.MaxWeeklyHours,
            today,
            utcNow,
            cancellationToken);
        await DetectMissedTimesheetsAsync(
            employeeRepository,
            allocationRepository,
            timesheetRepository,
            today,
            utcNow,
            cancellationToken);
        await PruneRefreshTokensAsync(refreshTokenRepository, utcNow, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("PRM background scheduler tick completed at {UtcNow}", utcNow);
    }

    private async Task RecomputeUtilisationAsync(
        IEmployeeRepository employeeRepository,
        IAllocationRepository allocationRepository,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var employees = await employeeRepository.ListAsync(status: null, department: null, managerId: null, cancellationToken);

        foreach (var employee in employees.Where(e => e.Status != EmployeeStatus.Inactive))
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
        IProjectRepository projectRepository,
        ITimesheetRepository timesheetRepository,
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

            var employeeIds = activeAllocations.Select(a => a.EmployeeId).Distinct().ToList();
            var timesheets = employeeIds.Count == 0
                ? []
                : await timesheetRepository.ListForTeamWeekAsync(employeeIds, lastWeekStart, cancellationToken);
            var timesheetByEmployee = timesheets.ToDictionary(t => t.EmployeeId);

            var effortData = activeAllocations
                .Select(allocation =>
                {
                    var expected = allocation.UtilisationPercentage / 100m * maxWeeklyHours;
                    var logged = 0m;
                    if (timesheetByEmployee.TryGetValue(allocation.EmployeeId, out var timesheet))
                    {
                        logged = timesheet.Entries
                            .Where(e => e.ProjectId == project.Id)
                            .Sum(e => e.Hours);
                    }

                    return (allocation.Employee.User.FullName, expected, logged);
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
        IEmployeeRepository employeeRepository,
        IAllocationRepository allocationRepository,
        ITimesheetRepository timesheetRepository,
        DateOnly today,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        var employees = await employeeRepository.ListAsync(status: null, department: null, managerId: null, cancellationToken);

        foreach (var employee in employees.Where(e => e.Status != EmployeeStatus.Inactive && e.User.IsActive))
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

    private async Task PruneRefreshTokensAsync(
        IRefreshTokenRepository refreshTokenRepository,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var cutoff = utcNow.AddDays(-7);
        var removed = await refreshTokenRepository.PruneExpiredAsync(cutoff, cancellationToken);
        if (removed > 0)
            logger.LogInformation("Pruned {Count} expired refresh tokens.", removed);
    }
}
