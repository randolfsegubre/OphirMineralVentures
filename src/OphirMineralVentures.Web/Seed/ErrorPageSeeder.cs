using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using uSync.BackOffice;
using uSync.BackOffice.SyncHandlers.Models;

namespace OphirMineralVentures.Web.Seed;

/// <summary>
/// One-off, idempotent bootstrap (invoked via `dotnet run -- --seed-error-page`) that creates the
/// `errorPage` Document Type (schema, uSync-exported and committed to git per CLAUDE.md §4a) plus a
/// single real content node under Home (content, deliberately NOT uSync-tracked — the owner's own
/// data, same rule every other page's content follows). Exists because the 404 page must be
/// something John can actually open in the backoffice and rewrite, not a hardcoded string in this
/// repo — see the `ErrorPageContentFinder` doc comment for how it's served, and
/// `docs/build/04_ARCHITECTURE_AND_PATTERNS_GUIDE.md`'s `ErrorPageContentFinder` card for why this
/// replaced an earlier, non-editable version of the 404 page. Safe to re-run: checks for the
/// content type/node before creating either.
/// </summary>
public static class ErrorPageSeeder
{
    private const int SuperUserId = -1;
    private const string EnUs = "en-US";
    private const string ZhHans = "zh-Hans";

    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var contentTypeService = sp.GetRequiredService<IContentTypeService>();
        var contentService = sp.GetRequiredService<IContentService>();
        var templateService = sp.GetRequiredService<ITemplateService>();
        var userService = sp.GetRequiredService<IUserService>();
        var syncService = sp.GetRequiredService<ISyncService>();
        var shortStringHelper = sp.GetRequiredService<IShortStringHelper>();
        var dataTypeService = sp.GetRequiredService<IDataTypeService>();

        var adminUser = userService.GetByEmail("rsegubre@gmail.com");
        var userKey = adminUser?.Key ?? new Guid("1e70f841-c261-413b-abb2-2d68cdb96094");

        var existingType = contentTypeService.Get("errorPage");
        if (existingType is null)
        {
            Console.WriteLine("=== Creating errorPage Document Type ===");

            // Reuse the exact data types already backing legalPage's "body" (Richtext editor) and
            // siteSettings' "companyName" (Textstring) rather than guessing default names/GUIDs -
            // guarantees this matches what's already installed on this instance.
            var legalPage = contentTypeService.Get("legalPage") ?? throw new InvalidOperationException("legalPage content type not found - run the main content-model seed first.");
            var siteSettings = contentTypeService.Get("siteSettings") ?? throw new InvalidOperationException("siteSettings content type not found - run the main content-model seed first.");
            var richTextDataTypeKey = legalPage.PropertyTypes.First(p => p.Alias == "body").DataTypeKey;
            var textBoxDataTypeKey = siteSettings.PropertyTypes.First(p => p.Alias == "companyName").DataTypeKey;
            var richTextDataType = await dataTypeService.GetAsync(richTextDataTypeKey) ?? throw new InvalidOperationException("Richtext data type not found.");
            var textBoxDataType = await dataTypeService.GetAsync(textBoxDataTypeKey) ?? throw new InvalidOperationException("Textstring data type not found.");

            var contentType = new ContentType(shortStringHelper, -1)
            {
                Alias = "errorPage",
                Name = "Error Page",
                Icon = "icon-alert",
                AllowedAsRoot = false,
                Variations = ContentVariation.Culture,
            };
            contentType.AddPropertyGroup("content", "Content");
            contentType.AddPropertyType(
                new PropertyType(shortStringHelper, textBoxDataType, "heading")
                {
                    Name = "Heading",
                    Mandatory = false,
                    Variations = ContentVariation.Culture,
                },
                "content");
            contentType.AddPropertyType(
                new PropertyType(shortStringHelper, richTextDataType, "message")
                {
                    Name = "Message",
                    Mandatory = false,
                    Variations = ContentVariation.Culture,
                },
                "content");

            var createResult = await contentTypeService.CreateAsync(contentType, userKey);
            if (!createResult.Success)
            {
                Console.WriteLine($"  errorPage: content type creation failed, status={createResult.Result}");
                return;
            }

            var savedType = contentTypeService.Get("errorPage") ?? throw new InvalidOperationException("errorPage content type was reported created but can't be re-fetched.");

#pragma warning disable CS0618 // see Phase2Seeder's own comment on why the 3-arg overload is used
            var templateResult = await templateService.CreateForContentTypeAsync("errorPage", "ErrorPage", userKey);
#pragma warning restore CS0618
            if (templateResult.Success && templateResult.Result is not null)
            {
                savedType.AllowedTemplates = new[] { templateResult.Result };
                savedType.SetDefaultTemplate(templateResult.Result);
                await contentTypeService.UpdateAsync(savedType, userKey);
            }
            else
            {
                Console.WriteLine($"  errorPage: template creation failed, status={templateResult.Status}");
            }

            Console.WriteLine("=== Exporting errorPage schema to uSync (commit the resulting files) ===");
            await syncService.StartupExportAsync("uSync/v18/", new SyncHandlerOptions(), null!);
        }
        else
        {
            Console.WriteLine("errorPage Document Type already exists - skipping schema creation.");
        }

        var home = contentService.GetRootContent().FirstOrDefault(c => c.ContentType.Alias == "home");
        if (home is null)
        {
            Console.WriteLine("home content node not found - run the main content-model seed first, skipping content node creation.");
            return;
        }

        var existingNode = contentService.GetPagedChildren(home.Id, 0, 100, out _, null, null, null, false)
            .FirstOrDefault(c => c.ContentType.Alias == "errorPage");
        if (existingNode is not null)
        {
            Console.WriteLine("errorPage content node already exists - nothing more to do.");
            return;
        }

        Console.WriteLine("=== Creating the errorPage content node (editable placeholder copy) ===");
        var node = contentService.Create("Page Not Found", home.Id, "errorPage", SuperUserId);
        node.SetCultureName("Page Not Found", EnUs);
        node.SetCultureName("[ZH] Page Not Found", ZhHans);
        node.SetValue("heading", "We couldn't find that page", EnUs);
        node.SetValue("heading", "[zh-Hans placeholder — pending client translation]", ZhHans);
        node.SetValue("message", "<p>The page you're looking for doesn't exist or may have moved.</p>", EnUs);
        node.SetValue("message", "<p>[zh-Hans placeholder — pending client translation]</p>", ZhHans);
        // No nav-hide flag needed - _Nav.cshtml builds its links from an explicit content-type
        // alias allowlist (NavChild("aboutPage") etc.), so an errorPage child never appears there
        // regardless of where it sits in the tree.
        contentService.Save(node, SuperUserId);
        var publishResult = contentService.Publish(node, [EnUs, ZhHans], SuperUserId);
        Console.WriteLine($"errorPage content node published: success={publishResult.Success} status={publishResult.Result}");
    }
}
