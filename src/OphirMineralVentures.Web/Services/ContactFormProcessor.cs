using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OphirMineralVentures.Web.Models;

namespace OphirMineralVentures.Web.Services;

public enum ContactSubmissionResult
{
    Sent,
    Dropped,
    Invalid,
    Failed,
}

/// <summary>
/// The contact form's decision logic, kept independent of SurfaceController/Umbraco so it's directly
/// unit-testable (CLAUDE.md §4's TDD scope covers this controller's validation/honeypot behavior).
/// </summary>
public class ContactFormProcessor
{
    private readonly IEmailSender _emailSender;
    private readonly string _recipientEmail;
    private readonly ILogger<ContactFormProcessor> _logger;

    // TODO confirm exact recipient inbox per CLAUDE.md §11 — placeholder from §6's sample until then.
    public ContactFormProcessor(
        IEmailSender emailSender,
        string recipientEmail = "john@ophirminerals.com",
        ILogger<ContactFormProcessor>? logger = null)
    {
        _emailSender = emailSender;
        _recipientEmail = recipientEmail;
        _logger = logger ?? NullLogger<ContactFormProcessor>.Instance;
    }

    public async Task<ContactSubmissionResult> ProcessAsync(ContactFormModel model, bool isModelStateValid, CancellationToken cancellationToken = default)
    {
        if (!isModelStateValid)
        {
            return ContactSubmissionResult.Invalid;
        }

        if (!string.IsNullOrEmpty(model.HoneypotField))
        {
            return ContactSubmissionResult.Dropped;
        }

        try
        {
            await _emailSender.SendAsync(_recipientEmail, model, cancellationToken);
            return ContactSubmissionResult.Sent;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Logged, not swallowed — a visitor sees a friendly error (CLAUDE.md §1: this site's whole
            // job is compliance credibility), but the failure itself must stay visible to us.
            _logger.LogError(ex, "Failed to send contact form email to {Recipient}", _recipientEmail);
            return ContactSubmissionResult.Failed;
        }
    }
}
