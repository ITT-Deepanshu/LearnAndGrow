using MediatR;
using PRM.Application.Features.Allocations.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Allocations.Queries;

public sealed class ListAllocationsQueryHandler(
    IAllocationRepository allocationRepository,
    ICurrentUser currentUser) : IRequestHandler<ListAllocationsQuery, IReadOnlyList<AllocationListItemDto>>
{
    public async Task<IReadOnlyList<AllocationListItemDto>> Handle(ListAllocationsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var allocations = await allocationRepository.ListAllAsync(request.EmployeeId, request.ProjectId, cancellationToken);
        return allocations.Select(AllocationMappings.ToListItemDto).ToList();
    }
}
