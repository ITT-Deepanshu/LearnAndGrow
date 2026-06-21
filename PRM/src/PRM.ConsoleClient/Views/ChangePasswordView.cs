using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views;

public sealed class ChangePasswordView(AuthApi auth, ConsoleUi ui, SessionContext session)
{
    public async Task<string?> RunAsync(CancellationToken ct = default)
    {
        while (session.RequiresPasswordChange)
        {
            ui.ClearScreen();
            ui.DrawBox(
                "CHANGE PASSWORD",
                "You must set a new password to continue.");

            var newPassword = ui.PromptPassword("New Password");
            var confirmPassword = ui.PromptConfirmPassword(newPassword);
            ui.DrawDivider();
            Console.WriteLine("[S] Save and Continue");
            Console.WriteLine();
            var action = ui.Prompt("Action").ToUpperInvariant();

            if (action != "S")
                continue;

            try
            {
                await auth.ChangePasswordAsync(string.Empty, newPassword, confirmPassword, ct);
                await auth.GetMeAsync(ct);
                ui.WriteSuccess("Password updated. Welcome!");
                ui.Pause();
                return newPassword;
            }
            catch (ApiException ex)
            {
                ui.WriteError(ex.Message);
                ui.Pause();
            }
        }

        return null;
    }
}
