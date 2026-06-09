using MediatR;
using PRM.Application.Features.Projects.Dtos;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Factories;

namespace PRM.Application.Features.Projects.Commands;

public sealed class CreateProjectCommandHandler(
    IProjectRepository projectRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        EnsureAdmin();

        if (await projectRepository.ExistsByNameAsync(request.Name, cancellationToken))
            throw new ConflictException($"Project '{request.Name}' already exists.");

        var manager = await userRepository.GetByIdAsync(request.ManagerId, cancellationToken)
            ?? throw new NotFoundException("Manager not found.");

        if (manager.Role != UserRole.Manager)
            throw new BusinessRuleException("Assigned user must have the Manager role.");

        var actorId = currentUser.UserId!.Value;
        var project = ProjectFactory.Create(
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Status,
            request.ManagerId,
            request.TotalStoryPoints,
            actorId,
            clock.UtcNow);

        projectRepository.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        project = await projectRepository.GetByIdWithDetailsAsync(project.Id, cancellationToken)
            ?? throw new NotFoundException("Project not found after creation.");

        return ProjectMappings.ToDto(project);
    }

    private void EnsureAdmin()
    {
        if (currentUser.UserId is null)
            throw new UnauthorizedException("Not authenticated.");
        if (currentUser.Role != UserRole.Admin)
            throw new ForbiddenException("Admin role required.");
    }
}
