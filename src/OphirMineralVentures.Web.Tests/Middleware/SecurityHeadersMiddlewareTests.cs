using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OphirMineralVentures.Web.Middleware;

namespace OphirMineralVentures.Web.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private static async Task<TestServer> CreateServerAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(app =>
                    {
                        app.UseMiddleware<SecurityHeadersMiddleware>();
                        app.Run(context => context.Response.WriteAsync("ok"));
                    });
            })
            .StartAsync();

        return host.GetTestServer();
    }

    [Fact]
    public async Task Response_IncludesXFrameOptions_Deny()
    {
        using var server = await CreateServerAsync();
        var response = await server.CreateClient().GetAsync("/");

        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Response_IncludesReferrerPolicy_StrictOriginWhenCrossOrigin()
    {
        using var server = await CreateServerAsync();
        var response = await server.CreateClient().GetAsync("/");

        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task Response_IncludesPermissionsPolicy_DenyingGeoMicCamera()
    {
        using var server = await CreateServerAsync();
        var response = await server.CreateClient().GetAsync("/");

        Assert.Equal("geolocation=(), microphone=(), camera=()", response.Headers.GetValues("Permissions-Policy").Single());
    }

    [Fact]
    public async Task Response_IncludesContentSecurityPolicy_WithExactValue()
    {
        using var server = await CreateServerAsync();
        var response = await server.CreateClient().GetAsync("/");

        const string expected = "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
            "script-src 'self'; frame-ancestors 'none'";
        Assert.Equal(expected, response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task BackofficeRequests_DoNotGetThePublicSiteCsp()
    {
        // The public CSP blocks the backoffice's own inline bootstrap script and ES module imports
        // (confirmed live in-browser — the backoffice renders a blank page under it). The backoffice
        // is an authenticated admin SPA with its own security model, not the anonymous-visitor surface
        // this CSP exists to protect — CLAUDE.md §7's "loosen only for a specific, understood reason."
        using var server = await CreateServerAsync();
        var response = await server.CreateClient().GetAsync("/umbraco/");

        Assert.False(response.Headers.Contains("Content-Security-Policy"));
    }
}
