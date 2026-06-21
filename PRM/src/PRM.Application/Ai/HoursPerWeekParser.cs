using System.Text.RegularExpressions;

namespace PRM.Application.Ai;

public static partial class HoursPerWeekParser
{
    public static int? Parse(string? requirementText)
    {
        if (string.IsNullOrWhiteSpace(requirementText))
            return null;

        var text = requirementText.ToLowerInvariant();
        if (!text.Contains("week", StringComparison.Ordinal) && !text.Contains("weekly", StringComparison.Ordinal))
            return null;

        var matches = HourPattern().Matches(text);
        if (matches.Count == 0)
            return null;

        return matches
            .Select(m => int.Parse(m.Groups[1].Value))
            .Max();
    }

    [GeneratedRegex(@"\b(\d{1,2})\s*(?:hrs?|hours?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex HourPattern();
}
