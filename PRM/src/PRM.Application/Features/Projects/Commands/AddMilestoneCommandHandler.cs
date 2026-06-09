using MediatR;
using PRM.Application.Features.Projects.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Projects.Commands;

public sealed class AddMilestoneCommandHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<AddMilestoneCommand, MilestoneDto>
{
    public async Task<MilestoneDto> Handle(AddMilestoneCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var project = await projectRepository.GetByIdWithDetailsAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        var milestone = project.AddMilestone(
            request.Title,
            request.DueDate,
            request.StoryPoints,
            currentUser.UserId.Value,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ProjectMappings.ToMilestoneDto(milestone);
    }
}
