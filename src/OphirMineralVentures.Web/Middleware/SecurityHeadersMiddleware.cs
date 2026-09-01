namespace OphirMineralVentures.Web.Middleware;

/// <summary>Security headers required by CLAUDE.md §7 — start strict, loosen only for a specific,
/// understood reason (e.g. adding a third-party script), never speculatively.</summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // The backoffice is an authenticated admin SPA, not the anonymous-visitor surface this CSP
        // exists to protect — applying it here blocks Umbraco's own inline bootstrap script and ES
        // module imports (confirmed live in-browser: the backoffice renders a blank page under it).
        // CLAUDE.md §7: "loosen only for a specific, understood reason" — this is one.
        if (!context.Request.Path.StartsWithSegments("/umbraco", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
                "script-src 'self'; frame-ancestors 'none'");
        }

        await _next(context);
    }
}
