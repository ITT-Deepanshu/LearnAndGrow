namespace PRM.Infrastructure.Ai;

public sealed class AiSettings
{
    public const string SectionName = "Ai";

    public GemmaSettings Gemma { get; set; } = new();
}

public sealed class GemmaSettings
{
    /// <summary>Base URL of the in-house Gemma host (e.g. http://164.52.211.238).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = "gemma";

    /// <summary>Generate endpoint path (e.g. /api/generate).</summary>
    public string ApiPath { get; set; } = "/api/generate";

    /// <summary>HTTP header name for the API key (e.g. apikey).</summary>
    public string ApiKeyHeader { get; set; } = "apikey";

    /// <summary>API key is read from Admin → System Configuration when true.</summary>
    public bool RequireApiKey { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 120;
}
