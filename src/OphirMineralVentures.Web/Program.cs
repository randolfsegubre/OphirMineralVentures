using OphirMineralVentures.Web.Middleware;
using OphirMineralVentures.Web.RateLimiting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();


await app.BootUmbracoAsync();

if (args.Contains("--seed-phase2"))
{
    await OphirMineralVentures.Web.Seed.Phase2Seeder.RunAsync(app.Services);
    return;
}

if (args.Contains("--seed-error-page"))
{
    await OphirMineralVentures.Web.Seed.ErrorPageSeeder.RunAsync(app.Services);
    return;
}

// CLAUDE.md §7 — always on, all environments except local dev.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();

    // The custom 500 page is production/staging only - in Development, ASP.NET Core's own
    // detailed exception page is more useful for actually fixing the bug, and stays in place
    // deliberately (the standard ASP.NET Core convention this mirrors).
    app.UseExceptionHandler("/error/500");
}

// The 404 case itself is handled natively inside Umbraco's own content-resolution pipeline (see
// ErrorPageContentFinder), not by re-executing to a path here - CLAUDE.md's own "the owner must be
// able to edit content himself" constraint applies to the 404 page too, so it's a real,
// backoffice-editable content node, not a route. This re-execute wrapper stays registered as a
// generic safety net for any other non-content 404/4xx/5xx that doesn't go through Umbraco's
// pipeline at all (a status code set by other custom code, for example).
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ContactFormRateLimitMiddleware>();

// Must be mapped here, before app.UseUmbraco() below - see ErrorPageMiddleware's own doc comment
// for why an MVC controller/view can't be used for this instead. Only serves /error/500 now - see
// its own doc comment for why the 500 case is a deliberate exception to "error pages must be
// CMS-editable" (a hardcoded last-resort matters precisely when Umbraco itself might be broken).
app.MapErrorPages();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
