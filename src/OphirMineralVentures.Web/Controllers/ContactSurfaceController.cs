using Microsoft.AspNetCore.Mvc;
using OphirMineralVentures.Web.Models;
using OphirMineralVentures.Web.Services;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace OphirMineralVentures.Web.Controllers;

/// <summary>The site's only public write path (CLAUDE.md §6) — anti-forgery, honeypot, rate limiting,
/// Post-Redirect-Get. The actual validation/honeypot decision lives in ContactFormProcessor so it's
/// unit-testable without constructing this controller's full Umbraco dependency chain.</summary>
public class ContactSurfaceController : SurfaceController
{
    private readonly ContactFormProcessor _processor;

    public ContactSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        ContactFormProcessor processor)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _processor = processor;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(ContactFormModel model)
    {
        var result = await _processor.ProcessAsync(model, ModelState.IsValid);

        if (result == ContactSubmissionResult.Invalid)
        {
            return CurrentUmbracoPage();
        }

        // Dropped (honeypot) redirects the same way Sent does — no error, no distinguishing behavior
        // a bot could detect — it just doesn't get the "message sent" notice on the reloaded page.
        // Failed (e.g. an SMTP outage) gets its own visible state — a visitor shouldn't be told their
        // message went through when it didn't.
        TempData["ContactFormResult"] = result.ToString();
        return RedirectToCurrentUmbracoPage();
    }
}
