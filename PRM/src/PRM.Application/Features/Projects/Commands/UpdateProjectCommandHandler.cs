using MediatR;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Application.Features.Projects.Commands;

public sealed class UpdateProjectCommandHandler(
    IProjectRepository projectRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateProjectCommand>
{
    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        var manager = await userRepository.GetByIdAsync(request.ManagerId, cancellationToken)
            ?? throw new NotFoundException("Manager not found.");

        if (manager.Role != UserRole.Manager)
            throw new BusinessRuleException("Assigned user must have the Manager role.");

        project.Update(
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Status,
            request.ManagerId,
            request.TotalStoryPoints,
            currentUser.UserId.Value,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
