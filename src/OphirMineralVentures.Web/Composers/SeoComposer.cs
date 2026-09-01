using OphirMineralVentures.Web.Seo;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace OphirMineralVentures.Web.Composers;

public class SeoComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<UmbracoSitemapPageSource>();
    }
}
