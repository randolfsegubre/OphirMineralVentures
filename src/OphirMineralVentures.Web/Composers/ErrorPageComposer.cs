using OphirMineralVentures.Web.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace OphirMineralVentures.Web.Composers;

public class ErrorPageComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.SetContentLastChanceFinder<ErrorPageContentFinder>();
    }
}
