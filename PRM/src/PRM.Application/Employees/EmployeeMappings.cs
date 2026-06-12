using PRM.Application.Employees;
using PRM.Domain.Entities;

namespace PRM.Application.Employees;

internal static class EmployeeMappings
{
    internal static EmployeeListItemDto ToListItemDto(ResourceProfile profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.Department,
            profile.Designation,
            profile.Status.ToString(),
            profile.ManagerId,
            profile.Manager?.ResourceProfile?.FullName ?? string.Empty,
            profile.Skills.Count);

    internal static ResourceProfileSkillDto ToSkillDto(ResourceProfileSkill skill) =>
        new(
            skill.Id,
            skill.Name,
            skill.Category.ToString(),
            skill.Proficiency.ToString());

    internal static EmployeeDetailDto ToDetailDto(ResourceProfile profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.User.Username,
            profile.User.Email,
            profile.FullName,
            profile.Department,
            profile.Designation,
            profile.Status.ToString(),
            profile.ManagerId,
            profile.Manager?.ResourceProfile?.FullName ?? string.Empty,
            profile.JoinedAt,
            profile.Skills.Select(ToSkillDto).ToList());
}
