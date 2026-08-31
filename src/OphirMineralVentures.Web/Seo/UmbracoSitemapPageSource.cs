using OphirMineralVentures.Web.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace OphirMineralVentures.Web.Seo;

/// <summary>Walks the published content tree from Home and builds the plain SitemapPage records
/// SitemapGenerator needs. Umbraco-integration glue (per CLAUDE.md §4/02_TESTING_QA_PLAN.md's TDD
/// scope note — configuration/tree-walking, not logic with an independently testable contract), kept
/// thin and verified functionally rather than unit-tested; the actual XML generation it feeds is
/// TDD-covered in SitemapGeneratorTests.</summary>
public class UmbracoSitemapPageSource
{
    // certification has no routable template (CLAUDE.md 01_CONTENT_MODEL_SPEC.md — card-only on
    // certificationsListing); siteSettings sits outside the Home subtree and is never public.
    private static readonly HashSet<string> ExcludedFromSitemap = ["certification", "siteSettings"];

    private readonly UmbracoHelper _umbraco;

    public UmbracoSitemapPageSource(UmbracoHelper umbraco)
    {
        _umbraco = umbraco;
    }

    public IReadOnlyList<SitemapPage> GetPages(HttpContext httpContext)
    {
        var home = _umbraco.ContentAtRoot().FirstOrDefault(x => x.ContentType.Alias == "home");
        if (home is null)
        {
            return [];
        }

        var pages = new List<SitemapPage>();
        CollectPages(home, httpContext, pages);
        return pages;
    }

    private static void CollectPages(IPublishedContent node, HttpContext httpContext, List<SitemapPage> pages)
    {
        if (!ExcludedFromSitemap.Contains(node.ContentType.Alias))
        {
            // UrlMode.Absolute builds on Umbraco's own cached "application URL" (Program.cs's
            // ApplicationUrlDetection setting), which can go stale across local dev profile switches —
            // building off the current request's actual scheme/host instead keeps the sitemap correct
            // regardless of that cache, and costs nothing once there's one fixed production hostname.
            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var urlsByCulture = node.Cultures.Keys
                .ToDictionary(culture => culture, culture => baseUrl + node.Url(culture, UrlMode.Relative));

            if (urlsByCulture.Count > 0)
            {
                pages.Add(new SitemapPage(urlsByCulture));
            }
        }

        foreach (var child in node.ChildrenOf(httpContext))
        {
            CollectPages(child, httpContext, pages);
        }
    }
}
