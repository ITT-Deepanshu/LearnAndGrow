using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Admin;

public sealed class ManageUsersView(UsersApi usersApi, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("MANAGE USERS");
            Console.WriteLine("1. Create User Account");
            Console.WriteLine("2. View All Users");
            Console.WriteLine("3. Reset User Password");
            Console.WriteLine("4. Deactivate User");
            Console.WriteLine("5. Back");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await CreateUserAsync(ct); break;
                case "2": await ViewAllUsersAsync(ct); break;
                case "3": await ResetPasswordAsync(ct); break;
                case "4": await DeactivateUserAsync(ct); break;
                case "5": return;
                default: ui.WriteError("Invalid option."); ui.Pause(); break;
            }
        }
    }

    private async Task CreateUserAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("CREATE USER ACCOUNT");

        var fullName = ui.PromptRequired("Full Name", minLength: 2, maxLength: 128);
        var email = ui.PromptEmail();
        var username = ui.PromptUsername();
        var tempPassword = ui.PromptPassword("Temporary Password");
        var confirmPassword = ui.PromptConfirmPassword(tempPassword, "Confirm Temporary Password");
        Console.WriteLine("Role: (1) Admin  (2) Manager  (3) Employee");
        var roleChoice = ui.PromptInt("Enter choice", 1, 3);
        var role = roleChoice switch { 1 => "admin", 2 => "manager", _ => "resource" };

        ui.DrawDivider();
        var action = ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant();
        if (action == "B") return;

        try
        {
            await usersApi.CreateAsync(new CreateUserRequest(fullName, email, username, tempPassword, role), ct);
            ui.WriteSuccess("Account created. User must change password on first login.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task ViewAllUsersAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("ALL USERS");

        try
        {
            var userList = await usersApi.ListAsync(ct);
            ui.PrintTable(
                ["ID", "Username", "Role", "Status"],
                userList.Select(u => new List<string>
                {
                    u.Id.ToString(),
                    u.Username,
                    u.Role,
                    u.IsActive ? "Active" : "Inactive"
                }));

            Console.WriteLine();
            Console.WriteLine($"Total: {userList.Count}   |   Active: {userList.Count(u => u.IsActive)}   |   Inactive: {userList.Count(u => !u.IsActive)}");
            ui.DrawDivider();
            Console.WriteLine("[R] Reactivate a user     [B] Back");
            var action = ui.Prompt("Action").ToUpperInvariant();

            if (action == "R")
                await ReactivateUserAsync(userList, ct);
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task ReactivateUserAsync(IReadOnlyList<UserListItem> userList, CancellationToken ct)
    {
        var id = ui.PromptLong("Enter User ID to reactivate");
        var user = userList.FirstOrDefault(u => u.Id == id);
        if (user is null) { ui.WriteError("User not found."); ui.Pause(); return; }
        if (user.IsActive) { ui.WriteError("User is already active."); ui.Pause(); return; }

        Console.WriteLine($"User: {user.Username} ({user.Role}) — currently Inactive");
        if (!ui.Confirm("Reactivate this account?")) return;

        try
        {
            await usersApi.ReactivateAsync(id, ct);
            ui.WriteSuccess($"Account reactivated. {user.Username} can now log in.");
            Console.WriteLine("Note: Previous allocations are NOT restored. Admin must re-allocate manually if needed.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task ResetPasswordAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("RESET USER PASSWORD");

        var input = ui.Prompt("Enter Username or User ID");
        try
        {
            var userList = await usersApi.ListAsync(ct);
            var user = long.TryParse(input, out var id)
                ? userList.FirstOrDefault(u => u.Id == id)
                : userList.FirstOrDefault(u => u.Username.Equals(input, StringComparison.OrdinalIgnoreCase));

            if (user is null) { ui.WriteError("User not found."); ui.Pause(); return; }

            var detail = await usersApi.GetAsync(user.Id, ct);
            Console.WriteLine($"User found: {detail.FullName} ({detail.Role})");
            var newPassword = ui.PromptPassword("New Temporary Password");
            var confirmPassword = ui.PromptConfirmPassword(newPassword);
            ui.DrawDivider();
            if (ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant() != "S") return;

            await usersApi.ResetPasswordAsync(user.Id, newPassword, confirmPassword, ct);
            ui.WriteSuccess("Password reset. User will be prompted to change it on next login.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task DeactivateUserAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("DEACTIVATE USER");

        var input = ui.Prompt("Enter Username or User ID");
        try
        {
            var userList = await usersApi.ListAsync(ct);
            var user = long.TryParse(input, out var id)
                ? userList.FirstOrDefault(u => u.Id == id)
                : userList.FirstOrDefault(u => u.Username.Equals(input, StringComparison.OrdinalIgnoreCase));

            if (user is null) { ui.WriteError("User not found."); ui.Pause(); return; }
            if (!user.IsActive) { ui.WriteError("User is already inactive."); ui.Pause(); return; }

            var detail = await usersApi.GetAsync(user.Id, ct);
            Console.WriteLine($"User found: {detail.FullName} ({detail.Role})");
            Console.WriteLine("Status     : Active");
            Console.WriteLine();
            Console.WriteLine("Are you sure you want to deactivate this account?");
            Console.WriteLine("Deactivated users cannot log in. Their data is preserved.");
            if (!ui.Confirm("[Y] Yes, Deactivate")) return;

            await usersApi.DeactivateAsync(user.Id, ct);
            ui.WriteSuccess("User deactivated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }
}
