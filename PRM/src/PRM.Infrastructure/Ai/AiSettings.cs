namespace PRM.Infrastructure.Ai;

public sealed class AiSettings
{
    public const string SectionName = "Ai";

    public ProviderSettings Gemini { get; set; } = new();
    public ProviderSettings Grok { get; set; } = new();
}

public sealed class ProviderSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
