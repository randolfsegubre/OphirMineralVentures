namespace OphirMineralVentures.Web.Seo;

/// <summary>One logical page's URL in every published culture — e.g. { "en-US": "/about/",
/// "zh-Hans": "/zh-hans/zh-about/" }. Deliberately plain data, no IPublishedContent dependency, so the
/// XML-generation logic (the actual custom logic CLAUDE.md §4/00_BUILD_PLAN.md Phase 4 calls out for
/// TDD) stays testable without needing a real Umbraco content tree.</summary>
public record SitemapPage(IReadOnlyDictionary<string, string> UrlsByCulture);
