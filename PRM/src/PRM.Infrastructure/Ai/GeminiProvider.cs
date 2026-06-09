using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PRM.Application.Interfaces.Ai;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Ai;

public sealed class GeminiProvider(
    HttpClient httpClient,
    AiApiKeyResolver apiKeyResolver,
    IOptions<AiSettings> options) : IAiProvider
{
    private readonly ProviderSettings _settings = options.Value.Gemini;

    public string ProviderName => nameof(AiProviderType.Gemini);

    public async Task<SkillMatchAiResponse> MatchSkillsAsync(
        SkillMatchAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildSkillMatchPrompt(request);
        var content = await GenerateContentAsync(prompt, cancellationToken);
        var validIds = request.Candidates.Select(c => c.EmployeeId).ToHashSet();
        return AiResponseParser.ParseSkillMatch(content, validIds);
    }

    public async Task<RiskSummaryAiResponse> SummarizeRiskAsync(
        RiskSummaryAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildRiskSummaryPrompt(request);
        var content = await GenerateContentAsync(prompt, cancellationToken);
        return AiResponseParser.ParseRiskSummary(content);
    }

    private async Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken)
    {
        var apiKey = await apiKeyResolver.ResolveAsync(_settings.ApiKey, cancellationToken);
        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-1.5-flash" : _settings.Model;
        var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
            ? "https://generativelanguage.googleapis.com"
            : _settings.BaseUrl.TrimEnd('/');

        var url = $"{baseUrl}/v1beta/models/{model}:generateContent?key={apiKey}";
        var body = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                responseMimeType = "application/json"
            }
        };

        using var response = await httpClient.PostAsJsonAsync(url, body, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new AiException($"Gemini API returned {(int)response.StatusCode}: {payload}");

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new AiException("Gemini API returned no candidates.");

        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.GetArrayLength() == 0)
        {
            throw new AiException("Gemini API returned an empty response.");
        }

        var text = parts[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new AiException("Gemini API returned empty text.");

        return text;
    }
}
