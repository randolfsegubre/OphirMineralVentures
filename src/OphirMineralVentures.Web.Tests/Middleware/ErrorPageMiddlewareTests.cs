using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OphirMineralVentures.Web.Middleware;

namespace OphirMineralVentures.Web.Tests.Middleware;

/// <summary>
/// Exercises the real Program.cs pipeline wiring - UseStatusCodePagesWithReExecute,
/// UseExceptionHandler, and ErrorPageMiddleware.MapErrorPages() - in an isolated TestServer
/// (same approach as SecurityHeadersMiddlewareTests) rather than booting the full Umbraco host.
///
/// ErrorPageMiddleware.MapErrorPages() is used directly here. An earlier version of this feature
/// used a plain MVC ErrorController with Views instead, with this test file standing in for it via
/// app.Map() look-alikes because getting Razor view discovery working in an isolated TestServer
/// wasn't practical. That controller approach turned out to be unreachable in the real app anyway:
/// confirmed live that Umbraco's own content-resolution middleware (registered via UseWebsite())
/// runs ahead of ASP.NET Core's normal endpoint routing/dispatch and short-circuits any path it
/// doesn't recognise as real content - forcing early UseRouting/UseEndpoints to compensate broke
/// the real homepage instead (Umbraco's own content routes aren't pre-registered endpoints either;
/// they're also resolved by that same middleware). ErrorPageMiddleware sidesteps this entirely with
/// a plain app.Map() terminal branch, which is what this test suite now verifies directly - the
/// production code under test is no longer a stand-in for something else, it's the real thing.
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

                        app.UseStatusCodePagesWithReExecute("/error/{0}");

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
    public async Task UnmatchedRoute_Returns404_AndReExecutesToTheCustomErrorPage()
    {
        using var server = await CreateServerAsync(addProductionErrorHandler: false);
        var response = await server.CreateClient().GetAsync("/this-page-does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Page Not Found", body);
        Assert.Contains("Back to Home", body);
    }

    [Fact]
    public async Task DirectHitToErrorNotFoundPath_Returns404_WithTheCustomPage()
    {
        // Covers the real app's known limitation: Umbraco's own built-in "no content" handler
        // pre-empts UseStatusCodePagesWithReExecute for unmatched public-site routes (it writes a
        // full response body before the re-execute condition is ever checked - see DEVLOG.md) so
        // the custom page is only guaranteed to render via a direct link to /error/404, not via the
        // automatic fallback there. This test is what keeps that direct path itself proven working.
        using var server = await CreateServerAsync(addProductionErrorHandler: false);
        var response = await server.CreateClient().GetAsync("/error/404");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page Not Found", await response.Content.ReadAsStringAsync());
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
