using PRM.Domain.Enums;

namespace PRM.Application.Features.Employees.Dtos;

public sealed record UpdateEmployeeDto(string Department, string Designation);

public sealed record AssignManagerDto(long ManagerId);

public sealed record AddEmployeeSkillDto(string Name, SkillCategory Category, SkillProficiency Proficiency);

public sealed record UpdateSkillProficiencyDto(SkillProficiency Proficiency);

public sealed record EmployeeListItemDto(
    long Id,
    long UserId,
    string FullName,
    string Department,
    string Designation,
    string Status,
    long? ManagerId,
    string ManagerName,
    int SkillCount);

public sealed record EmployeeSkillDto(
    long Id,
    string Name,
    string Category,
    string Proficiency);

public sealed record EmployeeDetailDto(
    long Id,
    long UserId,
    string Username,
    string Email,
    string FullName,
    string Department,
    string Designation,
    string Status,
    long? ManagerId,
    string ManagerName,
    DateTime? JoinedAt,
    IReadOnlyList<EmployeeSkillDto> Skills);
