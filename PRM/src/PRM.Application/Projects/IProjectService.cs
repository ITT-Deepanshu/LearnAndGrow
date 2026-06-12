using PRM.Application.Projects;
using PRM.Domain.Enums;

namespace PRM.Application.Projects;

public interface IProjectService
{
    Task<ProjectDto> CreateProjectAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectListItemDto>> ListProjectsAsync(CancellationToken cancellationToken = default);
    Task<ProjectDetailDto> GetProjectByIdAsync(long projectId, CancellationToken cancellationToken = default);
    Task UpdateProjectAsync(long projectId, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    Task<MilestoneDto> AddMilestoneAsync(long projectId, AddMilestoneDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MilestoneDto>> ListMilestonesAsync(long projectId, CancellationToken cancellationToken = default);
    Task<ProjectHealthDto> GetProjectHealthAsync(long projectId, CancellationToken cancellationToken = default);
    Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, MilestoneStatus status, CancellationToken cancellationToken = default);
}
