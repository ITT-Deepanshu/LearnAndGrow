using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Projects.Commands;

public sealed class UpdateMilestoneStatusCommandHandler(
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateMilestoneStatusCommand>
{
    public async Task Handle(UpdateMilestoneStatusCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var project = await projectRepository.GetByIdWithDetailsAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        project.UpdateMilestoneStatus(
            request.MilestoneId,
            request.Status,
            currentUser.UserId.Value,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
