using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class ResourceProfile : AuditableEntity
{
    private readonly List<ResourceProfileSkill> _skills = [];
    private readonly List<Allocation> _allocations = [];

    private ResourceProfile() { }

    public long UserId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string Designation { get; private set; } = string.Empty;
    public ResourceProfileStatus Status { get; private set; } = ResourceProfileStatus.Bench;
    public long? ManagerId { get; private set; }
    public DateTime? JoinedAt { get; private set; }
    public bool TimesheetSubmissionFrozen { get; private set; }
    public int TimesheetReminderCount { get; private set; }
    public DateOnly? MissedTimesheetWeekStart { get; private set; }

    public User User { get; private set; } = null!;
    public User? Manager { get; private set; }
    public IReadOnlyCollection<ResourceProfileSkill> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<Allocation> Allocations => _allocations.AsReadOnly();

    public static ResourceProfile Create(
        long userId,
        string fullName,
        string department,
        string designation,
        long createdBy,
        DateTime utcNow)
    {
        var profile = new ResourceProfile
        {
            UserId = userId,
            FullName = fullName.Trim(),
            Department = department.Trim(),
            Designation = designation.Trim(),
            Status = ResourceProfileStatus.Bench,
            JoinedAt = utcNow
        };
        profile.SetCreated(createdBy, utcNow);
        return profile;
    }

    public void Update(string fullName, string department, string designation, long actorId, DateTime utcNow)
    {
        FullName = fullName.Trim();
        Department = department.Trim();
        Designation = designation.Trim();
        SetModified(actorId, utcNow);
    }

    public void AssignManager(long managerUserId, long actorId, DateTime utcNow)
    {
        ManagerId = managerUserId;
        SetModified(actorId, utcNow);
    }

    public void Deactivate(long actorId, DateTime utcNow)
    {
        Status = ResourceProfileStatus.Inactive;
        SetModified(actorId, utcNow);
    }

    public void Reactivate(long actorId, DateTime utcNow)
    {
        Status = ResourceProfileStatus.Bench;
        SetModified(actorId, utcNow);
    }

    public bool RecomputeStatus(decimal totalUtilisation, long actorId = 0, DateTime? utcNow = null)
    {
        if (Status == ResourceProfileStatus.Inactive) return false;

        var newStatus = totalUtilisation switch
        {
            <= 0 => ResourceProfileStatus.Bench,
            < 100 => ResourceProfileStatus.PartiallyAllocated,
            _ => ResourceProfileStatus.Allocated
        };

        if (Status == newStatus) return false;

        Status = newStatus;
        if (utcNow.HasValue)
            SetModified(actorId, utcNow.Value);

        return true;
    }

    public ResourceProfileSkill AddSkill(string name, SkillCategory category, SkillProficiency proficiency, long actorId, DateTime utcNow)
    {
        if (_skills.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"Skill '{name}' already exists for this resource.");

        var skill = ResourceProfileSkill.Create(Id, name, category, proficiency, actorId, utcNow);
        _skills.Add(skill);
        return skill;
    }

    public void UpdateSkillProficiency(long skillId, SkillProficiency proficiency, long actorId, DateTime utcNow)
    {
        var skill = _skills.FirstOrDefault(s => s.Id == skillId)
            ?? throw new NotFoundException("Skill not found.");
        skill.UpdateProficiency(proficiency, actorId, utcNow);
    }

    public void RemoveSkill(long skillId)
    {
        var skill = _skills.FirstOrDefault(s => s.Id == skillId)
            ?? throw new NotFoundException("Skill not found.");
        _skills.Remove(skill);
    }

    public void SyncMissedTimesheetWeek(DateOnly weekStart, long actorId, DateTime utcNow)
    {
        if (MissedTimesheetWeekStart == weekStart)
            return;

        MissedTimesheetWeekStart = weekStart;
        TimesheetReminderCount = 0;
        SetModified(actorId, utcNow);
    }

    public void RecordTimesheetReminderSent(long actorId, DateTime utcNow)
    {
        TimesheetReminderCount++;
        SetModified(actorId, utcNow);
    }

    public void FreezeTimesheetSubmission(long actorId, DateTime utcNow)
    {
        TimesheetSubmissionFrozen = true;
        SetModified(actorId, utcNow);
    }

    public void RestoreTimesheetSubmission(long actorId, DateTime utcNow)
    {
        TimesheetSubmissionFrozen = false;
        TimesheetReminderCount = 0;
        SetModified(actorId, utcNow);
    }

    public void ClearTimesheetCompliance(long actorId, DateTime utcNow)
    {
        TimesheetSubmissionFrozen = false;
        TimesheetReminderCount = 0;
        MissedTimesheetWeekStart = null;
        SetModified(actorId, utcNow);
    }
}
