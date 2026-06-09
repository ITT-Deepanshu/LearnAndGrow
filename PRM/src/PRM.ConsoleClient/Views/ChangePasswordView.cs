using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views;

public sealed class ChangePasswordView(ApiClient api, ConsoleUi ui, SessionContext session)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (session.ForcePasswordChange)
        {
            ui.ClearScreen();
            ui.DrawBox(
                "CHANGE PASSWORD",
                "You must set a new password to continue.");

            var newPassword = ui.Prompt("New Password", secret: true);
            var confirmPassword = ui.Prompt("Confirm Password", secret: true);
            ui.DrawDivider();
            Console.WriteLine("[S] Save and Continue");
            Console.WriteLine();
            var action = ui.Prompt("Action").ToUpperInvariant();

            if (action != "S")
                continue;

            try
            {
                await api.ChangePasswordAsync(string.Empty, newPassword, confirmPassword, ct);
                session.ForcePasswordChange = false;
                ui.WriteSuccess("Password updated. Welcome!");
                ui.Pause();
                return;
            }
            catch (ApiException ex)
            {
                ui.WriteError(ex.Message);
                ui.Pause();
            }
        }
    }
}
