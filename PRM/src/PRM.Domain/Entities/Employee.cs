using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class Employee : AuditableEntity
{
    private readonly List<EmployeeSkill> _skills = [];
    private readonly List<Allocation> _allocations = [];

    private Employee() { }

    public long UserId { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string Designation { get; private set; } = string.Empty;
    public EmployeeStatus Status { get; private set; } = EmployeeStatus.Bench;
    public long? ManagerId { get; private set; }
    public DateTime? JoinedAt { get; private set; }

    public User User { get; private set; } = null!;
    public User? Manager { get; private set; }
    public IReadOnlyCollection<EmployeeSkill> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<Allocation> Allocations => _allocations.AsReadOnly();

    public static Employee Create(
        long userId,
        string department,
        string designation,
        long createdBy,
        DateTime utcNow)
    {
        var employee = new Employee
        {
            UserId = userId,
            Department = department.Trim(),
            Designation = designation.Trim(),
            Status = EmployeeStatus.Bench,
            JoinedAt = utcNow
        };
        employee.SetCreated(createdBy, utcNow);
        return employee;
    }

    public void Update(string department, string designation, long actorId, DateTime utcNow)
    {
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
        Status = EmployeeStatus.Inactive;
        SetModified(actorId, utcNow);
    }

    public void Reactivate(long actorId, DateTime utcNow)
    {
        Status = EmployeeStatus.Bench;
        SetModified(actorId, utcNow);
    }

    public void RecomputeStatus(decimal totalUtilisation)
    {
        if (Status == EmployeeStatus.Inactive) return;

        Status = totalUtilisation switch
        {
            <= 0 => EmployeeStatus.Bench,
            < 100 => EmployeeStatus.PartiallyAllocated,
            _ => EmployeeStatus.Allocated
        };
    }

    public EmployeeSkill AddSkill(string name, SkillCategory category, SkillProficiency proficiency, long actorId, DateTime utcNow)
    {
        if (_skills.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new ConflictException($"Skill '{name}' already exists for this employee.");

        var skill = EmployeeSkill.Create(Id, name, category, proficiency, actorId, utcNow);
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
