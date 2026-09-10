namespace OphirMineralVentures.Web.Middleware;

/// <summary>
/// Serves the site's 404/500 error pages as a plain terminal middleware branch, not an MVC
/// controller+view. That's a deliberate departure from the usual ASP.NET Core convention:
/// Umbraco's own content-resolution middleware (registered later in Program.cs via UseWebsite())
/// runs ahead of ordinary endpoint routing and short-circuits any path it doesn't recognise as
/// real content with its own backoffice SPA shell, before endpoint dispatch is ever reached — so
/// a controller/view mapped only through MVC routing is unreachable here regardless of pipeline
/// order (confirmed live: forcing early UseRouting/UseEndpoints didn't fix it, it broke the real
/// homepage instead, since Umbraco's page routes aren't pre-registered endpoints either — they're
/// resolved dynamically by that same content middleware). A plain app.Map() branch, by contrast,
/// terminates the request itself the moment the path matches, before Umbraco's middleware ever
/// runs — the same mechanism already proven reliable for this app's own qa-test-throw diagnostic.
/// </summary>
public static class ErrorPageMiddleware
{
    public static void MapErrorPages(this IApplicationBuilder app)
    {
        app.Map("/error/404", branch => branch.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(NotFoundHtml);
        }));

        app.Map("/error/500", branch => branch.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(ServerErrorHtml);
        }));
    }

    // Deliberately not @inherits UmbracoViewPage / extending _Layout.cshtml, and no Model — an
    // error page must never depend on the Umbraco content resolution that might be exactly what's
    // broken. Self-contained HTML reusing the site's own design tokens (site.css) only.
    private const string NotFoundHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>Page Not Found — Ophir Mineral Ventures</title>
            <link rel="stylesheet" href="/css/site.css" />
        </head>
        <body>
            <main style="min-height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;padding:0 24px;">
                <p style="font-family:var(--font-mono);font-size:12px;text-transform:uppercase;letter-spacing:.08em;color:var(--ink-soft);">404</p>
                <h1 style="margin-top:12px;font-family:var(--font-display);font-size:28px;">We couldn&rsquo;t find that page</h1>
                <p style="margin-top:12px;color:var(--ink-soft);max-width:420px;">The page you&rsquo;re looking for doesn&rsquo;t exist or may have moved.</p>
                <a href="/" class="btn btn-primary" style="margin-top:32px;">Back to Home</a>
            </main>
        </body>
        </html>
        """;

    // Same reasoning as NotFoundHtml — no Umbraco content dependency. Doubly important here:
    // this renders precisely when something in the request pipeline already threw.
    private const string ServerErrorHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>Something Went Wrong — Ophir Mineral Ventures</title>
            <link rel="stylesheet" href="/css/site.css" />
        </head>
        <body>
            <main style="min-height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;padding:0 24px;">
                <p style="font-family:var(--font-mono);font-size:12px;text-transform:uppercase;letter-spacing:.08em;color:var(--ink-soft);">Something went wrong</p>
                <h1 style="margin-top:12px;font-family:var(--font-display);font-size:28px;">We hit a snag loading this page</h1>
                <p style="margin-top:12px;color:var(--ink-soft);max-width:420px;">This is on us, not you. Going back to the homepage usually clears it up.</p>
                <a href="/" class="btn btn-primary" style="margin-top:32px;">Back to Home</a>
            </main>
        </body>
        </html>
        """;
}
