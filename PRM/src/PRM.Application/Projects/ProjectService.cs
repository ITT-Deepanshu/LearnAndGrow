using PRM.Application.Common;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Helpers;
using PRM.Domain.Services;

namespace PRM.Application.Projects;

public sealed class ProjectService(
    IProjectRepository projectRepository,
    IUserRepository userRepository,
    ISystemConfigRepository systemConfigRepository,
    ProjectEffortBuilder effortBuilder,
    ProjectHealthDomainService healthService,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock) : IProjectService
{
    public async Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureAdmin(currentUser);

        if (await projectRepository.ExistsByNameAsync(dto.Name, cancellationToken))
            throw new ConflictException($"Project '{dto.Name}' already exists.");

        var manager = await userRepository.GetByIdAsync(dto.ManagerId, cancellationToken)
            ?? throw new NotFoundException("Manager not found.");

        if (manager.RoleId != (long)UserRole.Manager)
            throw new BusinessRuleException("Assigned user must have the Manager role.");

        var project = Project.Create(
            dto.Name,
            dto.Description,
            dto.StartDate,
            dto.EndDate,
            dto.Status,
            dto.ManagerId,
            dto.TotalStoryPoints,
            CurrentUserGuards.ActorId(currentUser),
            clock.UtcNow);

        projectRepository.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        project = await projectRepository.GetByIdWithDetailsAsync(project.Id, cancellationToken)
            ?? throw new NotFoundException("Project not found after creation.");

        return ProjectMappings.ToDto(project);
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        var managerFilter = CurrentUserGuards.ManagerFilter(currentUser);
        var projects = await projectRepository.ListAsync(managerFilter, cancellationToken);
        return projects.Select(ProjectMappings.ToListItemDto).ToList();
    }

    public async Task<ProjectDetailDto> GetProjectByIdAsync(long projectId, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureAuthenticated(currentUser);

        var project = await projectRepository.GetByIdWithDetailsAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        CurrentUserGuards.EnsureCanViewProject(currentUser, project.ManagerId);
        return ProjectMappings.ToDetailDto(project);
    }

    public async Task UpdateProjectAsync(long projectId, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureAdmin(currentUser);

        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        var manager = await userRepository.GetByIdAsync(dto.ManagerId, cancellationToken)
            ?? throw new NotFoundException("Manager not found.");

        if (manager.RoleId != (long)UserRole.Manager)
            throw new BusinessRuleException("Assigned user must have the Manager role.");

        project.Update(
            dto.Name,
            dto.Description,
            dto.StartDate,
            dto.EndDate,
            dto.Status,
            dto.ManagerId,
            dto.TotalStoryPoints,
            currentUser.UserId!.Value,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MilestoneDto> AddMilestoneAsync(long projectId, AddMilestoneDto dto, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureAdmin(currentUser);

        var project = await projectRepository.GetByIdWithDetailsAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        var milestone = project.AddMilestone(
            dto.Title,
            dto.DueDate,
            dto.StoryPoints,
            currentUser.UserId!.Value,
            clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProjectMappings.ToMilestoneDto(milestone);
    }

    public async Task<IReadOnlyList<MilestoneDto>> ListMilestonesAsync(long projectId, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureAuthenticated(currentUser);

        var project = await projectRepository.GetByIdWithDetailsAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        if (currentUser.Role != UserRole.Admin && currentUser.UserId != project.ManagerId)
            throw new ForbiddenException("You do not have access to this project's milestones.");

        return project.Milestones.Select(ProjectMappings.ToMilestoneDto).ToList();
    }

    public async Task<ProjectHealthDto> GetProjectHealthAsync(long projectId, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureAuthenticated(currentUser);

        var project = await projectRepository.GetByIdWithDetailsAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        CurrentUserGuards.EnsureCanViewProject(currentUser, project.ManagerId);

        var config = await systemConfigRepository.GetAsync(cancellationToken);
        var today = clock.Today;
        var lastWeekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        var activeAllocations = project.Allocations.Where(a => a.IsActiveOn(today)).ToList();
        var effortData = await effortBuilder.BuildAsync(
            activeAllocations,
            project.Id,
            lastWeekStart,
            config.MaxWeeklyHours,
            cancellationToken);

        var result = healthService.Evaluate(project, today, effortData);

        return new ProjectHealthDto(
            project.Id,
            result.Status.ToString(),
            result.DisplayLabel,
            result.RiskFlags);
    }

    public async Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, MilestoneStatus status, CancellationToken cancellationToken = default)
    {
        CurrentUserGuards.EnsureAdmin(currentUser);

        var project = await projectRepository.GetByIdWithDetailsAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project not found.");

        project.UpdateMilestoneStatus(milestoneId, status, currentUser.UserId!.Value, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
