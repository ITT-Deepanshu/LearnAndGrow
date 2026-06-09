using MediatR;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Employees.Commands;

public sealed record UpdateSkillProficiencyCommand(
    long EmployeeId,
    long SkillId,
    SkillProficiency Proficiency) : IRequest;
