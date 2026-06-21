using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class Project : AuditableEntity
{
    private readonly List<ProjectMilestone> _milestones = [];
    private readonly List<Allocation> _allocations = [];

    private Project() { }

    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public ProjectStatus Status { get; private set; }
    public long ManagerId { get; private set; }
    public int TotalStoryPoints { get; private set; }
    public HealthStatus Health { get; private set; } = HealthStatus.Green;
    public string? HealthReason { get; private set; }
    public DateTime? AtRiskNotificationSentAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public User Manager { get; private set; } = null!;
    public IReadOnlyCollection<ProjectMilestone> Milestones => _milestones.AsReadOnly();
    public IReadOnlyCollection<Allocation> Allocations => _allocations.AsReadOnly();

    public static Project Create(
        string name,
        string description,
        DateOnly startDate,
        DateOnly endDate,
        ProjectStatus status,
        long managerId,
        int totalStoryPoints,
        long createdBy,
        DateTime utcNow)
    {
        if (endDate < startDate)
            throw new ValidationException("End date must be on or after start date.");

        var project = new Project
        {
            Name = name.Trim(),
            Description = description.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            ManagerId = managerId,
            TotalStoryPoints = totalStoryPoints,
            Health = HealthStatus.Green
        };
        project.SetCreated(createdBy, utcNow);
        return project;
    }

    public void Update(
        string name,
        string description,
        DateOnly startDate,
        DateOnly endDate,
        ProjectStatus status,
        long managerId,
        int totalStoryPoints,
        long actorId,
        DateTime utcNow)
    {
        if (endDate < startDate)
            throw new ValidationException("End date must be on or after start date.");

        Name = name.Trim();
        Description = description.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        ManagerId = managerId;
        TotalStoryPoints = totalStoryPoints;
        SetModified(actorId, utcNow);
    }

    public bool CanReceiveAllocations() =>
        Status is ProjectStatus.Planned or ProjectStatus.Active;

    public ProjectMilestone AddMilestone(string title, DateOnly dueDate, int storyPoints, long actorId, DateTime utcNow)
    {
        if (dueDate < StartDate || dueDate > EndDate)
            throw new BusinessRuleException("Milestone due date must be within the project period.");

        var milestone = ProjectMilestone.Create(Id, title, dueDate, storyPoints, actorId, utcNow);
        _milestones.Add(milestone);
        return milestone;
    }

    public void UpdateMilestoneStatus(long milestoneId, MilestoneStatus status, long actorId, DateTime utcNow)
    {
        var milestone = _milestones.FirstOrDefault(m => m.Id == milestoneId)
            ?? throw new NotFoundException("Milestone not found.");
        milestone.UpdateStatus(status, actorId, utcNow);
    }

    public bool SetHealth(HealthStatus health, string reason, long actorId, DateTime utcNow)
    {
        var clearAtRiskFlag = health != HealthStatus.Red && AtRiskNotificationSentAt is not null;
        if (Health == health && HealthReason == reason && !clearAtRiskFlag)
            return false;

        Health = health;
        HealthReason = reason;
        if (health != HealthStatus.Red)
            AtRiskNotificationSentAt = null;
        SetModified(actorId, utcNow);
        return true;
    }

    public void MarkAtRiskNotificationSent(DateTime utcNow, long actorId)
    {
        AtRiskNotificationSentAt = utcNow;
        SetModified(actorId, utcNow);
    }
}
