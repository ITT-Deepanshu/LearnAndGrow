using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PRM.Application.Interfaces.Ai;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Ai;

/// <summary>
/// In-house Gemma via POST /api/generate with model, prompt, and apikey header.
/// Base URL and model come from appsettings; API key from system configuration.
/// </summary>
public sealed class GemmaProvider(
    HttpClient httpClient,
    AiApiKeyResolver apiKeyResolver,
    IOptions<AiSettings> options) : IAiProvider
{
    private readonly GemmaSettings _settings = options.Value.Gemma;

    public async Task<SkillMatchAiResponse> MatchSkillsAsync(
        SkillMatchAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildSkillMatchPrompt(request);
        var content = await GenerateAsync(prompt, cancellationToken);
        var validIds = request.Candidates.Select(c => c.ResourceProfileId).ToHashSet();
        return AiResponseParser.ParseSkillMatch(content, validIds);
    }

    public async Task<TeamSkillMatchAiResponse> MatchTeamAsync(
        TeamSkillMatchAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildTeamSkillMatchPrompt(request);
        var content = await GenerateAsync(prompt, cancellationToken);
        var validIds = request.BenchEmployees.Select(c => c.ResourceProfileId).ToHashSet();
        return AiResponseParser.ParseTeamSkillMatch(content, validIds);
    }

    public async Task<RiskSummaryAiResponse> SummarizeRiskAsync(
        RiskSummaryAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildRiskSummaryPrompt(request);
        var content = await GenerateAsync(prompt, cancellationToken);
        return AiResponseParser.ParseRiskSummary(content);
    }

    private async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        ValidateSettings();

        var apiKey = _settings.RequireApiKey
            ? await apiKeyResolver.ResolveAsync(providerApiKey: null, cancellationToken)
            : await apiKeyResolver.ResolveOptionalAsync(providerApiKey: null, cancellationToken);

        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemma" : _settings.Model.Trim();
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var apiPath = string.IsNullOrWhiteSpace(_settings.ApiPath) ? "/api/generate" : _settings.ApiPath.Trim();
        if (!apiPath.StartsWith('/'))
            apiPath = "/" + apiPath;

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{apiPath}");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            var headerName = string.IsNullOrWhiteSpace(_settings.ApiKeyHeader) ? "apikey" : _settings.ApiKeyHeader;
            requestMessage.Headers.TryAddWithoutValidation(headerName, apiKey);
        }

        requestMessage.Content = JsonContent.Create(new
        {
            model,
            prompt,
            stream = false
        });

        try
        {
            using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new AiException($"Gemma API returned {(int)response.StatusCode}: {payload}");

            return ExtractText(payload);
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            throw new AiException(
                $"Cannot reach Gemma server at '{baseUrl}'. Check Ai:Gemma:BaseUrl in appsettings.json " +
                $"and ensure the API is running. ({ex.Message})");
        }
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            throw new AiException("Gemma BaseUrl is not configured in appsettings (Ai:Gemma:BaseUrl).");

        if (_settings.BaseUrl.Contains("your-gemma-server", StringComparison.OrdinalIgnoreCase))
            throw new AiException(
                "Gemma BaseUrl is still the placeholder 'your-gemma-server'. " +
                "Set Ai:Gemma:BaseUrl in appsettings.json to your real server URL and restart PRM.Api.");
    }

    private static string ExtractText(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        if (root.TryGetProperty("response", out var response))
        {
            var text = response.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        if (root.TryGetProperty("text", out var textProp))
        {
            var text = textProp.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        if (root.TryGetProperty("content", out var content))
        {
            var text = content.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        throw new AiException("Gemma API returned an empty or unrecognized response.");
    }
}
