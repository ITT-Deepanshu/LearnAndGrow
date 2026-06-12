using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views;

public sealed class LoginView(AuthApi auth, ChangePasswordView changePassword, ConsoleUi ui, SessionContext session)
{
    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox(
                "PROJECT & RESOURCE MANAGEMENT TOOL",
                "Learn & Code — Final Project");

            Console.WriteLine("1. Login");
            Console.WriteLine("2. Exit");
            Console.WriteLine();
            var choice = ui.Prompt("Enter option");

            switch (choice)
            {
                case "1":
                    if (await TryLoginAsync(ct))
                        return true;
                    break;
                case "2":
                    return false;
                default:
                    ui.WriteError("Invalid option.");
                    ui.Pause();
                    break;
            }
        }
    }

    private async Task<bool> TryLoginAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("LOGIN");

        var username = ui.PromptRequired("Username", minLength: 1, maxLength: 64);
        var password = ui.PromptRequired("Password", minLength: 1, maxLength: 128, secret: true);

        try
        {
            var result = await auth.LoginAsync(username, password, ct);
            session.RequiresPasswordChange = result.RequiresPasswordChange;

            if (session.RequiresPasswordChange)
            {
                var newPassword = await changePassword.RunAsync(ct);
                if (string.IsNullOrEmpty(newPassword))
                    return false;

                // ChangePassword already returns a fresh JWT; no second login needed.
            }

            await auth.GetMeAsync(ct);

            ui.WriteSuccess($"Welcome, {session.FullName}!");
            ui.Pause();
            return true;
        }
        catch (ApiException ex)
        {
            ui.WriteError(ex.Message);
            ui.Pause();
            return false;
        }
        catch (HttpRequestException)
        {
            ui.WriteError("Unable to reach the API. Make sure the server is running.");
            ui.Pause();
            return false;
        }
        catch (Exception)
        {
            ui.WriteError("Login failed. Please check your username and password.");
            ui.Pause();
            return false;
        }
    }
}
