using MediatR;
using PRM.Application.Features.Projects.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Projects.Queries;

public sealed class ListMilestonesQueryHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser) : IRequestHandler<ListMilestonesQuery, IReadOnlyList<MilestoneDto>>
{
    public async Task<IReadOnlyList<MilestoneDto>> Handle(ListMilestonesQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var project = await projectRepository.GetByIdWithDetailsAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (currentUser.Role != UserRole.Admin && currentUser.UserId != project.ManagerId)
            throw new ForbiddenException("You do not have access to this project's milestones.");

        return project.Milestones.Select(ProjectMappings.ToMilestoneDto).ToList();
    }
}
