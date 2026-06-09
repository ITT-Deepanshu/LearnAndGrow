using MediatR;
using PRM.Application.Features.Projects.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Projects.Queries;

public sealed class ListProjectsQueryHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser) : IRequestHandler<ListProjectsQuery, IReadOnlyList<ProjectListItemDto>>
{
    public async Task<IReadOnlyList<ProjectListItemDto>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");

        long? managerFilter = currentUser.Role switch
        {
            UserRole.Admin => null,
            UserRole.Manager => currentUser.UserId,
            _ => throw new ForbiddenException("Insufficient permissions to list projects.")
        };

        var projects = await projectRepository.ListAsync(managerFilter, cancellationToken);
        return projects.Select(ProjectMappings.ToListItemDto).ToList();
    }
}
