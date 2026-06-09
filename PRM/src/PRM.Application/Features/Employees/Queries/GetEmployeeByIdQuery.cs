using MediatR;
using PRM.Application.Features.Employees.Dtos;

namespace PRM.Application.Features.Employees.Queries;

public sealed record GetEmployeeByIdQuery(long EmployeeId) : IRequest<EmployeeDetailDto>;
