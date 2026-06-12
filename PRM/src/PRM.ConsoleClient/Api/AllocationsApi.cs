using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Api;

public sealed class AllocationsApi(PrmHttpClient http)
{
    public Task<Allocation> CreateAsync(CreateAllocationRequest request, CancellationToken ct = default) =>
        http.PostAsync<CreateAllocationRequest, Allocation>("api/v1/allocations", request, ct);

    public Task EndAsync(long id, CancellationToken ct = default) =>
        http.PostAsync($"api/v1/allocations/{id}/end", new { }, ct);

    public Task<IReadOnlyList<AllocationListItem>> ListAsync(long? employeeId = null, long? projectId = null, CancellationToken ct = default)
    {
        var query = PrmHttpClient.BuildQuery(("employeeId", employeeId?.ToString()), ("projectId", projectId?.ToString()));
        return http.GetAsync<IReadOnlyList<AllocationListItem>>($"api/v1/allocations{query}", ct);
    }

    public Task<IReadOnlyList<Allocation>> ListByProjectAsync(long projectId, CancellationToken ct = default) =>
        http.GetAsync<IReadOnlyList<Allocation>>($"api/v1/allocations/by-project/{projectId}", ct);

    public Task<IReadOnlyList<Allocation>> ListByEmployeeAsync(long employeeId, CancellationToken ct = default) =>
        http.GetAsync<IReadOnlyList<Allocation>>($"api/v1/allocations/by-employee/{employeeId}", ct);

    public Task<IReadOnlyList<Allocation>> ListMyAsync(CancellationToken ct = default) =>
        http.GetAsync<IReadOnlyList<Allocation>>("api/v1/allocations/my", ct);
}
