using PRM.Domain.Enums;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class ResourceProfileSkill : AuditableEntity
{
    private ResourceProfileSkill() { }

    public long ResourceProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SkillCategory Category { get; private set; }
    public SkillProficiency Proficiency { get; private set; }

    public ResourceProfile ResourceProfile { get; private set; } = null!;

    public static ResourceProfileSkill Create(
        long resourceProfileId,
        string name,
        SkillCategory category,
        SkillProficiency proficiency,
        long actorId,
        DateTime utcNow)
    {
        var skill = new ResourceProfileSkill
        {
            ResourceProfileId = resourceProfileId,
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
