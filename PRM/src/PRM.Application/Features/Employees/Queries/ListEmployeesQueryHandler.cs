using MediatR;
using PRM.Application.Features.Employees.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Employees.Queries;

public sealed class ListEmployeesQueryHandler(
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser) : IRequestHandler<ListEmployeesQuery, IReadOnlyList<EmployeeListItemDto>>
{
    public async Task<IReadOnlyList<EmployeeListItemDto>> Handle(ListEmployeesQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        long? managerFilter = currentUser.Role switch
        {
            UserRole.Admin => null,
            UserRole.Manager => currentUser.UserId,
            _ => throw new ForbiddenException("Insufficient permissions to list employees.")
        };

        var employees = await employeeRepository.ListAsync(request.Status, request.Department, managerFilter, cancellationToken);
        return employees.Select(EmployeeMappings.ToListItemDto).ToList();
    }
}
