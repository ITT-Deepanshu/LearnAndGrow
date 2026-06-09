using MediatR;
using PRM.Application.Features.Ai.Dtos;
using PRM.Application.Interfaces.Ai;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;
using PRM.Domain.Services;

namespace PRM.Application.Features.Ai.Queries;

public sealed class GetProjectRiskSummaryQueryHandler(
    IProjectRepository projectRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigRepository systemConfigRepository,
    IAiProviderOrchestrator aiOrchestrator,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<GetProjectRiskSummaryQuery, RiskSummaryDto>
{
    private readonly ProjectHealthDomainService _healthService = new();

    public async Task<RiskSummaryDto> Handle(
        GetProjectRiskSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        var project = await projectRepository.GetByIdWithDetailsAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (project.ManagerId != currentUser.UserId.Value)
            throw new ForbiddenException("You do not own this project.");

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var today = clock.Today;
        var lastWeekStart = WeekHelper.GetLastCompletedWeekMonday(today);

        var activeAllocations = (await allocationRepository.ListActiveOnProjectAsync(project.Id, cancellationToken))
            .Where(a => a.IsActiveOn(today))
            .ToList();

        var effortData = await BuildEffortDataAsync(
            activeAllocations,
            lastWeekStart,
            config.MaxWeeklyHours,
            cancellationToken);

        var health = _healthService.Evaluate(project, today, effortData);
        var aiRequest = new RiskSummaryAiRequest(
            project.Name,
            project.Milestones
                .Select(m => new MilestoneAiContext(m.Title, m.DueDate, m.Status.ToString()))
                .ToList(),
            activeAllocations
                .Select(a => $"{a.Employee.User.FullName} ({a.UtilisationPercentage}%)")
                .ToList(),
            effortData
                .Select(e => new EffortAiContext(e.EmployeeName, e.ExpectedHours, e.LoggedHours))
                .ToList());

        var aiResponse = await aiOrchestrator.SummarizeRiskAsync(aiRequest, cancellationToken);

        return new RiskSummaryDto(
            aiResponse.Paragraph,
            new RiskSummarySnapshotDto(
                health.RiskFlags.Where(f => f.Contains("overdue", StringComparison.OrdinalIgnoreCase)).ToList(),
                health.RiskFlags.Where(f => f.Contains("logged", StringComparison.OrdinalIgnoreCase)).ToList(),
                health.DisplayLabel));
    }

    private async Task<IReadOnlyList<(string EmployeeName, decimal ExpectedHours, decimal LoggedHours)>> BuildEffortDataAsync(
        IReadOnlyList<Allocation> allocations,
        DateOnly weekStart,
        int maxWeeklyHours,
        CancellationToken cancellationToken)
    {
        if (allocations.Count == 0)
            return [];

        var employeeIds = allocations.Select(a => a.EmployeeId).Distinct().ToList();
        var timesheets = await timesheetRepository.ListForTeamWeekAsync(employeeIds, weekStart, cancellationToken);
        var timesheetByEmployee = timesheets.ToDictionary(t => t.EmployeeId);

        var results = new List<(string EmployeeName, decimal ExpectedHours, decimal LoggedHours)>();
        foreach (var allocation in allocations)
        {
            var expected = allocation.UtilisationPercentage / 100m * maxWeeklyHours;
            var logged = 0m;

            if (timesheetByEmployee.TryGetValue(allocation.EmployeeId, out var timesheet))
            {
                logged = timesheet.Entries
                    .Where(e => e.ProjectId == allocation.ProjectId)
                    .Sum(e => e.Hours);
            }

            results.Add((allocation.Employee.User.FullName, expected, logged));
        }

        return results;
    }
}
