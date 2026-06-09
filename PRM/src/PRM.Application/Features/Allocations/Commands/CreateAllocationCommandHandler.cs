using MediatR;
using PRM.Application.Features.Allocations.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Factories;
using PRM.Domain.Helpers;

namespace PRM.Application.Features.Allocations.Commands;

public sealed class CreateAllocationCommandHandler(
    IProjectRepository projectRepository,
    IEmployeeRepository employeeRepository,
    IAllocationRepository allocationRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CreateAllocationCommand, AllocationDto>
{
    public async Task<AllocationDto> Handle(CreateAllocationCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (project.ManagerId != currentUser.UserId.Value)
            throw new ForbiddenException("You do not own this project.");

        if (!project.CanReceiveAllocations())
            throw new BusinessRuleException("Allocations can only be created for Planned or Active projects.");

        if (request.FromDate < project.StartDate || request.ToDate > project.EndDate)
            throw new BusinessRuleException("Allocation period must be within the project period.");

        if (request.ToDate < clock.Today)
            throw new BusinessRuleException("Allocation end date cannot be before today.");

        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee not found.");

        if (employee.Status == EmployeeStatus.Inactive || !employee.User.IsActive)
            throw new BusinessRuleException("Employee is not active.");

        var existingAllocations = await allocationRepository.ListActiveForEmployeeAsync(
            request.EmployeeId,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        if (AllocationCapacityHelper.WouldExceedCapacity(
                existingAllocations,
                request.FromDate,
                request.ToDate,
                request.UtilisationPercentage))
        {
            throw new BusinessRuleException("Total utilisation would exceed 100% for the requested period.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var allocation = AllocationFactory.Create(
                request.EmployeeId,
                request.ProjectId,
                request.UtilisationPercentage,
                request.FromDate,
                request.ToDate,
                currentUser.UserId.Value,
                clock.UtcNow);

            allocationRepository.Add(allocation);
            await RecomputeEmployeeStatusAsync(employee, allocation, cancellationToken);

            auditLogRepository.Add(AuditLog.Create(
                currentUser.UserId,
                "ALLOCATION_CREATED",
                nameof(Allocation),
                null,
                $"employee={request.EmployeeId};project={request.ProjectId}",
                clock.UtcNow));

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            allocation = await allocationRepository.GetByIdAsync(allocation.Id, cancellationToken)
                ?? throw new NotFoundException("Allocation not found after creation.");

            return AllocationMappings.ToDto(allocation);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task RecomputeEmployeeStatusAsync(
        Employee employee,
        Allocation? pendingAllocation,
        CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var allocations = (await allocationRepository.ListActiveForEmployeeAsync(
            employee.Id,
            today,
            today,
            cancellationToken)).ToList();

        if (pendingAllocation is not null && pendingAllocation.IsActiveOn(today))
            allocations.Add(pendingAllocation);

        var total = AllocationCapacityHelper.CalculateTotalUtilisation(allocations, today, today);
        employee.RecomputeStatus(total);
    }
}
