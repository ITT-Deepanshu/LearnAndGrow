using MediatR;

namespace PRM.Application.Features.Employees.Commands;

public sealed record UpdateEmployeeCommand(long EmployeeId, string Department, string Designation) : IRequest;
