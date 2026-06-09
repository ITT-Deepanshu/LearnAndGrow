using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views;

public sealed class LoginView(ApiClient api, ConsoleUi ui, SessionContext session)
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

        var username = ui.Prompt("Username");
        var password = ui.Prompt("Password", secret: true);

        try
        {
            var result = await api.LoginAsync(username, password, ct);
            var me = await api.GetMeAsync(ct);
            session.UserId = me.Id;
            session.ForcePasswordChange = result.ForcePasswordChange || me.ForcePasswordChange;

            if (session.ForcePasswordChange)
            {
                var changePassword = new ChangePasswordView(api, ui, session);
                await changePassword.RunAsync(ct);
            }

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
    }
}
