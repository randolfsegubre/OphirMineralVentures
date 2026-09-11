using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OphirMineralVentures.Web.Middleware;

namespace OphirMineralVentures.Web.Tests.Middleware;

/// <summary>
/// Exercises the real Program.cs pipeline wiring - UseExceptionHandler + ErrorPageMiddleware's
/// /error/500 branch - in an isolated TestServer (same approach as SecurityHeadersMiddlewareTests)
/// rather than booting the full Umbraco host.
///
/// The 404 case is deliberately NOT covered here - it moved to
/// <see cref="OphirMineralVentures.Web.Services.ErrorPageContentFinder"/>, a real, backoffice-editable
/// content node (CLAUDE.md's "the owner must be able to edit content himself" applies to the 404
/// page too, so it can't be a hardcoded page or route the way 500 still deliberately is). That
/// finder has no unit test for the same reason UmbracoSitemapPageSource doesn't: it's thin
/// Umbraco-integration glue (tree-walking via IPublishedContentQuery/IPublishedContent.DescendantsOrSelf(),
/// which needs real Umbraco infrastructure under the hood, not just an interface to mock) rather than
/// independently testable logic - see its own doc comment, and 04_ARCHITECTURE_AND_PATTERNS_GUIDE.md's
/// card, for the reasoning and how it was verified live instead.
/// </summary>
public class ErrorPageMiddlewareTests
{
    private static async Task<TestServer> CreateServerAsync(bool addProductionErrorHandler)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        if (addProductionErrorHandler)
                        {
                            app.UseExceptionHandler("/error/500");
                        }

                        app.MapErrorPages();

                        // A route that always throws, standing in for "any unhandled exception
                        // anywhere in the real pipeline" - the test doesn't care where the throw
                        // came from, only that it's handled.
                        app.Map("/throws", throwApp => throwApp.Run(_ => throw new InvalidOperationException("QA test exception")));
                    });
            })
            .StartAsync();

        return host.GetTestServer();
    }

    [Fact]
    public async Task DirectHitToErrorServerErrorPath_Returns500_WithTheCustomPage()
    {
        using var server = await CreateServerAsync(addProductionErrorHandler: false);
        var response = await server.CreateClient().GetAsync("/error/500");

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Something Went Wrong", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnhandledException_InProduction_Returns500_WithTheCustomPage_NeverLeakingTheOriginalMessage()
    {
        using var server = await CreateServerAsync(addProductionErrorHandler: true);
        var response = await server.CreateClient().GetAsync("/throws");

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Something Went Wrong", body);
        Assert.Contains("Back to Home", body);
        // The real exception message/stack trace must never leak to a visitor.
        Assert.DoesNotContain("QA test exception", body);
    }

    [Fact]
    public async Task UnhandledException_WithoutProductionHandler_PropagatesRatherThanBeingSilentlySwallowed()
    {
        // Confirms the Development-only gate in Program.cs is real: without UseExceptionHandler
        // registered, an unhandled exception is NOT quietly turned into a generic 500 - it
        // propagates, which is exactly what lets ASP.NET Core's own Developer Exception Page take
        // over locally.
        using var server = await CreateServerAsync(addProductionErrorHandler: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => server.CreateClient().GetAsync("/throws"));
    }
}
