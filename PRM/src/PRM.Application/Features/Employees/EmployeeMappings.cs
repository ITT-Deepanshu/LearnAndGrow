using PRM.Application.Features.Employees.Dtos;
using PRM.Domain.Entities;

namespace PRM.Application.Features.Employees;

internal static class EmployeeMappings
{
    internal static EmployeeListItemDto ToListItemDto(Employee employee) =>
        new(
            employee.Id,
            employee.UserId,
            employee.User.FullName,
            employee.Department,
            employee.Designation,
            employee.Status.ToString(),
            employee.ManagerId,
            employee.Manager?.FullName ?? string.Empty,
            employee.Skills.Count);

    internal static EmployeeSkillDto ToSkillDto(EmployeeSkill skill) =>
        new(
            skill.Id,
            skill.Name,
            skill.Category.ToString(),
            skill.Proficiency.ToString());

    internal static EmployeeDetailDto ToDetailDto(Employee employee) =>
        new(
            employee.Id,
            employee.UserId,
            employee.User.Username,
            employee.User.Email,
            employee.User.FullName,
            employee.Department,
            employee.Designation,
            employee.Status.ToString(),
            employee.ManagerId,
            employee.Manager?.FullName ?? string.Empty,
            employee.JoinedAt,
            employee.Skills.Select(ToSkillDto).ToList());
}
