using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Admin;

public sealed class ManageEmployeesView(ApiClient api, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("MANAGE EMPLOYEES");
            Console.WriteLine("1. View All Employees");
            Console.WriteLine("2. Update Employee");
            Console.WriteLine("3. Deactivate Employee");
            Console.WriteLine("4. Manage Employee Skills");
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
            ui.DrawBox("ALL EMPLOYEES");

            try
            {
                var employees = await api.ListEmployeesAsync(statusFilter, departmentFilter, ct);
                ui.PrintTable(
                    ["ID", "Name", "Department", "Status"],
                    employees.Select(e => new List<string>
                    {
                        e.Id.ToString(), e.FullName, e.Department, e.Status
                    }));

                var allocated = employees.Count(e => e.Status.Equals("Allocated", StringComparison.OrdinalIgnoreCase));
                var bench = employees.Count(e => e.Status.Equals("Bench", StringComparison.OrdinalIgnoreCase));
                Console.WriteLine();
                Console.WriteLine($"Total: {employees.Count}   |   Allocated: {allocated}   |   Bench: {bench}");
                ui.DrawDivider();
                Console.WriteLine("[F] Filter by Status / Department     [B] Back");
                var action = ui.Prompt("Action").ToUpperInvariant();
                if (action == "B") return;
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

    private async Task UpdateEmployeeAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("UPDATE EMPLOYEE");
        var id = ui.PromptLong("Enter Employee ID");

        try
        {
            var employee = await api.GetEmployeeAsync(id, ct);
            ui.DrawSection(employee.FullName);
            Console.WriteLine($"Department  : {employee.Department}");
            Console.WriteLine($"Designation : {employee.Designation}");
            Console.WriteLine();

            var department = ui.PromptOptional("New Department", employee.Department);
            var designation = ui.PromptOptional("New Designation", employee.Designation);
            ui.DrawDivider();
            if (ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant() != "S") return;

            await api.UpdateEmployeeAsync(id, department, designation, ct);
            ui.WriteSuccess("Employee updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task DeactivateEmployeeAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("DEACTIVATE EMPLOYEE");
        var id = ui.PromptLong("Enter Employee ID");

        try
        {
            var employee = await api.GetEmployeeAsync(id, ct);
            var allocations = await api.ListAllocationsAsync(employeeId: id, projectId: null, ct);
            var active = allocations.Where(a => a.ToDate >= DateOnly.FromDateTime(DateTime.Today)).ToList();

            ui.DrawSection(employee.FullName);
            Console.WriteLine($"Department : {employee.Department}");
            Console.WriteLine($"Status     : {employee.Status}");
            Console.WriteLine();

            if (active.Count > 0)
            {
                ui.WriteWarning($"This employee has {active.Count} active allocation(s).");
                Console.WriteLine("   Ending their employment will remove them from:");
                foreach (var a in active)
                    Console.WriteLine($"     - {a.ProjectName}  ({a.UtilisationPercentage}%,  ends {ui.FormatDate(a.ToDate)})");
                Console.WriteLine();
            }

            Console.WriteLine($"Are you sure you want to deactivate {employee.FullName}?");
            Console.WriteLine("This will: set is_active = false, end all active allocations today,");
            Console.WriteLine("and block their login account.");
            if (!ui.Confirm("[Y] Yes, Deactivate")) return;

            await api.DeactivateEmployeeAsync(id, ct);
            ui.WriteSuccess("Employee deactivated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task ManageSkillsAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("MANAGE SKILLS");
        var id = ui.PromptLong("Enter Employee ID");

        try
        {
            while (true)
            {
                var employee = await api.GetEmployeeAsync(id, ct);
                ui.ClearScreen();
                ui.DrawBox("MANAGE SKILLS");
                ui.DrawSection(employee.FullName);
                Console.WriteLine("Current Skills:");
                for (var i = 0; i < employee.Skills.Count; i++)
                {
                    var skill = employee.Skills[i];
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
                        await UpdateSkillAsync(id, employee, ct);
                        break;
                    case "3":
                        await RemoveSkillAsync(id, employee, ct);
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
            await api.AddSkillAsync(employeeId, name, category, proficiency, ct);
            ui.WriteSuccess("Skill added.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task UpdateSkillAsync(long employeeId, Models.EmployeeDetail employee, CancellationToken ct)
    {
        if (employee.Skills.Count == 0) { ui.WriteError("No skills to update."); ui.Pause(); return; }
        var index = ui.PromptInt("Enter skill #", 1, employee.Skills.Count) - 1;
        Console.WriteLine("Proficiency Level: (1) Beginner  (2) Intermediate  (3) Advanced");
        var proficiency = ui.PromptInt("Enter choice", 1, 3);
        try
        {
            await api.UpdateSkillProficiencyAsync(employeeId, employee.Skills[index].Id, proficiency, ct);
            ui.WriteSuccess("Proficiency updated.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task RemoveSkillAsync(long employeeId, Models.EmployeeDetail employee, CancellationToken ct)
    {
        if (employee.Skills.Count == 0) { ui.WriteError("No skills to remove."); ui.Pause(); return; }
        var index = ui.PromptInt("Enter skill #", 1, employee.Skills.Count) - 1;
        if (!ui.Confirm($"Remove {employee.Skills[index].Name}?")) return;
        try
        {
            await api.RemoveSkillAsync(employeeId, employee.Skills[index].Id, ct);
            ui.WriteSuccess("Skill removed.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task AssignManagerAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("ASSIGN MANAGER");

        var employeeUserId = ui.PromptLong("Employee User ID");
        var managerUserId = ui.PromptLong("Manager User ID");
        ui.DrawDivider();
        if (ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant() != "S") return;

        try
        {
            var employees = await api.ListEmployeesAsync(ct: ct);
            var employee = employees.FirstOrDefault(e => e.UserId == employeeUserId);
            if (employee is null) { ui.WriteError("Employee not found for the given User ID."); ui.Pause(); return; }

            await api.AssignManagerAsync(employee.Id, managerUserId, ct);
            ui.WriteSuccess("Manager assigned.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }
}
