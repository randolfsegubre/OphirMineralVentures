using Moq;
using OphirMineralVentures.Web.Models;
using OphirMineralVentures.Web.Services;

namespace OphirMineralVentures.Web.Tests.Services;

public class ContactFormProcessorTests
{
    private static ContactFormModel ValidModel(string? honeypot = null) => new()
    {
        CompanyName = "Acme Trading Co.",
        Email = "buyer@example.com",
        InquiryType = "Buyer inquiry / quote request",
        Message = "We'd like a quote for nickel ore.",
        HoneypotField = honeypot,
    };

    [Fact]
    public async Task ProcessAsync_WhenModelStateInvalid_ReturnsInvalid_AndDoesNotSendEmail()
    {
        var emailSender = new Mock<IEmailSender>();
        var processor = new ContactFormProcessor(emailSender.Object);

        var result = await processor.ProcessAsync(ValidModel(), isModelStateValid: false);

        Assert.Equal(ContactSubmissionResult.Invalid, result);
        emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<ContactFormModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenHoneypotFilled_ReturnsDropped_AndDoesNotSendEmail()
    {
        var emailSender = new Mock<IEmailSender>();
        var processor = new ContactFormProcessor(emailSender.Object);

        var result = await processor.ProcessAsync(ValidModel(honeypot: "http://spam.example"), isModelStateValid: true);

        Assert.Equal(ContactSubmissionResult.Dropped, result);
        emailSender.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<ContactFormModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenValidAndHoneypotEmpty_ReturnsSent_AndSendsToConfiguredRecipient()
    {
        var emailSender = new Mock<IEmailSender>();
        var processor = new ContactFormProcessor(emailSender.Object, recipientEmail: "john@ophirminerals.com");
        var model = ValidModel();

        var result = await processor.ProcessAsync(model, isModelStateValid: true);

        Assert.Equal(ContactSubmissionResult.Sent, result);
        emailSender.Verify(s => s.SendAsync("john@ophirminerals.com", model, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ProcessAsync_WhenHoneypotEmptyOrNull_DoesNotDropSubmission(string? honeypot)
    {
        var emailSender = new Mock<IEmailSender>();
        var processor = new ContactFormProcessor(emailSender.Object);

        var result = await processor.ProcessAsync(ValidModel(honeypot), isModelStateValid: true);

        Assert.Equal(ContactSubmissionResult.Sent, result);
    }

    [Fact]
    public async Task ProcessAsync_WhenEmailSenderThrows_ReturnsFailed_InsteadOfPropagatingTheException()
    {
        // A real visitor hitting an SMTP outage should see a friendly error, not a raw 500 — this
        // site's whole purpose is compliance credibility (CLAUDE.md §1), a crash on the one write
        // path undermines that directly.
        var emailSender = new Mock<IEmailSender>();
        emailSender
            .Setup(s => s.SendAsync(It.IsAny<string>(), It.IsAny<ContactFormModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));
        var processor = new ContactFormProcessor(emailSender.Object);

        var result = await processor.ProcessAsync(ValidModel(), isModelStateValid: true);

        Assert.Equal(ContactSubmissionResult.Failed, result);
    }
}
