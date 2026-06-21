using PRM.Domain.Entities;

namespace PRM.Application.Interfaces.Persistence;

public interface ISystemConfigRepository
{
    Task<SystemConfiguration> GetAsync(CancellationToken cancellationToken = default);
}
