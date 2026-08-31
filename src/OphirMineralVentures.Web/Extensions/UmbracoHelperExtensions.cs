using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;

namespace OphirMineralVentures.Web.Extensions;

public static class UmbracoHelperExtensions
{
    /// <summary>
    /// The single siteSettings node. It sits alongside Home as its own content-root node rather than
    /// inside the Home subtree, which is what keeps it out of nav (CLAUDE.md §4a / 01_CONTENT_MODEL_SPEC.md).
    /// </summary>
    public static IPublishedContent? SiteSettings(this UmbracoHelper umbraco) =>
        umbraco.ContentAtRoot().FirstOrDefault(x => x.ContentType.Alias == "siteSettings");
}
