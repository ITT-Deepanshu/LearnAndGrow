namespace PRM.Infrastructure.Email.Providers;

public sealed class SmtpProviderOptions
{
    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    /// <summary>Gmail address used to authenticate with SMTP.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Google App Password (not your regular Gmail password).</summary>
    public string Password { get; set; } = string.Empty;

    public bool UseStartTls { get; set; } = true;
}
