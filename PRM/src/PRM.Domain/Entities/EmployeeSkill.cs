using PRM.Domain.Enums;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class EmployeeSkill : AuditableEntity
{
    private EmployeeSkill() { }

    public long EmployeeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SkillCategory Category { get; private set; }
    public SkillProficiency Proficiency { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public static EmployeeSkill Create(
        long employeeId,
        string name,
        SkillCategory category,
        SkillProficiency proficiency,
        long actorId,
        DateTime utcNow)
    {
        var skill = new EmployeeSkill
        {
            EmployeeId = employeeId,
            Name = name.Trim(),
            Category = category,
            Proficiency = proficiency
        };
        skill.SetCreated(actorId, utcNow);
        return skill;
    }

    public void UpdateProficiency(SkillProficiency proficiency, long actorId, DateTime utcNow)
    {
        Proficiency = proficiency;
        SetModified(actorId, utcNow);
    }
}
