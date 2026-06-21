using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Api;

public sealed class ProjectsApi(PrmHttpClient http)
{
    public Task<ProjectListItem> CreateAsync(CreateProjectRequest request, CancellationToken ct = default) =>
        http.PostAsync<CreateProjectRequest, ProjectListItem>("api/v1/projects", request, ct);

    public Task<IReadOnlyList<ProjectListItem>> ListAsync(CancellationToken ct = default) =>
        http.GetAsync<IReadOnlyList<ProjectListItem>>("api/v1/projects", ct);

    public Task<ProjectDetail> GetAsync(long id, CancellationToken ct = default) =>
        http.GetAsync<ProjectDetail>($"api/v1/projects/{id}", ct);

    public Task UpdateAsync(long id, UpdateProjectRequest request, CancellationToken ct = default) =>
        http.PutAsync($"api/v1/projects/{id}", request, ct);

    public Task<Milestone> AddMilestoneAsync(long projectId, AddMilestoneRequest request, CancellationToken ct = default) =>
        http.PostAsync<AddMilestoneRequest, Milestone>($"api/v1/projects/{projectId}/milestones", request, ct);

    public Task<IReadOnlyList<Milestone>> ListMilestonesAsync(long projectId, CancellationToken ct = default) =>
        http.GetAsync<IReadOnlyList<Milestone>>($"api/v1/projects/{projectId}/milestones", ct);

    public Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, int status, CancellationToken ct = default) =>
        http.PutAsync($"api/v1/projects/{projectId}/milestones/{milestoneId}/status", new UpdateMilestoneStatusRequest(status), ct);

    public Task<ProjectHealth> GetHealthAsync(long projectId, CancellationToken ct = default) =>
        http.GetAsync<ProjectHealth>($"api/v1/projects/{projectId}/health", ct);
}
