using System.Text.Json;
using PRM.Application.Interfaces.Ai;
using PRM.Domain.Exceptions;

namespace PRM.Infrastructure.Ai;

internal static class AiResponseParser
{
    public static SkillMatchAiResponse ParseSkillMatch(string content, IReadOnlySet<long> validResourceProfileIds)
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
            if (!validResourceProfileIds.Contains(employeeId))
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

    public static TeamSkillMatchAiResponse ParseTeamSkillMatch(string content, IReadOnlySet<long> validResourceProfileIds)
    {
        var json = ExtractJson(content);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var teamDefined = ParseTeamDefined(root);
        var assignments = ParseAssignments(root, validResourceProfileIds);
        var unfilled = ParseUnfilled(root);

        return new TeamSkillMatchAiResponse(teamDefined, assignments, unfilled);
    }

    private static IReadOnlyList<TeamRoleDefinitionAiResult> ParseTeamDefined(JsonElement root)
    {
        if (!root.TryGetProperty("teamDefined", out var element) || element.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<TeamRoleDefinitionAiResult>();
        foreach (var item in element.EnumerateArray())
        {
            var roleTitle = item.TryGetProperty("roleTitle", out var titleElement)
                ? titleElement.GetString() ?? string.Empty
                : string.Empty;
            var count = item.TryGetProperty("count", out var countElement) && countElement.ValueKind == JsonValueKind.Number
                ? countElement.GetInt32()
                : 1;

            var skills = new List<RequiredSkillAiResult>();
            if (item.TryGetProperty("requiredSkills", out var skillsElement) && skillsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var skill in skillsElement.EnumerateArray())
                {
                    var name = skill.TryGetProperty("name", out var nameElement)
                        ? nameElement.GetString() ?? string.Empty
                        : string.Empty;
                    var minProficiency = skill.TryGetProperty("minProficiency", out var profElement)
                        ? profElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (!string.IsNullOrWhiteSpace(name))
                        skills.Add(new RequiredSkillAiResult(name, minProficiency));
                }
            }

            if (!string.IsNullOrWhiteSpace(roleTitle))
                results.Add(new TeamRoleDefinitionAiResult(roleTitle, Math.Max(1, count), skills));
        }

        return results;
    }

    private static IReadOnlyList<TeamAssignmentAiResult> ParseAssignments(
        JsonElement root,
        IReadOnlySet<long> validResourceProfileIds)
    {
        if (!root.TryGetProperty("assignments", out var element) || element.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<TeamAssignmentAiResult>();
        var usedIds = new HashSet<long>();

        foreach (var item in element.EnumerateArray())
        {
            if (!item.TryGetProperty("employeeId", out var idElement) || idElement.ValueKind != JsonValueKind.Number)
                continue;

            var employeeId = idElement.GetInt64();
            if (!validResourceProfileIds.Contains(employeeId) || !usedIds.Add(employeeId))
                continue;

            var roleTitle = item.TryGetProperty("roleTitle", out var titleElement)
                ? titleElement.GetString() ?? string.Empty
                : string.Empty;
            var slotNumber = item.TryGetProperty("slotNumber", out var slotElement) && slotElement.ValueKind == JsonValueKind.Number
                ? slotElement.GetInt32()
                : results.Count + 1;
            var why = item.TryGetProperty("why", out var whyElement)
                ? whyElement.GetString() ?? string.Empty
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(roleTitle))
                results.Add(new TeamAssignmentAiResult(roleTitle, slotNumber, employeeId, why));
        }

        return results;
    }

    private static IReadOnlyList<UnfilledRoleAiResult> ParseUnfilled(JsonElement root)
    {
        if (!root.TryGetProperty("unfilled", out var element) || element.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<UnfilledRoleAiResult>();
        foreach (var item in element.EnumerateArray())
        {
            var roleTitle = item.TryGetProperty("roleTitle", out var titleElement)
                ? titleElement.GetString() ?? string.Empty
                : string.Empty;
            var unfilledCount = item.TryGetProperty("unfilledCount", out var countElement) && countElement.ValueKind == JsonValueKind.Number
                ? countElement.GetInt32()
                : 1;
            var reason = item.TryGetProperty("reason", out var reasonElement)
                ? reasonElement.GetString() ?? string.Empty
                : string.Empty;
            var detail = item.TryGetProperty("detail", out var detailElement)
                ? detailElement.GetString() ?? string.Empty
                : string.Empty;

            if (!string.IsNullOrWhiteSpace(roleTitle))
                results.Add(new UnfilledRoleAiResult(roleTitle, Math.Max(1, unfilledCount), reason, detail));
        }

        return results;
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
