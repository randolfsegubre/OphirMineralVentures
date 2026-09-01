using System.Xml.Linq;
using OphirMineralVentures.Web.Seo;

namespace OphirMineralVentures.Web.Tests.Seo;

public class SitemapGeneratorTests
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    [Fact]
    public void Generate_SinglePageSingleCulture_ProducesOneUrlWithSelfReferencingAlternate()
    {
        var pages = new[]
        {
            new SitemapPage(new Dictionary<string, string> { ["en-US"] = "https://ophirminerals.com/about/" }),
        };

        var doc = SitemapGenerator.Generate(pages);

        var urls = doc.Root!.Elements(SitemapNs + "url").ToList();
        Assert.Single(urls);
        Assert.Equal("https://ophirminerals.com/about/", urls[0].Element(SitemapNs + "loc")!.Value);

        var alternates = urls[0].Elements(XhtmlNs + "link").ToList();
        Assert.Single(alternates);
        Assert.Equal("en-US", alternates[0].Attribute("hreflang")!.Value);
        Assert.Equal("https://ophirminerals.com/about/", alternates[0].Attribute("href")!.Value);
    }

    [Fact]
    public void Generate_PageWithTwoCultures_ProducesOneUrlEntryPerCulture_EachListingBothAlternates()
    {
        var pages = new[]
        {
            new SitemapPage(new Dictionary<string, string>
            {
                ["en-US"] = "https://ophirminerals.com/about/",
                ["zh-Hans"] = "https://ophirminerals.com/zh-hans/zh-about/",
            }),
        };

        var doc = SitemapGenerator.Generate(pages);

        var urls = doc.Root!.Elements(SitemapNs + "url").ToList();
        Assert.Equal(2, urls.Count);

        var locs = urls.Select(u => u.Element(SitemapNs + "loc")!.Value).ToHashSet();
        Assert.Contains("https://ophirminerals.com/about/", locs);
        Assert.Contains("https://ophirminerals.com/zh-hans/zh-about/", locs);

        // Every url entry — including the English one — lists BOTH cultures as alternates
        // (Google's own guidance: self-referencing hreflang is required, not optional).
        foreach (var url in urls)
        {
            var hreflangs = url.Elements(XhtmlNs + "link").Select(l => l.Attribute("hreflang")!.Value).ToHashSet();
            Assert.Equal(new HashSet<string> { "en-US", "zh-Hans" }, hreflangs);
        }
    }

    [Fact]
    public void Generate_MultiplePages_DoesNotCrossContaminateAlternatesBetweenPages()
    {
        var pages = new[]
        {
            new SitemapPage(new Dictionary<string, string>
            {
                ["en-US"] = "https://ophirminerals.com/about/",
                ["zh-Hans"] = "https://ophirminerals.com/zh-hans/zh-about/",
            }),
            new SitemapPage(new Dictionary<string, string>
            {
                ["en-US"] = "https://ophirminerals.com/contact/",
                ["zh-Hans"] = "https://ophirminerals.com/zh-hans/zh-contact/",
            }),
        };

        var doc = SitemapGenerator.Generate(pages);

        var urls = doc.Root!.Elements(SitemapNs + "url").ToList();
        Assert.Equal(4, urls.Count);

        var aboutEn = urls.Single(u => u.Element(SitemapNs + "loc")!.Value == "https://ophirminerals.com/about/");
        var aboutAlternateHrefs = aboutEn.Elements(XhtmlNs + "link").Select(l => l.Attribute("href")!.Value).ToHashSet();
        Assert.DoesNotContain("https://ophirminerals.com/contact/", aboutAlternateHrefs);
        Assert.DoesNotContain("https://ophirminerals.com/zh-hans/zh-contact/", aboutAlternateHrefs);
    }

    [Fact]
    public void Generate_EmptyPageList_ProducesEmptyUrlset()
    {
        var doc = SitemapGenerator.Generate([]);

        Assert.Equal(SitemapNs + "urlset", doc.Root!.Name);
        Assert.Empty(doc.Root.Elements());
    }

    [Fact]
    public void Generate_RootElement_DeclaresBothNamespaces()
    {
        var doc = SitemapGenerator.Generate([]);

        // Check what a real consumer (a search engine parsing the served file) actually sees: the
        // serialized form, not XLinq's in-memory pre-serialization representation, which doesn't
        // materialize xmlns as a queryable attribute until round-tripped through text.
        var reparsed = XDocument.Parse(doc.ToString());

        Assert.Equal(SitemapNs.NamespaceName, reparsed.Root!.GetDefaultNamespace().NamespaceName);
        Assert.Equal(XhtmlNs.NamespaceName, reparsed.Root.GetNamespaceOfPrefix("xhtml")?.NamespaceName);
    }
}
