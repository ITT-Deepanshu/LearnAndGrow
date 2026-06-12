using PRM.Domain.Entities;
using PRM.Domain.Enums;

namespace PRM.Application.Interfaces.Persistence;

public interface IResourceProfileRepository
{
    Task<ResourceProfile?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ResourceProfile?> GetByIdWithSkillsAsync(long id, CancellationToken cancellationToken = default);
    Task<ResourceProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceProfile>> ListAsync(ResourceProfileStatus? status, string? department, long? managerId, CancellationToken cancellationToken = default);
    void Add(ResourceProfile resourceProfile);
}
