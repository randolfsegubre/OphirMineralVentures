using System.Xml.Linq;

namespace OphirMineralVentures.Web.Seo;

/// <summary>Builds a sitemap.xml document with per-culture hreflang alternates. Google's own guidance
/// requires every url entry — including the "self" one — to list every language variant as an
/// alternate, so each page's set of culture URLs produces one &lt;url&gt; entry per culture, each
/// carrying the full set of alternates.</summary>
public static class SitemapGenerator
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    public static XDocument Generate(IEnumerable<SitemapPage> pages)
    {
        var urlset = new XElement(SitemapNs + "urlset", new XAttribute(XNamespace.Xmlns + "xhtml", XhtmlNs.NamespaceName));

        foreach (var page in pages)
        {
            foreach (var (culture, url) in page.UrlsByCulture)
            {
                var urlElement = new XElement(SitemapNs + "url", new XElement(SitemapNs + "loc", url));

                foreach (var (alternateCulture, alternateUrl) in page.UrlsByCulture)
                {
                    urlElement.Add(new XElement(
                        XhtmlNs + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", alternateCulture),
                        new XAttribute("href", alternateUrl)));
                }

                urlset.Add(urlElement);
            }
        }

        return new XDocument(new XDeclaration("1.0", "utf-8", null), urlset);
    }
}
