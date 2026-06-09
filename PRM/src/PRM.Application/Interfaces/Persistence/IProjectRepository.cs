using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Persistence;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> ListAsync(long? managerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> ListActiveWithDetailsAsync(CancellationToken cancellationToken = default);
    void Add(Project project);
}
