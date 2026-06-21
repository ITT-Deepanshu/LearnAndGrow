using FluentAssertions;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using PRM.Application.Interfaces.Notifications;
using PRM.Infrastructure.Email;
using PRM.Infrastructure.Email.Providers;

namespace PRM.UnitTests.Infrastructure;

public class EmailServiceTests
{
    [Fact]
    public async Task SendAsync_WhenDisabled_DoesNotCallProvider()
    {
        var provider = Substitute.For<IEmailProvider>();
        provider.ProviderName.Returns("Smtp");

        var options = Options.Create(new EmailOptions { Enabled = false, Provider = "Smtp" });
        var service = new EmailService(options, [provider], NullLogger<EmailService>.Instance);

        await service.SendAsync(new EmailMessage("user@example.com", "Subject", "<p>Hi</p>"));

        await provider.DidNotReceive().SendAsync(Arg.Any<EmailSendRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenEnabled_RoutesToConfiguredProvider()
    {
        var provider = Substitute.For<IEmailProvider>();
        provider.ProviderName.Returns("Smtp");

        var options = Options.Create(new EmailOptions
        {
            Enabled = true,
            Provider = "Smtp",
            FromEmail = "sender@example.com",
            FromName = "PRM"
        });
        var service = new EmailService(options, [provider], NullLogger<EmailService>.Instance);

        await service.SendAsync(new EmailMessage("user@example.com", "Hello", "<p>Body</p>"));

        await provider.Received(1).SendAsync(
            Arg.Is<EmailSendRequest>(r =>
                r.To == "user@example.com" &&
                r.From == "PRM <sender@example.com>" &&
                r.Subject == "Hello"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenProviderNotRegistered_Throws()
    {
        var options = Options.Create(new EmailOptions { Enabled = true, Provider = "Unknown" });
        var service = new EmailService(options, [], NullLogger<EmailService>.Instance);

        var act = () => service.SendAsync(new EmailMessage("user@example.com", "Hello", "<p>Body</p>"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Unknown*");
    }

    [Fact]
    public async Task SendAsync_WhenRecipientMissing_SkipsProvider()
    {
        var provider = Substitute.For<IEmailProvider>();
        provider.ProviderName.Returns("Smtp");

        var options = Options.Create(new EmailOptions { Enabled = true, Provider = "Smtp" });
        var service = new EmailService(options, [provider], NullLogger<EmailService>.Instance);

        await service.SendAsync(new EmailMessage(" ", "Subject", "<p>Hi</p>"));

        await provider.DidNotReceive().SendAsync(Arg.Any<EmailSendRequest>(), Arg.Any<CancellationToken>());
    }
}

public class SmtpEmailProviderTests
{
    [Fact]
    public async Task SendAsync_WhenUsernameMissing_Throws()
    {
        var provider = new SmtpEmailProvider(
            Options.Create(new SmtpProviderOptions { Password = "app-password" }),
            NullLogger<SmtpEmailProvider>.Instance);

        var act = () => provider.SendAsync(new EmailSendRequest("PRM <a@b.com>", "user@example.com", "Hi", "<p>x</p>"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Username*");
    }

    [Fact]
    public async Task SendAsync_WhenPasswordMissing_Throws()
    {
        var provider = new SmtpEmailProvider(
            Options.Create(new SmtpProviderOptions { Username = "user@gmail.com", Password = "" }),
            NullLogger<SmtpEmailProvider>.Instance);

        var act = () => provider.SendAsync(new EmailSendRequest("PRM <a@b.com>", "user@example.com", "Hi", "<p>x</p>"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Password*");
    }

    [Theory]
    [InlineData(587, true, SecureSocketOptions.StartTls)]
    [InlineData(465, true, SecureSocketOptions.SslOnConnect)]
    public void ResolveSecureSocketOptions_ReturnsExpectedMode(int port, bool useStartTls, SecureSocketOptions expected)
    {
        var options = new SmtpProviderOptions { Port = port, UseStartTls = useStartTls };
        SmtpEmailProvider.ResolveSecureSocketOptions(options).Should().Be(expected);
    }

    [Fact]
    public void NormalizeAppPassword_RemovesSpaces()
    {
        SmtpEmailProvider.NormalizeAppPassword("abcd efgh ijkl mnop").Should().Be("abcdefghijklmnop");
    }
}
