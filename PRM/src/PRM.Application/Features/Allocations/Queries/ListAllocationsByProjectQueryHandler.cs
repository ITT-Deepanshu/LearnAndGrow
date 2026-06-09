using MediatR;
using PRM.Application.Features.Allocations.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Allocations.Queries;

public sealed class ListAllocationsByProjectQueryHandler(
    IProjectRepository projectRepository,
    IAllocationRepository allocationRepository,
    ICurrentUser currentUser) : IRequestHandler<ListAllocationsByProjectQuery, IReadOnlyList<AllocationDto>>
{
    public async Task<IReadOnlyList<AllocationDto>> Handle(ListAllocationsByProjectQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (currentUser.Role != UserRole.Admin && currentUser.UserId != project.ManagerId)
            throw new ForbiddenException("You do not have access to this project's allocations.");

        var allocations = await allocationRepository.ListActiveOnProjectAsync(request.ProjectId, cancellationToken);
        return allocations.Select(AllocationMappings.ToDto).ToList();
    }
}
