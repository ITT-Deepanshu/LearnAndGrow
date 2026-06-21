using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PRM.Application.Interfaces.Notifications;

namespace PRM.Infrastructure.Email.Providers;

public sealed class SmtpEmailProvider(
    IOptions<SmtpProviderOptions> options,
    ILogger<SmtpEmailProvider> logger) : IEmailProvider
{
    public const string Name = "Smtp";
    public string ProviderName => Name;

    public async Task SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
    {
        var smtp = options.Value;
        ValidateOptions(smtp);

        var username = smtp.Username.Trim();
        var password = NormalizeAppPassword(smtp.Password);
        var message = BuildMessage(request, username);

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(
                smtp.Host.Trim(),
                smtp.Port,
                ResolveSecureSocketOptions(smtp),
                cancellationToken);

            await client.AuthenticateAsync(username, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            logger.LogInformation("Email sent via SMTP ({Host}) to {To}", smtp.Host, request.To);
        }
        catch (AuthenticationException ex)
        {
            logger.LogError(
                ex,
                "SMTP authentication failed for {Username} on {Host}:{Port}. Gmail error: {Message}",
                username,
                smtp.Host,
                smtp.Port,
                ex.Message);
            throw new InvalidOperationException(
                $"SMTP authentication failed for '{username}'. " +
                "2-Step Verification alone is not enough — you must create a Google App Password " +
                "(Google Account → Security → App passwords) and put the 16-character password in Email:Smtp:Password. " +
                "Do not use your normal Gmail login password. " +
                "Ensure Email:Smtp:Username exactly matches the Gmail account that owns the App Password.",
                ex);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "SMTP send failed. Host={Host}, From={From}, To={To}, Subject={Subject}",
                smtp.Host,
                request.From,
                request.To,
                request.Subject);
            throw;
        }
    }

    private static void ValidateOptions(SmtpProviderOptions smtp)
    {
        if (string.IsNullOrWhiteSpace(smtp.Host))
            throw new InvalidOperationException("Email:Smtp:Host is not configured in appsettings.");

        if (string.IsNullOrWhiteSpace(smtp.Username))
            throw new InvalidOperationException("Email:Smtp:Username is not configured in appsettings.");

        if (string.IsNullOrWhiteSpace(smtp.Password))
            throw new InvalidOperationException("Email:Smtp:Password is not configured in appsettings.");
    }

    internal static SecureSocketOptions ResolveSecureSocketOptions(SmtpProviderOptions smtp)
    {
        if (smtp.Port == 465)
            return SecureSocketOptions.SslOnConnect;

        return smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
    }

    internal static string NormalizeAppPassword(string password) =>
        password.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();

    private static MimeMessage BuildMessage(EmailSendRequest request, string authenticatedUsername)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(request.From));

        var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address;
        if (!string.Equals(fromAddress, authenticatedUsername, StringComparison.OrdinalIgnoreCase))
        {
            message.From.Clear();
            message.From.Add(new MailboxAddress("PRM", authenticatedUsername));
        }

        message.To.Add(MailboxAddress.Parse(request.To));
        message.Subject = request.Subject;
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = request.HtmlBody };
        return message;
    }
}
