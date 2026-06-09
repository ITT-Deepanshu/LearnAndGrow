using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class TimesheetEntry : AuditableEntity
{
    private readonly List<ActivityTag> _activityTags = [];

    private TimesheetEntry() { }

    public long TimesheetId { get; private set; }
    public long ProjectId { get; private set; }
    public decimal Hours { get; private set; }

    public Timesheet Timesheet { get; private set; } = null!;
    public Project Project { get; private set; } = null!;
    public IReadOnlyCollection<ActivityTag> ActivityTags => _activityTags.AsReadOnly();

    public static TimesheetEntry Create(
        long projectId,
        decimal hours,
        IEnumerable<ActivityTag> tags,
        decimal maxHoursForProject,
        long actorId,
        DateTime utcNow)
    {
        if (hours < 0)
            throw new ValidationException("Hours cannot be negative.");
        if (hours > maxHoursForProject)
            throw new BusinessRuleException($"Hours cannot exceed {maxHoursForProject} for this project.");

        var tagList = tags.ToList();
        if (tagList.Count == 0)
            throw new ValidationException("At least one activity tag is required.");

        var entry = new TimesheetEntry
        {
            ProjectId = projectId,
            Hours = hours
        };
        entry._activityTags.AddRange(tagList);
        entry.SetCreated(actorId, utcNow);
        return entry;
    }
}
