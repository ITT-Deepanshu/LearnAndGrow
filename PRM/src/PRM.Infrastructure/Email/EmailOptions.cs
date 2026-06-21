namespace PRM.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Registered provider name (e.g. Smtp). Must match <see cref="IEmailProvider.ProviderName"/>.</summary>
    public string Provider { get; set; } = "Smtp";

    public bool Enabled { get; set; } = true;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "PRM";
}
