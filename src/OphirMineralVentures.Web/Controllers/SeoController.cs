using Microsoft.AspNetCore.Mvc;
using OphirMineralVentures.Web.Seo;
using Umbraco.Cms.Core.Web;

namespace OphirMineralVentures.Web.Controllers;

/// <summary>Serves /sitemap.xml and /robots.txt. A plain MVC controller, not a SurfaceController —
/// there's no Umbraco page/culture context for a computed feed. That does mean UmbracoContext isn't
/// established automatically the way it is for a real content-routed request (confirmed live: it
/// throws "Wasn't able to get an UmbracoContext" without this), since Umbraco's own request pipeline
/// only sets it up when a URL actually resolves to a content node — /sitemap.xml never does. Fixed by
/// explicitly ensuring one via IUmbracoContextFactory, the documented pattern for exactly this case.</summary>
public class SeoController : Controller
{
    private readonly UmbracoSitemapPageSource _sitemapPageSource;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public SeoController(UmbracoSitemapPageSource sitemapPageSource, IUmbracoContextFactory umbracoContextFactory)
    {
        _sitemapPageSource = sitemapPageSource;
        _umbracoContextFactory = umbracoContextFactory;
    }

    [HttpGet("sitemap.xml")]
    public IActionResult Sitemap()
    {
        using var contextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var pages = _sitemapPageSource.GetPages(HttpContext);
        var document = SitemapGenerator.Generate(pages);
        return Content(document.Declaration + Environment.NewLine + document.Root, "application/xml");
    }

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        // No hardcoded domain — CLAUDE.md §11's domain question is still open, so build the Sitemap
        // directive's absolute URL from whatever host actually served the request.
        var sitemapUrl = $"{Request.Scheme}://{Request.Host}/sitemap.xml";
        var content = $"User-agent: *{Environment.NewLine}Disallow: /umbraco/{Environment.NewLine}{Environment.NewLine}Sitemap: {sitemapUrl}{Environment.NewLine}";
        return Content(content, "text/plain");
    }
}
