using MediatR;
using PRM.Application.Features.Employees.Dtos;
using PRM.Domain.Enums;

namespace PRM.Application.Features.Employees.Queries;

public sealed record ListEmployeesQuery(EmployeeStatus? Status, string? Department) : IRequest<IReadOnlyList<EmployeeListItemDto>>;
