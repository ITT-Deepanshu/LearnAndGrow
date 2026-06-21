using PRM.Domain.Enums;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class ProjectMilestone : AuditableEntity
{
    private ProjectMilestone() { }

    public long ProjectId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly DueDate { get; private set; }
    public int StoryPoints { get; private set; }
    public MilestoneStatus Status { get; private set; } = MilestoneStatus.NotStarted;

    public Project Project { get; private set; } = null!;

    public static ProjectMilestone Create(
        long projectId,
        string title,
        DateOnly dueDate,
        int storyPoints,
        long actorId,
        DateTime utcNow)
    {
        var milestone = new ProjectMilestone
        {
            ProjectId = projectId,
            Title = title.Trim(),
            DueDate = dueDate,
            StoryPoints = storyPoints,
            Status = MilestoneStatus.NotStarted
        };
        milestone.SetCreated(actorId, utcNow);
        return milestone;
    }

    public void UpdateStatus(MilestoneStatus status, long actorId, DateTime utcNow)
    {
        Status = status;
        SetModified(actorId, utcNow);
    }

    public bool IsOverdue(DateOnly today) =>
        Status is not MilestoneStatus.Done && DueDate < today;
}
