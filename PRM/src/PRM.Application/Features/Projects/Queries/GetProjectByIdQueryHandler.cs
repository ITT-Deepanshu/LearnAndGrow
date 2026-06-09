using MediatR;
using PRM.Application.Features.Projects.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Projects.Queries;

public sealed class GetProjectByIdQueryHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser) : IRequestHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        var project = await projectRepository.GetByIdWithDetailsAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        EnsureCanViewProject(project.ManagerId);

        return ProjectMappings.ToDetailDto(project);
    }

    private void EnsureCanViewProject(long managerId)
    {
        if (currentUser.Role == UserRole.Admin)
            return;

        if (currentUser.Role == UserRole.Manager && currentUser.UserId == managerId)
            return;

        throw new ForbiddenException("You do not have access to this project.");
    }
}
