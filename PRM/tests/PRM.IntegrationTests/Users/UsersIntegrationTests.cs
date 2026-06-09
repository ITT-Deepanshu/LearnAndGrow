using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PRM.Application.Features.Users.Dtos;
using PRM.IntegrationTests.Infrastructure;

namespace PRM.IntegrationTests.Users;

[Collection(IntegrationCollection.Name)]
public sealed class UsersIntegrationTests(PrmWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateUser_AsAdmin_ReturnsCreatedUser()
    {
        await LoginAndPrepareAdminAsync();

        var response = await Client.PostAsJsonAsync("/api/v1/users", new CreateUserDto(
            "Priya Sharma",
            "priya@prm.local",
            "priya.sharma",
            "TempPass1",
            "Manager"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user!.Username.Should().Be("priya.sharma");
        user.Role.Should().Be("MANAGER");
    }

    [Fact]
    public async Task CreateUser_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/users", new CreateUserDto(
            "No Auth",
            "noauth@prm.local",
            "noauth",
            "TempPass1",
            "Employee"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
