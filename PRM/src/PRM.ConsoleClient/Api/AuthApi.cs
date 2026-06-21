using PRM.ConsoleClient.Models;

using PRM.ConsoleClient.Services;



namespace PRM.ConsoleClient.Api;



public sealed class AuthApi(PrmHttpClient http)

{

    public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct = default)

    {

        var result = await http.PostAsync<LoginRequest, LoginResponse>(

            "api/v1/auth/login", new(username, password), ct, authenticated: false);



        http.Session.SetLogin(new LoginData(
            result.AccessToken,
            result.RequiresPasswordChange,
            result.Role,
            result.FullName,
            null,
            result.ResourceProfileId,
            result.Permissions));

        http.PersistTokens();

        return result;

    }



    public async Task<MeResponse> GetMeAsync(CancellationToken ct = default)

    {

        var me = await http.GetAsync<MeResponse>("api/v1/auth/me", ct);

        http.Session.ApplyMe(new MeData(
            me.Id,
            me.FullName,
            me.Role,
            me.RequiresPasswordChange,
            me.ResourceProfileId,
            me.Permissions));

        http.PersistTokens();

        return me;

    }



    public async Task ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword, CancellationToken ct = default)

    {

        var result = await http.PostAsync<ChangePasswordRequest, LoginResponse>(

            "api/v1/auth/change-password",

            new ChangePasswordRequest(currentPassword, newPassword, confirmPassword),

            ct);



        http.Session.SetLogin(new LoginData(
            result.AccessToken,
            result.RequiresPasswordChange,
            result.Role,
            result.FullName,
            http.Session.UserId,
            result.ResourceProfileId,
            result.Permissions));

        http.PersistTokens();

    }



    public async Task LogoutAsync(CancellationToken ct = default)

    {

        if (http.Session.IsAuthenticated)

        {

            try

            {

                await http.PostAsync("api/v1/auth/logout", new { }, ct);

            }

            catch

            {

                // Best-effort logout.

            }

        }



        http.Session.Clear();

        http.TokenStore.Clear();

    }

}


