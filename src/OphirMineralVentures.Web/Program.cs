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

// Unlike the 500 handler above, the custom 404 page applies in every environment, Development
// included - it's a normal visitor-facing UX page, not debug information, so there's no reason
// for it to look different (or be absent) locally.
app.UseStatusCodePagesWithReExecute("/error/{0}");

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ContactFormRateLimitMiddleware>();

// Must be mapped here, before app.UseUmbraco() below - see ErrorPageMiddleware's own doc comment
// for why an MVC controller/view can't be used for this instead.
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
