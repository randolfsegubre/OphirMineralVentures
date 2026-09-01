using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OphirMineralVentures.Web.Models;

namespace OphirMineralVentures.Web.Services;

/// <summary>MailKit + SMTP implementation of <see cref="IEmailSender"/> (CLAUDE.md §3). Chosen over
/// SendGrid so the contact form can send through the client's existing Google Workspace SMTP relay
/// rather than requiring a new third-party account before it's actually needed.</summary>
public class MailKitEmailSender : IEmailSender
{
    private readonly EmailSettings _settings;

    public MailKitEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string to, ContactFormModel model, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = $"Website inquiry from {model.CompanyName}";
        message.Body = new TextPart("plain")
        {
            Text = $"Company: {model.CompanyName}\n" +
                   $"Email: {model.Email}\n" +
                   $"Inquiry type: {model.InquiryType}\n\n" +
                   $"{model.Message}",
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
