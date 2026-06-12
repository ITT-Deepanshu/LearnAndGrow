using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Admin;

public sealed class ManageEmployeesView(EmployeesApi employees, AllocationsApi allocations, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("MANAGE RESOURCES");
            Console.WriteLine("1. View All resources");
            Console.WriteLine("2. Update resourceProfile");
            Console.WriteLine("3. Deactivate resourceProfile");
            Console.WriteLine("4. Manage resourceProfile Skills");
            Console.WriteLine("5. Assign Manager");
            Console.WriteLine("6. Back");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await ViewAllEmployeesAsync(ct); break;
                case "2": await UpdateEmployeeAsync(ct); break;
                case "3": await DeactivateEmployeeAsync(ct); break;
                case "4": await ManageSkillsAsync(ct); break;
                case "5": await AssignManagerAsync(ct); break;
                case "6": return;
                default: ui.WriteError("Invalid option."); ui.Pause(); break;
            }
        }
    }

    private async Task ViewAllEmployeesAsync(CancellationToken ct)
    {
        int? statusFilter = null;
        string? departmentFilter = null;

        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("ALL RESOURCES");

            try
            {
                var employeeList = await employees.ListAsync(statusFilter, departmentFilter, ct);
                ui.PrintTable(
                    ["ID", "Name", "Department", "Status"],
                    employeeList.Select(e => new List<string>
                    {
                        e.Id.ToString(), e.FullName, e.Department, e.Status
                    }));

                var allocated = employeeList.Count(e => e.Status.Equals("Allocated", StringComparison.OrdinalIgnoreCase));
                var bench = employeeList.Count(e => e.Status.Equals("Bench", StringComparison.OrdinalIgnoreCase));
                var inactive = employeeList.Count(e => e.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase));
                Console.WriteLine();
                Console.WriteLine($"Total: {employeeList.Count}   |   Allocated: {allocated}   |   Bench: {bench}   |   Inactive: {inactive}");
                ui.DrawDivider();
                Console.WriteLine("[F] Filter by Status / Department     [R] Reactivate resource     [B] Back");
                var action = ui.Prompt("Action").ToUpperInvariant();
                if (action == "B") return;
                if (action == "R")
                    await ReactivateEmployeeAsync(employeeList, ct);
                if (action == "F")
                {
                    Console.WriteLine("Status: (1) Bench  (2) PartiallyAllocated  (3) Allocated  (4) Inactive  [Enter] Clear");
                    var statusInput = ui.PromptOptional("Status filter");
                    statusFilter = statusInput switch
                    {
                        "1" => 1, "2" => 2, "3" => 3, "4" => 4, "" => null, _ => statusFilter
                    };
                    departmentFilter = ui.PromptOptional("Department filter (blank to clear)", departmentFilter ?? "");
                    if (string.IsNullOrWhiteSpace(departmentFilter)) departmentFilter = null;
                }
            }
            catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); return; }
        }
    }

    private async Task ReactivateEmployeeAsync(IReadOnlyList<Models.EmployeeListItem> employeeList, CancellationToken ct)
    {
        var id = ui.PromptLong("Enter resourceProfile ID to reactivate");
        var employee = employeeList.FirstOrDefault(e => e.Id == id);
        if (employee is null) { ui.WriteError("Resource not found."); ui.Pause(); return; }
        if (!employee.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
        {
            ui.WriteError("Resource is not inactive.");
            ui.Pause();
            return;
        }

        Console.WriteLine($"Resource: {employee.FullName} ({employee.Department}) — currently Inactive");
        if (!ui.Confirm("Reactivate this resource? Their login account will also be restored.")) return;

        try
        {
            await employees.ReactivateAsync(id, ct);
            ui.WriteSuccess($"Resource reactivated. {employee.FullName} can log in again.");
            Console.WriteLine("Note: Previous allocations are NOT restored. Re-allocate manually if needed.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task UpdateEmployeeAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("UPDATE resourceProfile");
        var id = ui.PromptLong("Enter resourceProfile ID");

        try
        {
            var resourceProfile = await employees.GetAsync(id, ct);
            ui.DrawSection(resourceProfile.FullName);
            Console.WriteLine($"Department  : {resourceProfile.Department}");
            Console.WriteLine($"Designation : {resourceProfile.Designation}");
            Console.WriteLine();

            var department = ui.PromptOptional("New Department", resourceProfile.Department);
            var designation = ui.PromptOptional("New Designation", resourceProfile.Designation);
            ui.DrawDivider();
            if (ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant() != "S") return;

            await employees.UpdateAsync(id, department, designation, ct);
            ui.WriteSuccess("resourceProfile updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task DeactivateEmployeeAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("DEACTIVATE resourceProfile");
        var id = ui.PromptLong("Enter resourceProfile ID");

        try
        {
            var resourceProfile = await employees.GetAsync(id, ct);
            var allocationList = await allocations.ListAsync(employeeId: id, projectId: null, ct);
            var active = allocationList.Where(a => a.ToDate >= DateOnly.FromDateTime(DateTime.Today)).ToList();

            ui.DrawSection(resourceProfile.FullName);
            Console.WriteLine($"Department : {resourceProfile.Department}");
            Console.WriteLine($"Status     : {resourceProfile.Status}");
            Console.WriteLine();

            if (active.Count > 0)
            {
                ui.WriteWarning($"This resourceProfile has {active.Count} active allocation(s).");
                Console.WriteLine("   Ending their employment will remove them from:");
                foreach (var a in active)
                    Console.WriteLine($"     - {a.ProjectName}  ({a.UtilisationPercentage}%,  ends {ui.FormatDate(a.ToDate)})");
                Console.WriteLine();
            }

            Console.WriteLine($"Are you sure you want to deactivate {resourceProfile.FullName}?");
            Console.WriteLine("This will: set is_active = false, end all active allocations today,");
            Console.WriteLine("and block their login account.");
            if (!ui.Confirm("[Y] Yes, Deactivate")) return;

            await employees.DeactivateAsync(id, ct);
            ui.WriteSuccess("resourceProfile deactivated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task ManageSkillsAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("MANAGE SKILLS");
        var id = ui.PromptLong("Enter resourceProfile ID");

        try
        {
            while (true)
            {
                var resourceProfile = await employees.GetAsync(id, ct);
                ui.ClearScreen();
                ui.DrawBox("MANAGE SKILLS");
                ui.DrawSection(resourceProfile.FullName);
                Console.WriteLine("Current Skills:");
                for (var i = 0; i < resourceProfile.Skills.Count; i++)
                {
                    var skill = resourceProfile.Skills[i];
                    Console.WriteLine($"  {i + 1}.  {skill.Name,-18} {skill.Proficiency}");
                }
                ui.DrawDivider();
                Console.WriteLine("1. Add Skill");
                Console.WriteLine("2. Update Proficiency Level");
                Console.WriteLine("3. Remove Skill");
                Console.WriteLine("4. Back");
                Console.WriteLine();

                switch (ui.Prompt("Enter option"))
                {
                    case "1":
                        await AddSkillAsync(id, ct);
                        break;
                    case "2":
                        await UpdateSkillAsync(id, resourceProfile, ct);
                        break;
                    case "3":
                        await RemoveSkillAsync(id, resourceProfile, ct);
                        break;
                    case "4":
                        return;
                }
            }
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task AddSkillAsync(long employeeId, CancellationToken ct)
    {
        var name = ui.Prompt("Skill Name");
        Console.WriteLine("Category: (1) Backend  (2) Frontend  (3) DevOps  (4) QA  (5) Other");
        var category = ui.PromptInt("Enter choice", 1, 5);
        Console.WriteLine("Proficiency Level: (1) Beginner  (2) Intermediate  (3) Advanced");
        var proficiency = ui.PromptInt("Enter choice", 1, 3);

        try
        {
            await employees.AddSkillAsync(employeeId, name, category, proficiency, ct);
            ui.WriteSuccess("Skill added.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task UpdateSkillAsync(long employeeId, Models.EmployeeDetail resourceProfile, CancellationToken ct)
    {
        if (resourceProfile.Skills.Count == 0) { ui.WriteError("No skills to update."); ui.Pause(); return; }
        var index = ui.PromptInt("Enter skill #", 1, resourceProfile.Skills.Count) - 1;
        Console.WriteLine("Proficiency Level: (1) Beginner  (2) Intermediate  (3) Advanced");
        var proficiency = ui.PromptInt("Enter choice", 1, 3);
        try
        {
            await employees.UpdateSkillProficiencyAsync(employeeId, resourceProfile.Skills[index].Id, proficiency, ct);
            ui.WriteSuccess("Proficiency updated.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task RemoveSkillAsync(long employeeId, Models.EmployeeDetail resourceProfile, CancellationToken ct)
    {
        if (resourceProfile.Skills.Count == 0) { ui.WriteError("No skills to remove."); ui.Pause(); return; }
        var index = ui.PromptInt("Enter skill #", 1, resourceProfile.Skills.Count) - 1;
        if (!ui.Confirm($"Remove {resourceProfile.Skills[index].Name}?")) return;
        try
        {
            await employees.RemoveSkillAsync(employeeId, resourceProfile.Skills[index].Id, ct);
            ui.WriteSuccess("Skill removed.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task AssignManagerAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("ASSIGN MANAGER");

        var employeeUserId = ui.PromptLong("resourceProfile User ID");
        var managerUserId = ui.PromptLong("Manager User ID");
        ui.DrawDivider();
        if (ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant() != "S") return;

        try
        {
            var employeeList = await employees.ListAsync(ct: ct);
            var resourceProfile = employeeList.FirstOrDefault(e => e.UserId == employeeUserId);
            if (resourceProfile is null) { ui.WriteError("Resource profile not found for the given User ID."); ui.Pause(); return; }

            await employees.AssignManagerAsync(resourceProfile.Id, managerUserId, ct);
            ui.WriteSuccess("Manager assigned.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }
}
