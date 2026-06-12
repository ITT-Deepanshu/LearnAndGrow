using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Api;

public sealed class SystemConfigApi(PrmHttpClient http)
{
    public Task<SystemConfig> GetAsync(CancellationToken ct = default) =>
        http.GetAsync<SystemConfig>("api/v1/system-config", ct);

    public Task<SystemConfig> UpdateAsync(UpdateSystemConfigRequest request, CancellationToken ct = default) =>
        http.PutAsync<UpdateSystemConfigRequest, SystemConfig>("api/v1/system-config", request, ct);
}
