using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRM.Application.Interfaces.Notifications;

namespace PRM.Infrastructure.Email;

/// <summary>
/// Application-facing email facade. Handles enablement checks and routes to the configured <see cref="IEmailProvider"/>.
/// </summary>
public sealed class EmailService(
    IOptions<EmailOptions> options,
    IEnumerable<IEmailProvider> providers,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation(
                "Email disabled — would send '{Subject}' to {To}",
                message.Subject,
                message.To);
            return;
        }

        if (string.IsNullOrWhiteSpace(message.To))
        {
            logger.LogWarning("Skipping email '{Subject}' — recipient address is empty.", message.Subject);
            return;
        }

        var provider = ResolveProvider();
        var from = $"{_options.FromName} <{_options.FromEmail}>";

        await provider.SendAsync(
            new EmailSendRequest(from, message.To, message.Subject, message.HtmlBody),
            cancellationToken);

        logger.LogInformation("Email sent: '{Subject}' to {To} via {Provider}", message.Subject, message.To, provider.ProviderName);
    }

    private IEmailProvider ResolveProvider()
    {
        var provider = providers.FirstOrDefault(p =>
            p.ProviderName.Equals(_options.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            var registered = string.Join(", ", providers.Select(p => p.ProviderName));
            throw new InvalidOperationException(
                $"Email provider '{_options.Provider}' is not registered. Registered providers: {registered}.");
        }

        return provider;
    }
}
