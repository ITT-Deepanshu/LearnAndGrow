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

    public void RecomputeStatus(decimal totalUtilisation)
    {
        if (Status == ResourceProfileStatus.Inactive) return;

        Status = totalUtilisation switch
        {
            <= 0 => ResourceProfileStatus.Bench,
            < 100 => ResourceProfileStatus.PartiallyAllocated,
            _ => ResourceProfileStatus.Allocated
        };
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
}
