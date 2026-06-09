using MediatR;
using PRM.Application.Features.Ai.Dtos;
using PRM.Application.Features.Ai.Helpers;
using PRM.Application.Interfaces.Ai;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Ai.Queries;

public sealed class FindResourcesUsingAIQueryHandler(
    IProjectRepository projectRepository,
    IEmployeeRepository employeeRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigRepository systemConfigRepository,
    IAiProviderOrchestrator aiOrchestrator,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<FindResourcesUsingAIQuery, SkillMatchResultDto>
{
    private const decimal FullTimeFreeThreshold = 50m;

    public async Task<SkillMatchResultDto> Handle(
        FindResourcesUsingAIQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        if (string.IsNullOrWhiteSpace(request.Requirement))
            throw new ValidationException("Requirement text is required.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (project.ManagerId != currentUser.UserId.Value)
            throw new ForbiddenException("You do not own this project.");

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var parsedHours = HoursPerWeekParser.Parse(request.Requirement);
        var today = clock.Today;

        var employees = await employeeRepository.ListAsync(
            EmployeeStatus.Bench,
            department: null,
            managerId: null,
            cancellationToken);

        var partiallyAllocated = await employeeRepository.ListAsync(
            EmployeeStatus.PartiallyAllocated,
            department: null,
            managerId: null,
            cancellationToken);

        var allCandidates = employees
            .Concat(partiallyAllocated)
            .Where(e => e.User.IsActive)
            .ToList();

        var contexts = new List<CandidateAiContext>();
        foreach (var employee in allCandidates)
        {
            var freeUtilisation = CalculateFreeUtilisation(employee, today);
            var freeHours = (int)Math.Floor(freeUtilisation / 100m * config.MaxWeeklyHours);

            if (!PassesCapacityFilter(parsedHours, freeUtilisation, freeHours))
                continue;

            contexts.Add(new CandidateAiContext(
                employee.Id,
                employee.User.FullName,
                employee.Department,
                employee.Skills.Select(s => s.Name).ToList(),
                freeUtilisation,
                freeHours,
                await GetRecentActivityTagsAsync(employee.Id, today, cancellationToken)));
        }

        if (contexts.Count == 0)
        {
            return new SkillMatchResultDto(
                [],
                "No employees have the required free capacity for this request.");
        }

        var aiRequest = new SkillMatchAiRequest(
            request.Requirement.Trim(),
            project.Name,
            contexts);

        var aiResponse = await aiOrchestrator.MatchSkillsAsync(aiRequest, cancellationToken);
        var nameLookup = contexts.ToDictionary(c => c.EmployeeId, c => c.Name);

        var ranked = aiResponse.Candidates
            .Select(c => new RankedCandidateDto(
                c.EmployeeId,
                string.IsNullOrWhiteSpace(c.Name) && nameLookup.TryGetValue(c.EmployeeId, out var name)
                    ? name
                    : c.Name,
                c.Reason,
                c.SuggestedUtilisation))
            .ToList();

        return new SkillMatchResultDto(ranked, null);
    }

    private static decimal CalculateFreeUtilisation(Employee employee, DateOnly today)
    {
        var used = AllocationCapacityHelper.CalculateTotalUtilisation(employee.Allocations, today, today);
        return Math.Max(0, 100 - used);
    }

    private static bool PassesCapacityFilter(int? parsedHours, decimal freeUtilisation, int freeHours)
    {
        if (parsedHours is null)
            return freeUtilisation >= FullTimeFreeThreshold;

        return freeHours >= parsedHours.Value;
    }

    private async Task<IReadOnlyList<string>> GetRecentActivityTagsAsync(
        long employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var sinceWeek = WeekHelper.GetMondayOfWeek(today).AddDays(-28);
        return await timesheetRepository.GetRecentActivityTagsForEmployeeAsync(
            employeeId,
            sinceWeek,
            cancellationToken);
    }
}
