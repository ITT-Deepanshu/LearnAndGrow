using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PRM.Application.Features.Auth.Dtos;
using PRM.IntegrationTests.Infrastructure;

namespace PRM.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public sealed class AuthIntegrationTests(PrmWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await Client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsAccessToken()
    {
        var token = await LoginAsAdminAsync();
        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginDto("admin", "wrong-password"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_AfterPasswordChange_ReturnsAdminProfile()
    {
        await LoginAndPrepareAdminAsync();

        var response = await Client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeDto>();
        me!.Username.Should().Be("admin");
        me.Role.Should().Be("ADMIN");
    }
}
