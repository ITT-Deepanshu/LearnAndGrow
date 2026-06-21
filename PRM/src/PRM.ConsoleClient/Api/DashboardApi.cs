using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Api;

public sealed class DashboardApi(PrmHttpClient http)
{
    public Task<ResourceDashboard> GetResourcesAsync(CancellationToken ct = default) =>
        http.GetAsync<ResourceDashboard>("api/v1/dashboard/resources", ct);
}
