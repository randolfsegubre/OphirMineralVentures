using System.ComponentModel.DataAnnotations;

namespace OphirMineralVentures.Web.Models;

public class ContactFormModel
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string InquiryType { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>Honeypot field — left blank by real visitors, filled in by bots. Never rendered visibly.</summary>
    public string? HoneypotField { get; set; }
}
