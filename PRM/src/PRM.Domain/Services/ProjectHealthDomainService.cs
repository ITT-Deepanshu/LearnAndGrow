using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.ValueObjects;

namespace PRM.Domain.Services;

public class ProjectHealthDomainService
{
    public ProjectHealthResult Evaluate(
        Project project,
        DateOnly today,
        IReadOnlyList<(string EmployeeName, decimal ExpectedHours, decimal LoggedHours)> effortData)
    {
        var flags = new List<string>();
        var hasOverdueMilestone = false;
        var hasLowEffort = false;
        var hasCriticalLowEffort = false;

        foreach (var milestone in project.Milestones)
        {
            if (milestone.IsOverdue(today))
            {
                hasOverdueMilestone = true;
                flags.Add($"{milestone.Title} milestone is {(today.DayNumber - milestone.DueDate.DayNumber)} days overdue");
            }
        }

        foreach (var (name, expected, logged) in effortData)
        {
            if (expected > 0 && logged < expected * 0.5m)
            {
                hasCriticalLowEffort = true;
                flags.Add($"{name} logged only {logged} hrs last week (expected {expected} hrs)");
            }
            else if (expected > 0 && logged < expected * 0.8m)
            {
                hasLowEffort = true;
                flags.Add($"{name} logged below expected effort ({logged}/{expected} hrs)");
            }
        }

        if (project.Allocations.Count > 0 && !hasOverdueMilestone && !hasLowEffort && !hasCriticalLowEffort)
            flags.Add("Resources are correctly allocated");

        var status = DetermineStatus(hasOverdueMilestone, hasCriticalLowEffort, hasLowEffort, project, today);
        return ProjectHealthResult.Create(status, flags);
    }

    private static HealthStatus DetermineStatus(
        bool hasOverdueMilestone,
        bool hasCriticalLowEffort,
        bool hasLowEffort,
        Project project,
        DateOnly today)
    {
        if (hasOverdueMilestone && hasCriticalLowEffort)
            return HealthStatus.Red;

        if (hasOverdueMilestone || hasCriticalLowEffort)
            return HealthStatus.Red;

        if (project.EndDate < today.AddDays(14)
            && project.Milestones.Any(m => m.Status == MilestoneStatus.NotStarted))
            return HealthStatus.Red;

        if (hasLowEffort)
            return HealthStatus.Yellow;

        return HealthStatus.Green;
    }
}
