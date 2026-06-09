using MediatR;

namespace PRM.Application.Features.Employees.Commands;

public sealed record DeactivateEmployeeCommand(long EmployeeId) : IRequest;
