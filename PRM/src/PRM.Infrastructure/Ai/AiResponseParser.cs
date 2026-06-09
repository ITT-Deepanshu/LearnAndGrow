using System.Text.Json;
using PRM.Application.Interfaces.Ai;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Ai;

internal static class AiResponseParser
{
    public static SkillMatchAiResponse ParseSkillMatch(string content, IReadOnlySet<long> validEmployeeIds)
    {
        var json = ExtractJson(content);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("candidates", out var candidatesElement) || candidatesElement.ValueKind != JsonValueKind.Array)
            throw new AiException("Malformed AI skill-match response.");

        var results = new List<RankedCandidateAiResult>();
        foreach (var item in candidatesElement.EnumerateArray())
        {
            if (!item.TryGetProperty("employeeId", out var idElement))
                continue;

            var employeeId = idElement.GetInt64();
            if (!validEmployeeIds.Contains(employeeId))
                continue;

            var reason = item.TryGetProperty("reason", out var reasonElement)
                ? reasonElement.GetString() ?? string.Empty
                : string.Empty;

            decimal? suggested = null;
            if (item.TryGetProperty("suggestedUtilisation", out var utilElement) &&
                utilElement.ValueKind is JsonValueKind.Number)
            {
                suggested = utilElement.GetDecimal();
            }

            var name = item.TryGetProperty("name", out var nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;

            results.Add(new RankedCandidateAiResult(employeeId, name, reason, suggested));
        }

        return new SkillMatchAiResponse(results);
    }

    public static RiskSummaryAiResponse ParseRiskSummary(string content)
    {
        var json = ExtractJson(content);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("paragraph", out var paragraphElement))
            return new RiskSummaryAiResponse(paragraphElement.GetString() ?? string.Empty);

        if (root.ValueKind == JsonValueKind.String)
            return new RiskSummaryAiResponse(root.GetString() ?? string.Empty);

        throw new AiException("Malformed AI risk-summary response.");
    }

    private static string ExtractJson(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
            throw new AiException("AI response did not contain JSON.");

        return content[start..(end + 1)];
    }
}
