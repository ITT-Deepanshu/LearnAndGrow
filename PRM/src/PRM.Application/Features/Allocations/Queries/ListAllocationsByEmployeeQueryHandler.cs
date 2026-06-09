using MediatR;
using PRM.Application.Features.Allocations.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Allocations.Queries;

public sealed class ListAllocationsByEmployeeQueryHandler(
    IEmployeeRepository employeeRepository,
    IAllocationRepository allocationRepository,
    ICurrentUser currentUser) : IRequestHandler<ListAllocationsByEmployeeQuery, IReadOnlyList<AllocationDto>>
{
    public async Task<IReadOnlyList<AllocationDto>> Handle(ListAllocationsByEmployeeQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        if (currentUser.Role == UserRole.Admin)
        {
            // Admin can view any employee's allocations.
        }
        else if (currentUser.Role == UserRole.Employee)
        {
            if (employee.UserId != currentUser.UserId)
                throw new ForbiddenException("You can only view your own allocations.");
        }
        else
        {
            throw new ForbiddenException("Insufficient permissions to view employee allocations.");
        }

        var allocations = await allocationRepository.ListAllAsync(request.EmployeeId, null, cancellationToken);
        return allocations.Select(AllocationMappings.ToDto).ToList();
    }
}
