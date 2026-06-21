using PRM.Application.Allocations;
using PRM.Application.Common;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;

namespace PRM.Application.Allocations;

public sealed class AllocationService(
    IProjectRepository projectRepository,
    IResourceProfileRepository employeeRepository,
    IAllocationRepository allocationRepository,
    IAuditLogRepository auditLogRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IAllocationService
{
    public async Task<AllocationDto> CreateAllocationAsync(CreateAllocationDto dto, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        var project = await projectRepository.GetByIdAsync(dto.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (project.ManagerId != currentUser.UserId.Value)
            throw new ForbiddenException("You do not own this project.");

        if (!project.CanReceiveAllocations())
            throw new BusinessRuleException("Allocations can only be created for Planned or Active projects.");

        if (dto.FromDate < project.StartDate || dto.ToDate > project.EndDate)
            throw new BusinessRuleException("Allocation period must be within the project period.");

        if (dto.ToDate < clock.Today)
            throw new BusinessRuleException("Allocation end date cannot be before today.");

        var resourceProfile = await employeeRepository.GetByIdAsync(dto.ResourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        if (!AllocationEligibility.IsEligibleActiveResource(resourceProfile))
            throw new BusinessRuleException("Only active employees with the Resource role can be allocated to projects.");

        var existingAllocations = await allocationRepository.ListActiveForEmployeeAsync(
            dto.ResourceProfileId,
            dto.FromDate,
            dto.ToDate,
            cancellationToken);

        if (AllocationCapacityHelper.WouldExceedCapacity(
                existingAllocations,
                dto.FromDate,
                dto.ToDate,
                dto.UtilisationPercentage))
        {
            throw new BusinessRuleException("Total utilisation would exceed 100% for the requested period.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var allocation = Allocation.Create(
                dto.ResourceProfileId,
                dto.ProjectId,
                dto.UtilisationPercentage,
                dto.FromDate,
                dto.ToDate,
                currentUser.UserId.Value,
                clock.UtcNow);

            allocationRepository.Add(allocation);
            await RecomputeResourceProfileStatusAsync(resourceProfile, allocation, cancellationToken);

            auditLogRepository.Add(AuditLog.Create(
                currentUser.UserId,
                "ALLOCATION_CREATED",
                nameof(Allocation),
                null,
                $"resourceProfile={dto.ResourceProfileId};project={dto.ProjectId}",
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

    public async Task EndAllocationAsync(long allocationId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Manager)
            throw new ForbiddenException("Manager role required.");

        var allocation = await allocationRepository.GetByIdAsync(allocationId, cancellationToken)
            ?? throw new NotFoundException("Allocation not found.");

        if (allocation.Project.ManagerId != currentUser.UserId.Value)
            throw new ForbiddenException("You do not own this project.");

        allocation.End(clock.Today, currentUser.UserId.Value, clock.UtcNow);

        var resourceProfile = await employeeRepository.GetByIdAsync(allocation.ResourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var today = clock.Today;
        var activeAllocations = await allocationRepository.ListActiveForEmployeeAsync(
            resourceProfile.Id,
            today,
            today,
            cancellationToken);

        var total = AllocationCapacityHelper.CalculateTotalUtilisation(activeAllocations, today, today);
        resourceProfile.RecomputeStatus(total);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AllocationListItemDto>> ListAllocationsAsync(
        long? resourceProfileId,
        long? projectId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var allocations = await allocationRepository.ListAllAsync(resourceProfileId, projectId, cancellationToken);
        return allocations.Select(AllocationMappings.ToListItemDto).ToList();
    }

    public async Task<IReadOnlyList<AllocationDto>> ListAllocationsByProjectAsync(long projectId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (currentUser.Role != UserRole.Admin && currentUser.UserId != project.ManagerId)
            throw new ForbiddenException("You do not have access to this project's allocations.");

        var allocations = await allocationRepository.ListActiveOnProjectAsync(projectId, cancellationToken);
        return allocations.Select(AllocationMappings.ToDto).ToList();
    }

    public async Task<IReadOnlyList<AllocationDto>> ListAllocationsByEmployeeAsync(long resourceProfileId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var resourceProfile = await employeeRepository.GetByIdAsync(resourceProfileId, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        if (currentUser.Role == UserRole.Admin)
        {
            // Admin can view any resourceProfile's allocations.
        }
        else if (currentUser.Role == UserRole.Manager)
        {
            CurrentUserGuards.EnsureCanViewEmployee(currentUser, resourceProfile.ManagerId, resourceProfile.Status);
        }
        else if (currentUser.Role == UserRole.Resource)
        {
            var ownProfile = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
                ?? throw new NotFoundException("Resource profile not found.");

            if (resourceProfile.Id != ownProfile.Id)
                throw new ForbiddenException("You can only view your own allocations.");
        }
        else
        {
            throw new ForbiddenException("Insufficient permissions to view resourceProfile allocations.");
        }

        var allocations = await allocationRepository.ListAllAsync(resourceProfileId, null, cancellationToken);
        return allocations.Select(AllocationMappings.ToDto).ToList();
    }

    public async Task<IReadOnlyList<AllocationDto>> ListMyAllocationsAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Resource)
            throw new ForbiddenException("Resource role required.");

        var resourceProfile = await employeeRepository.GetByUserIdAsync(currentUser.UserId.Value, cancellationToken)
            ?? throw new NotFoundException("Resource profile not found.");

        var allocations = await allocationRepository.ListAllAsync(resourceProfile.Id, null, cancellationToken);
        return allocations.Select(AllocationMappings.ToDto).ToList();
    }

    private async Task RecomputeResourceProfileStatusAsync(
        ResourceProfile resourceProfile,
        Allocation? pendingAllocation,
        CancellationToken cancellationToken)
    {
        var today = clock.Today;
        var allocations = (await allocationRepository.ListActiveForEmployeeAsync(
            resourceProfile.Id,
            today,
            today,
            cancellationToken)).ToList();

        if (pendingAllocation is not null && pendingAllocation.IsActiveOn(today))
            allocations.Add(pendingAllocation);

        var total = AllocationCapacityHelper.CalculateTotalUtilisation(allocations, today, today);
        resourceProfile.RecomputeStatus(total);
    }
}
