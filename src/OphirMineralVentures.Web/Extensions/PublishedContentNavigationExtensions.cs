using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services.Navigation;

namespace OphirMineralVentures.Web.Extensions;

/// <summary>
/// Umbraco 18's IPublishedContent.Children()/Root() extensions need an IPublishedStatusFilteringService,
/// which isn't registered in DI on this scaffold (confirmed via HttpContext.RequestServices — it's a
/// genuine gap, not a Razor-view injection quirk). These wrappers use IDocumentNavigationQueryService's
/// raw key lookup plus IPublishedContentQuery.Content(Guid), both of which resolve fine, and rely on
/// IPublishedContentQuery already only returning currently-published content — the same filtering the
/// missing service exists to provide.
/// </summary>
public static class PublishedContentNavigationExtensions
{
    public static IEnumerable<IPublishedContent> ChildrenOf(this IPublishedContent content, HttpContext httpContext)
    {
        var nav = httpContext.RequestServices.GetRequiredService<IDocumentNavigationQueryService>();
        var query = httpContext.RequestServices.GetRequiredService<IPublishedContentQuery>();

        if (!nav.TryGetChildrenKeys(content.Key, out IEnumerable<Guid> childrenKeys))
        {
            return [];
        }

        return childrenKeys.Select(query.Content).OfType<IPublishedContent>();
    }

    public static IPublishedContent RootOf(this IPublishedContent content, HttpContext httpContext)
    {
        var segments = content.Path.Split(',');
        if (segments.Length < 2 || !int.TryParse(segments[1], out var rootId))
        {
            return content;
        }

        if (rootId == content.Id)
        {
            return content;
        }

        var query = httpContext.RequestServices.GetRequiredService<IPublishedContentQuery>();
        return query.Content(rootId) ?? content;
    }
}
