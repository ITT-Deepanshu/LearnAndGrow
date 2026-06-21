namespace PRM.Application.Interfaces.Notifications;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? ToName = null);
