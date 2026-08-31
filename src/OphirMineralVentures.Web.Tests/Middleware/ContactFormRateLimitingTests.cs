using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using OphirMineralVentures.Web.RateLimiting;

namespace OphirMineralVentures.Web.Tests.Middleware;

public class ContactFormRateLimitingTests
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
                        app.UseMiddleware<ContactFormRateLimitMiddleware>();
                        app.Run(context => context.Response.WriteAsync("ok"));
                    });
            })
            .StartAsync();

        return host.GetTestServer();
    }

    // Umbraco's Html.BeginUmbracoForm posts back to whatever page the form is rendered on (with an
    // encrypted ufprt field carrying the real controller/action route data), not a fixed URL — and
    // that page URL varies per culture (/contact/ vs /zh-hans/zh-contact/). So the middleware can't
    // key off a fixed path; it has to recognize the submission by a form field it controls instead.
    private static HttpRequestMessage SubmitRequest(string path = "/contact/") =>
        new(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["HoneypotField"] = "",
                ["CompanyName"] = "Acme",
            }),
        };

    [Fact]
    public async Task RequestsWithinThreshold_AllSucceed()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        for (var i = 0; i < ContactFormRateLimitMiddleware.PermitLimit; i++)
        {
            var response = await client.SendAsync(SubmitRequest());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task RequestBeyondThreshold_IsThrottled()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        for (var i = 0; i < ContactFormRateLimitMiddleware.PermitLimit; i++)
        {
            await client.SendAsync(SubmitRequest());
        }

        var response = await client.SendAsync(SubmitRequest());

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task ThrottleAppliesRegardlessOfWhichCulturePageTheFormWasOn()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        // Same visitor (same partition/IP on the TestServer client), submitting from the English page
        // some times and the Chinese page others — the threshold should still be shared, not per-path.
        for (var i = 0; i < ContactFormRateLimitMiddleware.PermitLimit; i++)
        {
            var path = i % 2 == 0 ? "/contact/" : "/zh-hans/zh-contact/";
            await client.SendAsync(SubmitRequest(path));
        }

        var response = await client.SendAsync(SubmitRequest("/zh-hans/zh-contact/"));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task RequestsWithoutTheContactFormFields_AreNeverThrottled()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        for (var i = 0; i < ContactFormRateLimitMiddleware.PermitLimit + 5; i++)
        {
            var response = await client.GetAsync("/some-other-page/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
