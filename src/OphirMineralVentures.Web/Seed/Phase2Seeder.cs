using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using uSync.BackOffice;
using uSync.BackOffice.SyncHandlers.Models;

namespace OphirMineralVentures.Web.Seed;

/// <summary>
/// Dev-environment bootstrap (invoked via `dotnet run -- --seed-phase2`) — imports the uSync schema
/// into a fresh database, wires a Template to each routable content type, and creates placeholder
/// content in both cultures. Kept (unlike Phase 1's schema-only seeder, which was deleted) because
/// content is deliberately *not* uSync-tracked (CLAUDE.md §4a — content belongs to the owner, not
/// git), so this is the only way to get a demoable content tree on a fresh clone/environment without
/// clicking through the backoffice by hand. Only run this against an empty database — it always
/// creates new nodes, so running it twice duplicates the whole tree.
/// </summary>
public static class Phase2Seeder
{
    private static readonly Guid SuperUserKey = new("1e70f841-c261-413b-abb2-2d68cdb96094");
    private const int SuperUserId = -1;
    private const string EnUs = "en-US";
    private const string ZhHans = "zh-Hans";
    private const string ZhPlaceholder = "[zh-Hans placeholder — pending client translation]";

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var syncService = sp.GetRequiredService<ISyncService>();
        var contentTypeService = sp.GetRequiredService<IContentTypeService>();
        var contentService = sp.GetRequiredService<IContentService>();
        var json = sp.GetRequiredService<IJsonSerializer>();
        var userService = sp.GetRequiredService<IUserService>();
        var templateService = sp.GetRequiredService<ITemplateService>();

        var adminUser = userService.GetByEmail("rsegubre@gmail.com");
        var userKey = adminUser?.Key ?? SuperUserKey;
        Console.WriteLine($"Admin user found: {adminUser is not null}, using userKey={userKey}");

        Console.WriteLine("=== Importing uSync schema ===");
        var importResult = await syncService.StartupImportAsync(["uSync/v18/"], true, new SyncHandlerOptions());
        Console.WriteLine($"uSync import actions: {importResult.Count()}");
        foreach (var a in importResult.Where(a => !a.Success))
        {
            Console.WriteLine($"  FAILED: {a.ItemType} {a.Name} — {a.Message}");
        }

        Console.WriteLine("=== Creating templates ===");
        var templateNames = new Dictionary<string, string>
        {
            ["home"] = "Home",
            ["aboutPage"] = "AboutPage",
            ["productsListing"] = "ProductsListing",
            ["product"] = "Product",
            ["sustainabilityPage"] = "SustainabilityPage",
            ["certificationsListing"] = "CertificationsListing",
            ["newsListing"] = "NewsListing",
            ["article"] = "Article",
            ["contactPage"] = "ContactPage",
            ["legalPage"] = "LegalPage",
        };
        foreach (var (alias, name) in templateNames)
        {
            var ct = contentTypeService.Get(alias);
            if (ct is null)
            {
                Console.WriteLine($"  MISSING content type: {alias}");
                continue;
            }

            try
            {
                // IContentTypeService.CreateTemplateAsync (the one-call convenience that also wires
                // DefaultTemplate) returns Status=Unknown with no logged cause on every content type here —
                // use the lower-level template-only creation instead, then wire DefaultTemplate ourselves.
                // The 4-arg overload validates the view's `Layout = "..."` reference against a registered
                // Template entity, which fails here since _Layout.cshtml is a plain Razor layout, not a
                // Template — the 3-arg overload (obsolete in 18.x, removed in 19) doesn't do that check.
#pragma warning disable CS0618
                var templateResult = await templateService.CreateForContentTypeAsync(alias, name, userKey);
#pragma warning restore CS0618
                if (!templateResult.Success || templateResult.Result is null)
                {
                    Console.WriteLine($"  {alias}: template creation failed, status={templateResult.Status}");
                    continue;
                }

                ct.AllowedTemplates = new[] { templateResult.Result };
                ct.SetDefaultTemplate(templateResult.Result);
                var updateResult = await contentTypeService.UpdateAsync(ct, userKey);
                Console.WriteLine($"  {alias}: template created, default-template wiring success={updateResult.Success} result={updateResult.Result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {alias}: EXCEPTION {ex}");
            }
        }

        Console.WriteLine("=== Creating placeholder content ===");

        IContent NewNode(string alias, int parentId, string enName, string zhName)
        {
            var c = contentService.Create(enName, parentId, alias, SuperUserId);
            c.SetCultureName(enName, EnUs);
            c.SetCultureName(zhName, ZhHans);
            return c;
        }

        void SetText(IContent c, string propAlias, string enValue, string? zhValue = null)
        {
            c.SetValue(propAlias, enValue, EnUs);
            c.SetValue(propAlias, zhValue ?? ZhPlaceholder, ZhHans);
        }

        void SetSeo(IContent c, string title, string description)
        {
            SetText(c, "metaTitle", title);
            SetText(c, "metaDescription", description);
        }

        void SaveAndPublish(IContent c)
        {
            contentService.Save(c, SuperUserId);
            var result = contentService.Publish(c, [EnUs, ZhHans], SuperUserId);
            if (!result.Success)
            {
                Console.WriteLine($"  PUBLISH FAILED for {c.GetCultureName(EnUs)}: {result.Result}");
            }
        }

        string BuildBlockList(string elementAlias, (string alias, string en, string zh)[][] items)
        {
            var elementType = contentTypeService.Get(elementAlias)!;
            var value = new BlockListValue();
            var layoutItems = new List<BlockListLayoutItem>();

            foreach (var itemValues in items)
            {
                var key = Guid.NewGuid();
                var data = new BlockItemData(key, elementType.Key, elementAlias);
                foreach (var (alias, en, zh) in itemValues)
                {
                    data.Values.Add(new BlockPropertyValue { Alias = alias, Culture = EnUs, Value = en });
                    data.Values.Add(new BlockPropertyValue { Alias = alias, Culture = ZhHans, Value = zh });
                }

                value.ContentData.Add(data);
                layoutItems.Add(new BlockListLayoutItem(key));
                value.Expose.Add(new BlockItemVariation(key, EnUs, null));
                value.Expose.Add(new BlockItemVariation(key, ZhHans, null));
            }

            value.Layout["Umbraco.BlockList"] = layoutItems;
            return json.Serialize(value);
        }

        // --- home (root) — created before siteSettings so it gets the lower content id and is picked
        // as the implicit site root; also given an explicit domain so "/" resolves to it unambiguously
        // now that siteSettings is a second published root-level node (CLAUDE.md §4a / spec's "not part
        // of the public navigation" note — structurally excluded from nav by living outside the Home
        // subtree, but that alone left root-URL resolution ambiguous between the two root nodes). ---
        var home = NewNode("home", -1, "Ophir Mineral Ventures", "[ZH] Ophir Mineral Ventures");
        SetSeo(home, "Ophir Mineral Ventures", "Nickel and chromite ore exporter based in the Philippines, serving industrial buyers across East Asia.");
        SetText(home, "heroHeading", "Philippine ore, verified sourcing, delivered on schedule.");
        SetText(home, "heroSubtext", "Ophir Mineral Ventures, Inc. mines and exports nickel and chromite ore to industrial buyers across East Asia, backed by transparent permits, assay documentation, and a track record of on-time bulk shipments.");
        home.SetValue("stats", BuildBlockList("labelValueItem",
        [
            [("label", "Ore lines exported", "[ZH] Ore lines exported"), ("value", "2", "[ZH] 2")],
            [("label", "Export market", "[ZH] Export market"), ("value", "East Asia", "[ZH] East Asia")],
            [("label", "Ore exported to date", "[ZH] Ore exported to date"), ("value", "[confirm with client]", "[ZH] [confirm with client]")],
            [("label", "Bulk shipments completed", "[ZH] Bulk shipments completed"), ("value", "[confirm with client]", "[ZH] [confirm with client]")],
        ]), EnUs);
        home.SetValue("stats", home.GetValue<string>("stats", EnUs), ZhHans);
        SaveAndPublish(home);

        // Path-prefixed culture routing needs a real domain-to-culture mapping (CLAUDE.md §4a), and
        // Umbraco's domain matching is host+port specific — pinned to the HTTPS "Umbraco.Web.UI"
        // launch profile port since that's the one that also supports backoffice login. Reseed after
        // changing which port/profile you standardize on locally, or hreflang/zh-Hans routes will 404.
        var domainService = sp.GetRequiredService<IDomainService>();
        var domainResult = await domainService.UpdateDomainsAsync(home.Key, new DomainsUpdateModel
        {
            DefaultIsoCode = EnUs,
            Domains =
            [
                new DomainModel { DomainName = "localhost:44325/", IsoCode = EnUs },
                new DomainModel { DomainName = "localhost:44325/zh-hans", IsoCode = ZhHans },
            ],
        });
        Console.WriteLine($"Domain assignment for home: success={domainResult.Success} status={domainResult.Status}");

        // --- siteSettings (root, sibling of home — never linked to from nav, see note above) ---
        var siteSettings = NewNode("siteSettings", -1, "Site Settings", "[ZH] Site Settings");
        SetText(siteSettings, "companyName", "Ophir Mineral Ventures, Inc.");
        SetText(siteSettings, "registeredAddress", "[Business address — Philippines, confirm with client]");
        SetText(siteSettings, "phone", "[+63 ... — confirm with client]");
        SaveAndPublish(siteSettings);

        // --- aboutPage ---
        var about = NewNode("aboutPage", home.Id, "About", "[ZH] About");
        SetSeo(about, "About", "Company background and leadership.");
        SetText(about, "body",
            "<p>Ophir Mineral Ventures, Inc. is a Philippine mineral trading company sourcing and exporting nickel and chromite ore. <em>[Company history, years active and site locations — confirm with client]</em></p>" +
            "<p>Led by founder and president John David Montilla, the company works directly with accredited mine sites and maintains full documentation for every shipment, from ore transport permit to final assay certificate.</p>",
            "<p>[zh-Hans placeholder — pending client translation]</p>");
        about.SetValue("leadership", BuildBlockList("leadershipItem",
        [
            [("name", "John David Montilla", "[ZH] John David Montilla"), ("role", "Founder & President", "[ZH] Founder & President"), ("bio", "[Bio — confirm with client]", "[ZH] [Bio — confirm with client]")],
        ]), EnUs);
        about.SetValue("leadership", about.GetValue<string>("leadership", EnUs), ZhHans);
        SaveAndPublish(about);

        // --- productsListing + products ---
        var productsListing = NewNode("productsListing", home.Id, "Products", "[ZH] Products");
        SetSeo(productsListing, "Products", "Nickel and chromite ore, fully specified.");
        SetText(productsListing, "intro", "<p>The specifications shown are illustrative only — they will be replaced with actual assay figures once provided.</p>", "<p>[zh-Hans placeholder — pending client translation]</p>");
        SaveAndPublish(productsListing);

        var nickelOre = NewNode("product", productsListing.Id, "Nickel Ore", "[ZH] Nickel Ore");
        SetSeo(nickelOre, "Nickel Ore", "Laterite ore for NPI and stainless feed.");
        SetText(nickelOre, "useCase", "Laterite ore for NPI & stainless feed");
        nickelOre.SetValue("specs", BuildBlockList("labelValueItem",
        [
            [("label", "Ni content (indicative)", "[ZH] Ni content (indicative)"), ("value", "1.5–1.8%", "[ZH] 1.5–1.8%")],
            [("label", "Fe content (indicative)", "[ZH] Fe content (indicative)"), ("value", "~35–48%", "[ZH] ~35–48%")],
            [("label", "Moisture", "[ZH] Moisture"), ("value", "≤35%", "[ZH] ≤35%")],
            [("label", "Typical lot size", "[ZH] Typical lot size"), ("value", "50,000+ WMT", "[ZH] 50,000+ WMT")],
        ]), EnUs);
        nickelOre.SetValue("specs", nickelOre.GetValue<string>("specs", EnUs), ZhHans);
        SaveAndPublish(nickelOre);

        var chromiteOre = NewNode("product", productsListing.Id, "Chromite Ore", "[ZH] Chromite Ore");
        SetSeo(chromiteOre, "Chromite Ore", "Metallurgical and refractory grade chromite.");
        SetText(chromiteOre, "useCase", "Metallurgical & refractory grade");
        chromiteOre.SetValue("specs", BuildBlockList("labelValueItem",
        [
            [("label", "Cr₂O₃ content (indicative)", "[ZH] Cr₂O₃ content (indicative)"), ("value", "38–44%", "[ZH] 38–44%")],
            [("label", "Cr:Fe ratio (indicative)", "[ZH] Cr:Fe ratio (indicative)"), ("value", "~2.0–2.6:1", "[ZH] ~2.0–2.6:1")],
            [("label", "Moisture", "[ZH] Moisture"), ("value", "≤12%", "[ZH] ≤12%")],
            [("label", "Typical lot size", "[ZH] Typical lot size"), ("value", "10,000+ WMT", "[ZH] 10,000+ WMT")],
        ]), EnUs);
        chromiteOre.SetValue("specs", chromiteOre.GetValue<string>("specs", EnUs), ZhHans);
        SaveAndPublish(chromiteOre);

        // --- sustainabilityPage ---
        var sustainability = NewNode("sustainabilityPage", home.Id, "Sustainability", "[ZH] Sustainability");
        SetSeo(sustainability, "Sustainability", "Responsible sourcing initiatives.");
        SetText(sustainability, "body", "<p>Responsible sourcing is built into how Ophir Mineral Ventures operates, from site rehabilitation to community engagement around every mine site we work with.</p>", "<p>[zh-Hans placeholder — pending client translation]</p>");
        sustainability.SetValue("initiatives", BuildBlockList("initiativeItem",
        [
            [("title", "Site rehabilitation", "[ZH] Site rehabilitation"), ("description", "Progressive rehabilitation of worked areas in line with DENR requirements. [Specific initiatives — confirm with client]", "[ZH] Progressive rehabilitation placeholder")],
            [("title", "Community engagement", "[ZH] Community engagement"), ("description", "Local hiring and community programs around operating sites. [Specific programs — confirm with client]", "[ZH] Community engagement placeholder")],
            [("title", "Chain of custody", "[ZH] Chain of custody"), ("description", "Every shipment traceable from mine site to vessel, with permits attached.", "[ZH] Chain of custody placeholder")],
        ]), EnUs);
        sustainability.SetValue("initiatives", sustainability.GetValue<string>("initiatives", EnUs), ZhHans);
        SaveAndPublish(sustainability);

        // --- certificationsListing + certifications ---
        var certificationsListing = NewNode("certificationsListing", home.Id, "Compliance", "[ZH] Compliance");
        SetSeo(certificationsListing, "Compliance", "Registrations, permits and certificates.");
        SetText(certificationsListing, "intro", "<p>These will show our actual registrations and permits, with certificates available to download.</p>", "<p>[zh-Hans placeholder — pending client translation]</p>");
        SaveAndPublish(certificationsListing);

        (string name, string issuingBody, string refNumber)[] certs =
        [
            ("SEC Registration", "Securities and Exchange Commission (Philippines)", "[SEC Reg. No. — confirm with client]"),
            ("DTI / BIR Registration", "Department of Trade and Industry / Bureau of Internal Revenue", "[DTI/BIR TIN — confirm with client]"),
            ("DENR-MGB Mineral Ore Export Permit", "Mines and Geosciences Bureau (DENR)", "[DENR-MGB Permit No. — confirm with client]"),
        ];
        foreach (var (name, issuingBody, refNumber) in certs)
        {
            var cert = NewNode("certification", certificationsListing.Id, name, $"[ZH] {name}");
            SetSeo(cert, name, $"{name} — compliance documentation.");
            SetText(cert, "name", name);
            SetText(cert, "issuingBody", issuingBody);
            SetText(cert, "referenceNumber", refNumber);
            SaveAndPublish(cert);
        }

        // --- newsListing + articles ---
        var newsListing = NewNode("newsListing", home.Id, "News", "[ZH] News");
        SetSeo(newsListing, "News", "Company and shipment updates.");
        SetText(newsListing, "intro", "<p>Company and shipment updates.</p>", "<p>[zh-Hans placeholder — pending client translation]</p>");
        SaveAndPublish(newsListing);

        (string title, DateTime publishDate)[] articles =
        [
            ("[Sample headline] Ophir completes latest bulk shipment", DateTime.UtcNow.AddDays(-7)),
            ("[Sample headline] New compliance documentation now available", DateTime.UtcNow.AddDays(-21)),
        ];
        foreach (var (title, publishDate) in articles)
        {
            var article = NewNode("article", newsListing.Id, title, $"[ZH] {title}");
            SetSeo(article, title, "[Sample article summary — placeholder content for template verification.]");
            SetText(article, "title", title);
            article.SetValue("publishDate", publishDate, EnUs);
            article.SetValue("publishDate", publishDate, ZhHans);
            SetText(article, "body", "<p>[Sample article body — placeholder content for template verification.]</p>", "<p>[zh-Hans placeholder — pending client translation]</p>");
            SaveAndPublish(article);
        }

        // --- contactPage ---
        var contact = NewNode("contactPage", home.Id, "Contact", "[ZH] Contact");
        SetSeo(contact, "Contact", "Request a quote or ask about compliance documents.");
        SetText(contact, "address", "[Business address — Philippines, confirm with client]");
        SetText(contact, "phone", "[+63 ... — confirm with client]");
        SetText(contact, "hours", "Mon–Fri, 9:00–18:00 PHT");
        SaveAndPublish(contact);

        // --- legalPage x2 ---
        var privacy = NewNode("legalPage", home.Id, "Privacy Policy", "[ZH] Privacy Policy");
        SetSeo(privacy, "Privacy Policy", "How Ophir Mineral Ventures handles personal data.");
        SetText(privacy, "body", "<p>[Privacy policy content — to be provided/drafted before go-live.]</p>", "<p>[zh-Hans placeholder — pending client translation]</p>");
        SaveAndPublish(privacy);

        var terms = NewNode("legalPage", home.Id, "Terms of Service", "[ZH] Terms of Service");
        SetSeo(terms, "Terms of Service", "Terms governing use of this website.");
        SetText(terms, "body", "<p>[Terms of service content — to be provided/drafted before go-live.]</p>", "<p>[zh-Hans placeholder — pending client translation]</p>");
        SaveAndPublish(terms);

        Console.WriteLine("=== Done ===");
    }
}
