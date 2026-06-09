namespace PRM.Infrastructure.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; init; } = "PRM-Super-Secret-Key-Change-In-Production-Min32Chars!";
    public string Issuer { get; init; } = "PRM.Api";
    public string Audience { get; init; } = "PRM.Client";
    public int AccessTokenMinutes { get; init; } = 15;
}
