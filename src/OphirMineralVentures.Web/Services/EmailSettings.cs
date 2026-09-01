namespace OphirMineralVentures.Web.Services;

/// <summary>SMTP connection details, bound from the "Email" config section. Real values live in
/// user-secrets locally and host-level environment variables in production (CLAUDE.md §4) — never
/// committed here or in appsettings.json.</summary>
public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
}
