using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PRM.Application.Interfaces.Ai;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Ai;

public sealed class GrokProvider(
    HttpClient httpClient,
    AiApiKeyResolver apiKeyResolver,
    IOptions<AiSettings> options) : IAiProvider
{
    private readonly ProviderSettings _settings = options.Value.Grok;

    public string ProviderName => nameof(AiProviderType.Grok);

    public async Task<SkillMatchAiResponse> MatchSkillsAsync(
        SkillMatchAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildSkillMatchPrompt(request);
        var content = await ChatCompletionAsync(prompt, cancellationToken);
        var validIds = request.Candidates.Select(c => c.EmployeeId).ToHashSet();
        return AiResponseParser.ParseSkillMatch(content, validIds);
    }

    public async Task<RiskSummaryAiResponse> SummarizeRiskAsync(
        RiskSummaryAiRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = AiPromptBuilder.BuildRiskSummaryPrompt(request);
        var content = await ChatCompletionAsync(prompt, cancellationToken);
        return AiResponseParser.ParseRiskSummary(content);
    }

    private async Task<string> ChatCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        var apiKey = await apiKeyResolver.ResolveAsync(_settings.ApiKey, cancellationToken);
        var model = string.IsNullOrWhiteSpace(_settings.Model) ? "grok-4.3" : _settings.Model;
        var baseUrl = string.IsNullOrWhiteSpace(_settings.BaseUrl)
            ? "https://api.x.ai/v1"
            : _settings.BaseUrl.TrimEnd('/');

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        requestMessage.Content = JsonContent.Create(new
        {
            model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        });

        using var response = await httpClient.SendAsync(requestMessage, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new AiException($"Grok API returned {(int)response.StatusCode}: {payload}");

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            throw new AiException("Grok API returned no choices.");

        var message = choices[0].GetProperty("message");
        var text = message.GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(text))
            throw new AiException("Grok API returned empty text.");

        return text;
    }
}
