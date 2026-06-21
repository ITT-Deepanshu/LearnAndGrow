using Microsoft.EntityFrameworkCore;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;

namespace PRM.Persistence.Repositories;

public class SystemConfigRepository(PrmDbContext context) : ISystemConfigRepository
{
    public async Task<SystemConfiguration> GetAsync(CancellationToken cancellationToken = default) =>
        await context.SystemConfigurations.FirstAsync(cancellationToken);
}
