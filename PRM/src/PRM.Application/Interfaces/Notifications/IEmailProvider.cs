namespace PRM.Application.Interfaces.Notifications;

/// <summary>
/// Pluggable email delivery adapter (SMTP, etc.).
/// Register additional implementations and select via <c>Email:Provider</c> in appsettings.
/// </summary>
public interface IEmailProvider
{
    string ProviderName { get; }

    Task SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);
}

public sealed record EmailSendRequest(
    string From,
    string To,
    string Subject,
    string HtmlBody);
