using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PRM.ConsoleClient.Models;

namespace PRM.ConsoleClient.Services;

public sealed class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly SessionContext _session;
    private readonly TokenStore _tokenStore;

    public ApiClient(SessionContext session, TokenStore tokenStore)
    {
        _session = session;
        _tokenStore = tokenStore;

        var baseUrl = Environment.GetEnvironmentVariable(Constants.ApiUrlEnvVar) ?? Constants.DefaultApiUrl;
        var handler = new HttpClientHandler();
        if (baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

        _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string BaseAddress => _http.BaseAddress?.ToString() ?? Constants.DefaultApiUrl;

    // ── Auth ──────────────────────────────────────────────────────────────

    public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var result = await PostAsync<LoginRequest, LoginResponse>("api/v1/auth/login", new(username, password), authenticated: false, ct);
        ApplyLogin(result);
        return result;
    }

    public async Task<MeResponse> GetMeAsync(CancellationToken ct = default) =>
        await GetAsync<MeResponse>("api/v1/auth/me", ct);

    public async Task ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword, CancellationToken ct = default)
    {
        await PostAsync("api/v1/auth/change-password", new ChangePasswordRequest(currentPassword, newPassword, confirmPassword), ct);
        _session.ForcePasswordChange = false;
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(_session.RefreshToken))
        {
            try
            {
                await PostAsync("api/v1/auth/logout", new RefreshRequest(_session.RefreshToken), ct);
            }
            catch
            {
                // Best-effort logout.
            }
        }

        _session.Clear();
        _tokenStore.Clear();
    }

    // ── Users ─────────────────────────────────────────────────────────────

    public Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default) =>
        PostAsync<CreateUserRequest, UserResponse>("api/v1/users", request, ct);

    public Task<IReadOnlyList<UserListItem>> ListUsersAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<UserListItem>>("api/v1/users", ct);

    public Task<UserResponse> GetUserAsync(long id, CancellationToken ct = default) =>
        GetAsync<UserResponse>($"api/v1/users/{id}", ct);

    public Task ResetUserPasswordAsync(long id, string newPassword, string confirmPassword, CancellationToken ct = default) =>
        PostAsync($"api/v1/users/{id}/reset-password", new ResetPasswordRequest(newPassword, confirmPassword), ct);

    public Task DeactivateUserAsync(long id, CancellationToken ct = default) =>
        PostAsync($"api/v1/users/{id}/deactivate", new { }, ct);

    public Task ReactivateUserAsync(long id, CancellationToken ct = default) =>
        PostAsync($"api/v1/users/{id}/reactivate", new { }, ct);

    // ── Employees ───────────────────────────────────────────────────────

    public Task<IReadOnlyList<EmployeeListItem>> ListEmployeesAsync(int? status = null, string? department = null, CancellationToken ct = default)
    {
        var query = BuildQuery(("status", status?.ToString()), ("department", department));
        return GetAsync<IReadOnlyList<EmployeeListItem>>($"api/v1/employees{query}", ct);
    }

    public Task<EmployeeDetail> GetEmployeeAsync(long id, CancellationToken ct = default) =>
        GetAsync<EmployeeDetail>($"api/v1/employees/{id}", ct);

    public Task UpdateEmployeeAsync(long id, string department, string designation, CancellationToken ct = default) =>
        PutAsync($"api/v1/employees/{id}", new UpdateEmployeeRequest(department, designation), ct);

    public Task DeactivateEmployeeAsync(long id, CancellationToken ct = default) =>
        PostAsync($"api/v1/employees/{id}/deactivate", new { }, ct);

    public Task AssignManagerAsync(long employeeId, long managerUserId, CancellationToken ct = default) =>
        PostAsync($"api/v1/employees/{employeeId}/assign-manager", new AssignManagerRequest(managerUserId), ct);

    public Task<EmployeeSkill> AddSkillAsync(long employeeId, string name, int category, int proficiency, CancellationToken ct = default) =>
        PostAsync<AddSkillRequest, EmployeeSkill>($"api/v1/employees/{employeeId}/skills", new(name, category, proficiency), ct);

    public Task UpdateSkillProficiencyAsync(long employeeId, long skillId, int proficiency, CancellationToken ct = default) =>
        PutAsync($"api/v1/employees/{employeeId}/skills/{skillId}", new UpdateSkillProficiencyRequest(proficiency), ct);

    public Task RemoveSkillAsync(long employeeId, long skillId, CancellationToken ct = default) =>
        DeleteAsync($"api/v1/employees/{employeeId}/skills/{skillId}", ct);

    public async Task<long> ResolveEmployeeIdAsync(CancellationToken ct = default)
    {
        if (_session.EmployeeId.HasValue)
            return _session.EmployeeId.Value;

        var stored = _tokenStore.Load();
        if (stored.EmployeeId.HasValue)
        {
            _session.EmployeeId = stored.EmployeeId;
            return stored.EmployeeId.Value;
        }

        for (long id = 1; id <= 1000; id++)
        {
            try
            {
                await GetAsync<IReadOnlyList<Allocation>>($"api/v1/allocations/by-employee/{id}", ct);
                _session.EmployeeId = id;
                PersistTokens();
                return id;
            }
            catch (ApiException ex) when (ex.StatusCode is 403 or 404)
            {
                // Keep probing until the current user's employee record is found.
            }
        }

        throw new ApiException(404, "Could not resolve employee profile for the current user.");
    }

    // ── Projects ─────────────────────────────────────────────────────────

    public Task<ProjectListItem> CreateProjectAsync(CreateProjectRequest request, CancellationToken ct = default) =>
        PostAsync<CreateProjectRequest, ProjectListItem>("api/v1/projects", request, ct);

    public Task<IReadOnlyList<ProjectListItem>> ListProjectsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProjectListItem>>("api/v1/projects", ct);

    public Task<ProjectDetail> GetProjectAsync(long id, CancellationToken ct = default) =>
        GetAsync<ProjectDetail>($"api/v1/projects/{id}", ct);

    public Task UpdateProjectAsync(long id, UpdateProjectRequest request, CancellationToken ct = default) =>
        PutAsync($"api/v1/projects/{id}", request, ct);

    public Task<Milestone> AddMilestoneAsync(long projectId, AddMilestoneRequest request, CancellationToken ct = default) =>
        PostAsync<AddMilestoneRequest, Milestone>($"api/v1/projects/{projectId}/milestones", request, ct);

    public Task<IReadOnlyList<Milestone>> ListMilestonesAsync(long projectId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Milestone>>($"api/v1/projects/{projectId}/milestones", ct);

    public Task UpdateMilestoneStatusAsync(long projectId, long milestoneId, int status, CancellationToken ct = default) =>
        PutAsync($"api/v1/projects/{projectId}/milestones/{milestoneId}/status", new UpdateMilestoneStatusRequest(status), ct);

    public Task<ProjectHealth> GetProjectHealthAsync(long projectId, CancellationToken ct = default) =>
        GetAsync<ProjectHealth>($"api/v1/projects/{projectId}/health", ct);

    // ── Allocations ──────────────────────────────────────────────────────

    public Task<Allocation> CreateAllocationAsync(CreateAllocationRequest request, CancellationToken ct = default) =>
        PostAsync<CreateAllocationRequest, Allocation>("api/v1/allocations", request, ct);

    public Task EndAllocationAsync(long id, CancellationToken ct = default) =>
        PostAsync($"api/v1/allocations/{id}/end", new { }, ct);

    public Task<IReadOnlyList<AllocationListItem>> ListAllocationsAsync(long? employeeId = null, long? projectId = null, CancellationToken ct = default)
    {
        var query = BuildQuery(("employeeId", employeeId?.ToString()), ("projectId", projectId?.ToString()));
        return GetAsync<IReadOnlyList<AllocationListItem>>($"api/v1/allocations{query}", ct);
    }

    public Task<IReadOnlyList<Allocation>> ListAllocationsByProjectAsync(long projectId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Allocation>>($"api/v1/allocations/by-project/{projectId}", ct);

    public Task<IReadOnlyList<Allocation>> ListAllocationsByEmployeeAsync(long employeeId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Allocation>>($"api/v1/allocations/by-employee/{employeeId}", ct);

    // ── Timesheets ───────────────────────────────────────────────────────

    public Task<Timesheet> SubmitTimesheetAsync(SubmitTimesheetRequest request, CancellationToken ct = default) =>
        PostAsync<SubmitTimesheetRequest, Timesheet>("api/v1/timesheets", request, ct);

    public Task<IReadOnlyList<TimesheetListItem>> ListMyTimesheetsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<TimesheetListItem>>("api/v1/timesheets/my", ct);

    public Task<Timesheet?> GetMyTimesheetForWeekAsync(DateOnly weekStart, CancellationToken ct = default)
    {
        var key = weekStart.ToString("yyyy-MM-dd");
        return GetOptionalAsync<Timesheet>($"api/v1/timesheets/my/{key}", ct);
    }

    public Task<IReadOnlyList<TeamTimesheetRow>> ListTeamTimesheetsAsync(DateOnly weekStart, CancellationToken ct = default)
    {
        var query = BuildQuery(("weekStart", weekStart.ToString("yyyy-MM-dd")));
        return GetAsync<IReadOnlyList<TeamTimesheetRow>>($"api/v1/timesheets/team{query}", ct);
    }

    // ── Dashboard ────────────────────────────────────────────────────────

    public Task<ResourceDashboard> GetResourceDashboardAsync(CancellationToken ct = default) =>
        GetAsync<ResourceDashboard>("api/v1/dashboard/resources", ct);

    // ── AI ───────────────────────────────────────────────────────────────

    public Task<SkillMatchResult> SkillMatchAsync(long projectId, string requirement, CancellationToken ct = default)
    {
        var query = BuildQuery(("projectId", projectId.ToString()), ("requirement", requirement));
        return GetAsync<SkillMatchResult>($"api/v1/ai/skill-match{query}", ct);
    }

    public Task<RiskSummary> RiskSummaryAsync(long projectId, CancellationToken ct = default) =>
        GetAsync<RiskSummary>($"api/v1/ai/risk-summary/{projectId}", ct);

    // ── System Config ────────────────────────────────────────────────────

    public Task<SystemConfig> GetSystemConfigAsync(CancellationToken ct = default) =>
        GetAsync<SystemConfig>("api/v1/system-config", ct);

    public Task<SystemConfig> UpdateSystemConfigAsync(UpdateSystemConfigRequest request, CancellationToken ct = default) =>
        PutAsync<UpdateSystemConfigRequest, SystemConfig>("api/v1/system-config", request, ct);

    // ── HTTP helpers ─────────────────────────────────────────────────────

    private async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    private async Task<TResponse?> GetOptionalAsync<TResponse>(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;
        return await ReadAsync<TResponse>(response, ct);
    }

    private Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct) =>
        PostAsync<TRequest, TResponse>(path, body, authenticated: true, ct);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, bool authenticated, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Post, path, body, ct, authenticated);
        return await ReadAsync<TResponse>(response, ct);
    }

    private Task PostAsync<TRequest>(string path, TRequest body, CancellationToken ct) =>
        PostAsync(path, body, authenticated: true, ct);

    private async Task PostAsync<TRequest>(string path, TRequest body, bool authenticated, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Post, path, body, ct, authenticated);
        await EnsureSuccessAsync(response, ct);
    }

    private Task PostAsync(string path, object body, CancellationToken ct) =>
        PostAsync(path, body, authenticated: true, ct);

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Put, path, body, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    private Task PutAsync<TRequest>(string path, TRequest body, CancellationToken ct) =>
        PutAsync(path, body, authenticated: true, ct);

    private async Task PutAsync<TRequest>(string path, TRequest body, bool authenticated, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Put, path, body, ct, authenticated);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task DeleteAsync(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Delete, path, null, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body, CancellationToken ct, bool authenticated = true)
    {
        if (authenticated)
            await EnsureAuthenticatedAsync(ct);

        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        if (authenticated && !string.IsNullOrEmpty(_session.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        return await _http.SendAsync(request, ct);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_session.AccessToken))
            return;

        var stored = _tokenStore.Load();
        if (string.IsNullOrEmpty(stored.RefreshToken))
            throw new ApiException(401, "Not authenticated. Please log in.");

        var refreshed = await PostAsync<RefreshRequest, LoginResponse>(
            "api/v1/auth/refresh", new RefreshRequest(stored.RefreshToken), authenticated: false, ct);
        ApplyLogin(refreshed);
    }

    private void ApplyLogin(LoginResponse result)
    {
        _session.SetLogin(new LoginData(
            result.AccessToken,
            result.RefreshToken,
            result.ForcePasswordChange,
            result.Role,
            result.FullName,
            null));
        PersistTokens();
    }

    private void PersistTokens() =>
        _tokenStore.Save(_session.AccessToken, _session.RefreshToken, _session.EmployeeId);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return result ?? throw new ApiException((int)response.StatusCode, "Empty response from server.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await ReadErrorAsync(response, ct);
        throw new ApiException((int)response.StatusCode, message);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions, ct);
            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Title))
                return problem.Title;
        }
        catch
        {
            // Fall through to raw body.
        }

        var raw = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(raw) ? $"Request failed ({(int)response.StatusCode})." : raw;
    }

    private static string BuildQuery(params (string Key, string? Value)[] pairs)
    {
        var parts = pairs
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
