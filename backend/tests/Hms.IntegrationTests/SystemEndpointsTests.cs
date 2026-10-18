using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hms.IntegrationTests;

public class SystemEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SystemEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LivenessHealthCheck_ShouldReturn200()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SystemVersionEndpoint_ShouldReturnSystemMetadata()
    {
        var response = await _client.GetAsync("/api/v1/system/version");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        root.GetProperty("product").GetString().Should().Contain("Hospital Management System");
        root.GetProperty("version").GetString().Should().Be("1.0.0");
        root.GetProperty("framework").GetString().Should().Be(".NET 9.0");
        root.GetProperty("serverTimeDhaka").GetString().Should().Contain("UTC+06:00");
    }

    [Fact]
    public async Task ScalarDocsEndpoint_ShouldBeAccessible()
    {
        var response = await _client.GetAsync("/docs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
