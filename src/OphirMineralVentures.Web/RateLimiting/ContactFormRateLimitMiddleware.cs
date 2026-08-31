using System.Threading.RateLimiting;
using OphirMineralVentures.Web.Models;

namespace OphirMineralVentures.Web.RateLimiting;

/// <summary>Rate limits the contact form POST — the site's only public write path (CLAUDE.md §6/§7).
/// A few requests/minute/IP is enough to stop scripted abuse without getting in a real visitor's way.
/// Implemented as a plain form-field-checking middleware (rather than the endpoint-metadata-based
/// UseRateLimiter()/[EnableRateLimiting] combo, or a fixed-path check) because Html.BeginUmbracoForm
/// posts back to whatever page the form is rendered on — not a fixed URL, and that URL varies per
/// culture (/contact/ vs /zh-hans/zh-contact/) — so the only reliable signal is a field the form
/// itself carries. Also means this doesn't depend on exactly where Umbraco's own
/// WithMiddleware/WithEndpoints builder wires up routing internally, and stays testable in isolation
/// via a bare TestServer the same way SecurityHeadersMiddleware is.</summary>
public class ContactFormRateLimitMiddleware
{
    public const int PermitLimit = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private readonly RequestDelegate _next;
    private readonly PartitionedRateLimiter<HttpContext> _limiter;

    public ContactFormRateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
        _limiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    Window = Window,
                    QueueLimit = 0,
                }));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!await IsContactFormSubmissionAsync(context))
        {
            await _next(context);
            return;
        }

        using var lease = await _limiter.AcquireAsync(context, permitCount: 1, context.RequestAborted);
        if (!lease.IsAcquired)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await _next(context);
    }

    private static async Task<bool> IsContactFormSubmissionAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.HasFormContentType)
        {
            return false;
        }

        // Request.Form/ReadFormAsync buffers and rewinds automatically, so downstream MVC model
        // binding still sees the full body — no manual buffering needed here.
        var form = await context.Request.ReadFormAsync(context.RequestAborted);
        return form.ContainsKey(nameof(ContactFormModel.HoneypotField));
    }
}
