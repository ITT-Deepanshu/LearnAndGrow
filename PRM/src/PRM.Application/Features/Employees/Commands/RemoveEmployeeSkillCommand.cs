using MediatR;

namespace PRM.Application.Features.Employees.Commands;

public sealed record RemoveEmployeeSkillCommand(long EmployeeId, long SkillId) : IRequest;
