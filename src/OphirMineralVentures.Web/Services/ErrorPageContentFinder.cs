using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Routing;

namespace OphirMineralVentures.Web.Services;

/// <summary>
/// Umbraco's own extension point for "nothing else matched this URL" (registered via
/// <c>builder.SetContentLastChanceFinder&lt;ErrorPageContentFinder&gt;()</c> in
/// <see cref="Composers.ErrorPageComposer"/>), used here to serve the site's 404 page as a real,
/// backoffice-editable content node instead of a hardcoded page. This runs inside Umbraco's normal
/// content-resolution pipeline, so it doesn't fight the routing-precedence problem the earlier,
/// hardcoded-only implementation hit (see <see cref="Middleware.ErrorPageMiddleware"/>'s own doc
/// comment) - it *is* the content resolution for this case, not something trying to run around it.
///
/// Resolves <see cref="IPublishedContentQuery"/> through a fresh DI scope per call rather than
/// constructor-injecting it directly: Umbraco registers <see cref="IContentLastChanceFinder"/> as a
/// singleton (confirmed live - the app fails DI validation at startup otherwise, "cannot consume
/// scoped service IPublishedContentQuery from singleton IContentLastChanceFinder"), while
/// IPublishedContentQuery is scoped per-request.
/// </summary>
public class ErrorPageContentFinder : IContentLastChanceFinder
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ErrorPageContentFinder(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        using var scope = _scopeFactory.CreateScope();
        var contentQuery = scope.ServiceProvider.GetRequiredService<IPublishedContentQuery>();

        var errorPage = contentQuery.ContentAtRoot()
            .SelectMany(root => root.DescendantsOrSelf())
            .FirstOrDefault(c => c.ContentType.Alias == "errorPage");

        if (errorPage is null)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(errorPage);
        request.SetResponseStatus(404);
        return Task.FromResult(true);
    }
}
