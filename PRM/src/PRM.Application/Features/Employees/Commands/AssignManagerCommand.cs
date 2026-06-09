using MediatR;

namespace PRM.Application.Features.Employees.Commands;

public sealed record AssignManagerCommand(long EmployeeId, long ManagerUserId) : IRequest;
