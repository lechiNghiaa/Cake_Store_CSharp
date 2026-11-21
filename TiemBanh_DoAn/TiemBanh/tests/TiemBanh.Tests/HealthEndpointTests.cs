using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TiemBanh.Tests;

// Khánh Trình: kiểm thử smoke HTTP, không phụ thuộc cơ sở dữ liệu.
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task HealthLive_TraVe200()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EndpointKhongTonTai_TraVe404()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/khong-co-duong-dan-nay");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
