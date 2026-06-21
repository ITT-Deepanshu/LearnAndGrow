using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Persistence;

public interface IActivityTagRepository
{
    Task<IReadOnlyList<ActivityTag>> ListAllAsync(CancellationToken cancellationToken = default);
}
