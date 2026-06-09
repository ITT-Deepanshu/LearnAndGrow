using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PRM.Application.Features.Auth.Dtos;

namespace PRM.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<PrmWebApplicationFactory>
{
    private const string BootstrapPassword = "Admin@1234";
    private const string UpdatedPassword = "AdminPass1";

    protected IntegrationTestBase(PrmWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected PrmWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; }

    protected async Task<string> LoginAsAdminAsync()
    {
        foreach (var password in new[] { BootstrapPassword, UpdatedPassword })
        {
            var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginDto("admin", password));
            if (login.StatusCode != HttpStatusCode.OK)
                continue;

            var result = await login.Content.ReadFromJsonAsync<LoginResultDto>();
            return result!.AccessToken;
        }

        throw new InvalidOperationException("Unable to authenticate as bootstrap admin.");
    }

    protected async Task<string> LoginAndPrepareAdminAsync(string newPassword = UpdatedPassword)
    {
        var currentPassword = await ResolveCurrentAdminPasswordAsync();
        var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginDto("admin", currentPassword));
        login.EnsureSuccessStatusCode();
        var result = await login.Content.ReadFromJsonAsync<LoginResultDto>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.AccessToken);

        if (!result.ForcePasswordChange && currentPassword == newPassword)
            return result.AccessToken;

        var change = await Client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordDto(
            currentPassword,
            newPassword,
            newPassword));
        change.EnsureSuccessStatusCode();

        var relogin = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginDto("admin", newPassword));
        relogin.EnsureSuccessStatusCode();
        var refreshed = await relogin.Content.ReadFromJsonAsync<LoginResultDto>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed!.AccessToken);
        return refreshed.AccessToken;
    }

    private async Task<string> ResolveCurrentAdminPasswordAsync()
    {
        foreach (var password in new[] { BootstrapPassword, UpdatedPassword })
        {
            var login = await Client.PostAsJsonAsync("/api/v1/auth/login", new LoginDto("admin", password));
            if (login.StatusCode == HttpStatusCode.OK)
                return password;
        }

        throw new InvalidOperationException("Unable to resolve current admin password.");
    }

    protected static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("detail", out var detail)
            ? detail.GetString() ?? json
            : json;
    }
}
