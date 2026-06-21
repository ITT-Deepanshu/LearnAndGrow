using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Api;

/// <summary>
/// Low-level HTTP transport: auth header, JSON, error handling.
/// Feature APIs call this — views never use it directly.
/// </summary>
public sealed class PrmHttpClient
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly SessionContext _session;
    private readonly TokenStore _tokenStore;

    public PrmHttpClient(SessionContext session, TokenStore tokenStore)
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

    public SessionContext Session => _session;

    public TokenStore TokenStore => _tokenStore;

    public void PersistTokens() =>
        _tokenStore.Save(_session.AccessToken, _session.ResourceProfileId);

    public Task<T> GetAsync<T>(string path, CancellationToken ct) =>
        SendAndReadAsync<T>(HttpMethod.Get, path, null, authenticated: true, ct);

    public async Task<T?> GetOptionalAsync<T>(string path, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Get, path, null, authenticated: true, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return default;
        return await ReadAsync<T>(response, ct);
    }

    public Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct, bool authenticated = true) =>
        SendAndReadAsync<TResponse>(HttpMethod.Post, path, body, authenticated, ct);

    public Task PostAsync<TRequest>(string path, TRequest body, CancellationToken ct, bool authenticated = true) =>
        SendAndEnsureAsync(HttpMethod.Post, path, body, authenticated, ct);

    public Task PostAsync(string path, object body, CancellationToken ct, bool authenticated = true) =>
        SendAndEnsureAsync(HttpMethod.Post, path, body, authenticated, ct);

    public Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct) =>
        SendAndReadAsync<TResponse>(HttpMethod.Put, path, body, authenticated: true, ct);

    public Task PutAsync<TRequest>(string path, TRequest body, CancellationToken ct) =>
        SendAndEnsureAsync(HttpMethod.Put, path, body, authenticated: true, ct);

    public Task DeleteAsync(string path, CancellationToken ct) =>
        SendAndEnsureAsync(HttpMethod.Delete, path, null, authenticated: true, ct);

    public static string BuildQuery(params (string Key, string? Value)[] pairs)
    {
        var parts = pairs
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private async Task<T> SendAndReadAsync<T>(HttpMethod method, string path, object? body, bool authenticated, CancellationToken ct)
    {
        using var response = await SendAsync(method, path, body, authenticated, ct);
        return await ReadAsync<T>(response, ct);
    }

    private async Task SendAndEnsureAsync(HttpMethod method, string path, object? body, bool authenticated, CancellationToken ct)
    {
        using var response = await SendAsync(method, path, body, authenticated, ct);
        await EnsureSuccessAsync(response, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, bool authenticated, CancellationToken ct)
    {
        if (authenticated)
            EnsureAuthenticated();

        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        if (authenticated && !string.IsNullOrEmpty(_session.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);

        return await _http.SendAsync(request, ct);
    }

    private void EnsureAuthenticated()
    {
        if (!string.IsNullOrEmpty(_session.AccessToken))
            return;

        var stored = _tokenStore.Load();
        if (string.IsNullOrEmpty(stored.AccessToken))
            throw new ApiException(401, "Not authenticated. Please log in.");

        _session.AccessToken = stored.AccessToken;
        _session.ResourceProfileId = stored.ResourceProfileId;
    }

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
}
