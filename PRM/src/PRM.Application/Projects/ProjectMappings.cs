using PRM.Application.Projects;
using PRM.Domain.Entities;

namespace PRM.Application.Projects;

internal static class ProjectMappings
{
    internal static ProjectDto ToDto(Project project) =>
        new(
            project.Id,
            project.Name,
            project.Description,
            project.StartDate,
            project.EndDate,
            project.Status.ToString(),
            project.ManagerId,
            project.Manager?.ResourceProfile?.FullName ?? project.Manager?.Username ?? string.Empty,
            project.TotalStoryPoints,
            project.Health.ToString());

    internal static ProjectListItemDto ToListItemDto(Project project) =>
        new(
            project.Id,
            project.Name,
            project.Manager?.ResourceProfile?.FullName ?? project.Manager?.Username ?? string.Empty,
            project.EndDate,
            project.Status.ToString(),
            project.Health.ToString());

    internal static MilestoneDto ToMilestoneDto(ProjectMilestone milestone) =>
        new(
            milestone.Id,
            milestone.ProjectId,
            milestone.Title,
            milestone.DueDate,
            milestone.StoryPoints,
            milestone.Status.ToString());

    internal static ProjectDetailDto ToDetailDto(Project project) =>
        new(
            project.Id,
            project.Name,
            project.Description,
            project.StartDate,
            project.EndDate,
            project.Status.ToString(),
            project.ManagerId,
            project.Manager?.ResourceProfile?.FullName ?? project.Manager?.Username ?? string.Empty,
            project.TotalStoryPoints,
            project.Health.ToString(),
            project.HealthReason,
            project.Milestones.Select(ToMilestoneDto).ToList(),
            project.Allocations
                .Where(a => a.EndedAt is null)
                .Select(a => new AllocationSummaryDto(
                    a.Id,
                    a.ResourceProfileId,
                    a.ResourceProfile?.FullName ?? string.Empty,
                    a.UtilisationPercentage,
                    a.FromDate,
                    a.ToDate))
                .ToList());
}
