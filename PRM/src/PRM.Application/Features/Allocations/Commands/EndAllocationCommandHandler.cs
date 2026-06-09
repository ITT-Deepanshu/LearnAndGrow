using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Allocations.Commands;

public sealed class EndAllocationCommandHandler(
    IAllocationRepository allocationRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<EndAllocationCommand>
{
    public async Task Handle(EndAllocationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        var allocation = await allocationRepository.GetByIdAsync(request.AllocationId, cancellationToken)
            ?? throw new NotFoundException("Allocation not found.");

        if (allocation.Project.ManagerId != currentUser.UserId.Value)
            throw new ForbiddenException("You do not own this project.");

        allocation.End(clock.Today, currentUser.UserId.Value, clock.UtcNow);

        var employee = await employeeRepository.GetByIdAsync(allocation.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        var today = clock.Today;
        var activeAllocations = await allocationRepository.ListActiveForEmployeeAsync(
            employee.Id,
            today,
            today,
            cancellationToken);

        var total = AllocationCapacityHelper.CalculateTotalUtilisation(activeAllocations, today, today);
        employee.RecomputeStatus(total);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
