using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LinkShortener.IntegrationTests;

public sealed class LinkEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LinkEndpointTests()
    {
        var uniqueDb = Path.Combine(Path.GetTempPath(), $"linkshortener-tests-{Guid.NewGuid():N}.db");
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = $"Data Source={uniqueDb}"
                    });
                });
            });
    }

    [Fact]
    public async Task PostCreateLink_Returns201WithCreatedLink()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var code = UniqueCode("CRT");
        var response = await client.PostAsJsonAsync("/api/links", new
        {
            destinationUrl = "https://example.com/test",
            customCode = code,
            expiresAt = DateTimeOffset.UtcNow.AddDays(1).ToString("O")
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.TryGetProperty("code", out var createdCode));
        Assert.Equal(code.ToUpperInvariant(), createdCode.GetString());
    }

    [Fact]
    public async Task RedirectLink_WhenActive_Returns302()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var code = UniqueCode("RDR");
        var creation = await client.PostAsJsonAsync("/api/links", new
        {
            destinationUrl = "https://example.com/redirect",
            customCode = code
        });

        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);

        var created = await creation.Content.ReadFromJsonAsync<JsonElement>();
        var createdCode = created.GetProperty("code").GetString();

        var response = await client.GetAsync($"/{createdCode}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://example.com/redirect", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetLinkStats_WhenLinkExists_ReturnsStats()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var code = UniqueCode("STS");
        var creation = await client.PostAsJsonAsync("/api/links", new
        {
            destinationUrl = "https://example.com/stats",
            customCode = code
        });

        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);

        var response = await client.GetAsync($"/api/links/{code}/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, payload.GetProperty("code").GetString());
        Assert.Equal(0, payload.GetProperty("accessCount").GetInt64());
    }

    private static string UniqueCode(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{prefix}{suffix}".ToUpperInvariant();
    }
}
