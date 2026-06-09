using MediatR;
using PRM.Application.Features.Employees.Dtos;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Employees.Commands;

public sealed record AddEmployeeSkillCommand(
    long EmployeeId,
    string Name,
    SkillCategory Category,
    SkillProficiency Proficiency) : IRequest<EmployeeSkillDto>;
