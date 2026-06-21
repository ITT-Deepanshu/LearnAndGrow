using PRM.Application.Common;
using PRM.Application.Interfaces.Ai;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;
using PRM.Domain.Services;

namespace PRM.Application.Ai;

public sealed class AiService(
    IProjectRepository projectRepository,
    IResourceProfileRepository employeeRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigRepository systemConfigRepository,
    ProjectEffortBuilder effortBuilder,
    ProjectHealthDomainService healthService,
    IAiProviderOrchestrator aiOrchestrator,
    ICurrentUser currentUser,
    IClock clock) : IAiService
{
    private const decimal FullTimeFreeThreshold = 50m;

    public async Task<SkillMatchResultDto> FindResourcesAsync(long projectId, string requirement, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureManager(currentUser);

        if (string.IsNullOrWhiteSpace(requirement))
            throw new ValidationException("Requirement text is required.");

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        CurrentUserGuards.EnsureOwnsProject(currentUser, project.ManagerId);

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var parsedHours = HoursPerWeekParser.Parse(requirement);
        var today = clock.Today;

        var bench = await employeeRepository.ListAsync(ResourceProfileStatus.Bench, null, null, cancellationToken);
        var partial = await employeeRepository.ListAsync(ResourceProfileStatus.PartiallyAllocated, null, null, cancellationToken);
        var allCandidates = bench.Concat(partial)
            .Where(AllocationEligibility.IsEligibleActiveResource)
            .ToList();

        var contexts = new List<CandidateAiContext>();
        foreach (var resourceProfile in allCandidates)
        {
            var freeUtilisation = CalculateFreeUtilisation(resourceProfile, today);
            var freeHours = (int)Math.Floor(freeUtilisation / 100m * config.MaxWeeklyHours);

            if (!PassesCapacityFilter(parsedHours, freeUtilisation, freeHours))
                continue;

            contexts.Add(new CandidateAiContext(
                resourceProfile.Id,
                resourceProfile.FullName,
                resourceProfile.Department,
                resourceProfile.Skills.Select(s => s.Name).ToList(),
                freeUtilisation,
                freeHours,
                await GetRecentActivityTagsAsync(resourceProfile.Id, today, cancellationToken)));
        }

        if (contexts.Count == 0)
            return new SkillMatchResultDto([], "No active resource-role employees have the required free capacity for this request.");

        var aiResponse = await aiOrchestrator.MatchSkillsAsync(
            new SkillMatchAiRequest(requirement.Trim(), project.Name, contexts),
            cancellationToken);

        var nameLookup = contexts.ToDictionary(c => c.ResourceProfileId, c => c.Name);
        var eligibleIds = contexts.Select(c => c.ResourceProfileId).ToHashSet();
        var ranked = aiResponse.Candidates
            .Where(c => eligibleIds.Contains(c.ResourceProfileId) && c.SuggestedUtilisation is > 0)
            .Select(c => new RankedCandidateDto(
                c.ResourceProfileId,
                string.IsNullOrWhiteSpace(c.Name) && nameLookup.TryGetValue(c.ResourceProfileId, out var name) ? name : c.Name,
                c.Reason,
                c.SuggestedUtilisation))
            .ToList();

        return new SkillMatchResultDto(ranked, null);
    }

    public async Task<TeamSkillMatchResultDto> MatchTeamAsync(string requirement, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureManager(currentUser);

        if (string.IsNullOrWhiteSpace(requirement))
            throw new ValidationException("Requirement text is required.");

        var today = clock.Today;

        var bench = await employeeRepository.ListAsync(ResourceProfileStatus.Bench, null, null, cancellationToken);
        var partial = await employeeRepository.ListAsync(ResourceProfileStatus.PartiallyAllocated, null, null, cancellationToken);
        var allocated = await employeeRepository.ListAsync(ResourceProfileStatus.Allocated, null, null, cancellationToken);

        var benchCandidates = bench
            .Where(AllocationEligibility.IsEligibleActiveResource)
            .ToList();

        var benchContexts = benchCandidates
            .Select(BuildBenchEmployeeContext)
            .ToList();

        var allocatedContexts = partial.Concat(allocated)
            .Where(AllocationEligibility.IsEligibleActiveResource)
            .Select(p => BuildAllocatedEmployeeContext(p, today))
            .ToList();

        string? note = null;
        if (benchContexts.Count == 0)
            note = "No active resource-role employees are currently on bench.";

        var aiResponse = await aiOrchestrator.MatchTeamAsync(
            new TeamSkillMatchAiRequest(requirement.Trim(), benchContexts, allocatedContexts),
            cancellationToken);

        var lookup = benchCandidates.ToDictionary(c => c.Id);

        var teamDefined = aiResponse.TeamDefined
            .Select(r => new TeamRoleDefinitionDto(
                r.RoleTitle,
                r.Count,
                r.RequiredSkills.Select(s => new RequiredSkillDto(s.Name, s.MinProficiency)).ToList()))
            .ToList();

        var assignments = aiResponse.Assignments
            .Where(a => lookup.ContainsKey(a.ResourceProfileId))
            .Select(a =>
            {
                var profile = lookup[a.ResourceProfileId];
                return new TeamAssignmentDto(
                    a.RoleTitle,
                    a.SlotNumber,
                    a.ResourceProfileId,
                    profile.FullName,
                    GetManagerName(profile),
                    profile.Skills.Select(s => $"{s.Name} ({s.Proficiency})").ToList(),
                    a.Why);
            })
            .ToList();

        var unfilled = aiResponse.Unfilled
            .Select(u => new UnfilledRoleDto(u.RoleTitle, u.UnfilledCount, u.Reason, u.Detail))
            .ToList();

        if (unfilled.Count > 0 && note is null)
            note = "Some roles could not be filled. Review gaps for hiring or availability planning.";

        return new TeamSkillMatchResultDto(requirement.Trim(), teamDefined, assignments, unfilled, note);
    }

    public async Task<RiskSummaryDto> GetProjectRiskSummaryAsync(long projectId, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureManager(currentUser);

        var project = await projectRepository.GetByIdWithDetailsAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        CurrentUserGuards.EnsureOwnsProject(currentUser, project.ManagerId);

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var today = clock.Today;
        var lastWeekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        var activeAllocations = (await allocationRepository.ListActiveOnProjectAsync(project.Id, cancellationToken))
            .Where(a => a.IsActiveOn(today))
            .ToList();

        var effortData = await effortBuilder.BuildAsync(
            activeAllocations,
            project.Id,
            lastWeekStart,
            config.MaxWeeklyHours,
            cancellationToken);

        var health = healthService.Evaluate(project, today, effortData);
        var aiResponse = await aiOrchestrator.SummarizeRiskAsync(
            new RiskSummaryAiRequest(
                project.Name,
                project.Milestones.Select(m => new MilestoneAiContext(m.Title, m.DueDate, m.Status.ToString())).ToList(),
                activeAllocations.Select(a => $"{a.ResourceProfile.FullName} ({a.UtilisationPercentage}%)").ToList(),
                effortData.Select(e => new EffortAiContext(e.EmployeeName, e.ExpectedHours, e.LoggedHours)).ToList()),
            cancellationToken);

        return new RiskSummaryDto(
            aiResponse.Paragraph,
            new RiskSummarySnapshotDto(
                health.RiskFlags.Where(f => f.Contains("overdue", StringComparison.OrdinalIgnoreCase)).ToList(),
                health.RiskFlags.Where(f => f.Contains("logged", StringComparison.OrdinalIgnoreCase)).ToList(),
                health.DisplayLabel));
    }

    private static decimal CalculateFreeUtilisation(ResourceProfile resourceProfile, DateOnly today)
    {
        var used = AllocationCapacityHelper.CalculateTotalUtilisation(resourceProfile.Allocations, today, today);
        return Math.Max(0, 100 - used);
    }

    private static bool PassesCapacityFilter(int? parsedHours, decimal freeUtilisation, int freeHours) =>
        parsedHours is null ? freeUtilisation >= FullTimeFreeThreshold : freeHours >= parsedHours.Value;

    private async Task<IReadOnlyList<string>> GetRecentActivityTagsAsync(
        long employeeId, DateOnly today, CancellationToken cancellationToken)
    {
        var sinceWeek = WeekHelper.GetMondayOfWeek(today).AddDays(-28);
        return await timesheetRepository.GetRecentActivityTagsForEmployeeAsync(employeeId, sinceWeek, cancellationToken);
    }

    private static BenchEmployeeAiContext BuildBenchEmployeeContext(ResourceProfile profile) =>
        new(
            profile.Id,
            profile.FullName,
            GetManagerName(profile),
            profile.Skills.Select(s => $"{s.Name} ({s.Proficiency})").ToList());

    private static AllocatedEmployeeAiContext BuildAllocatedEmployeeContext(ResourceProfile profile, DateOnly today)
    {
        var activeAllocations = profile.Allocations
            .Where(a => a.IsActiveOn(today))
            .Select(a => $"{a.Project.Name} ({a.UtilisationPercentage}%, until {a.ToDate:yyyy-MM-dd})")
            .ToList();

        return new AllocatedEmployeeAiContext(
            profile.Id,
            profile.FullName,
            profile.Skills.Select(s => $"{s.Name} ({s.Proficiency})").ToList(),
            activeAllocations);
    }

    private static string GetManagerName(ResourceProfile profile) =>
        profile.Manager?.ResourceProfile?.FullName ?? "Unassigned";
}
