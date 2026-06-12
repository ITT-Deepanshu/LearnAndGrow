using PRM.Application.Dashboard;

namespace PRM.Application.Dashboard;

public interface IDashboardService
{
    Task<ResourceDashboardDto> GetResourceDashboardAsync(CancellationToken cancellationToken = default);
}
