using PRM.ConsoleClient.Api;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Views.Admin;

public sealed class ManageProjectsView(ProjectsApi projects, ConsoleUi ui)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ui.ClearScreen();
            ui.DrawBox("MANAGE PROJECTS");
            Console.WriteLine("1. Create Project");
            Console.WriteLine("2. View All Projects");
            Console.WriteLine("3. Update Project Details");
            Console.WriteLine("4. Manage Milestones");
            Console.WriteLine("5. Back");
            Console.WriteLine();

            switch (ui.Prompt("Enter option"))
            {
                case "1": await CreateProjectAsync(ct); break;
                case "2": await ViewAllProjectsAsync(ct); break;
                case "3": await UpdateProjectAsync(ct); break;
                case "4": await ManageMilestonesAsync(ct); break;
                case "5": return;
                default: ui.WriteError("Invalid option."); ui.Pause(); break;
            }
        }
    }

    private async Task CreateProjectAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("CREATE PROJECT");

        var name = ui.PromptRequired("Project Name", minLength: 2, maxLength: 128);
        var description = ui.Prompt("Description");
        var startDate = ui.PromptDate("Start Date (DD-MM-YYYY)");
        var endDate = ui.PromptDate("End Date (DD-MM-YYYY)");
        Console.WriteLine("Status: (1) PLANNED   (2) ACTIVE   (3) ON_HOLD");
        var status = ui.PromptInt("Enter choice", 1, 3);
        var managerId = ui.PromptLong("Assign Manager (Manager ID)");
        var storyPoints = ui.PromptInt("Total Story Points", 0);

        ui.DrawDivider();
        if (ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant() != "S") return;

        try
        {
            await projects.CreateAsync(new CreateProjectRequest(
                name, description, startDate, endDate, status, managerId, storyPoints), ct);
            ui.WriteSuccess("Project created.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        ui.Pause();
    }

    private async Task ViewAllProjectsAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("ALL PROJECTS");

        try
        {
            var projectList = await projects.ListAsync(ct);
            ui.PrintTable(
                ["ID", "Name", "Manager", "End Date", "Status", "Health"],
                projectList.Select(p => new List<string>
                {
                    p.Id.ToString(), p.Name, p.ManagerName, ui.FormatDate(p.EndDate), p.Status, p.Health
                }));
            ui.DrawDivider();
            Console.WriteLine("[B] Back");
            ui.Prompt("Action");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task UpdateProjectAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("UPDATE PROJECT DETAILS");
        var id = ui.PromptLong("Enter Project ID");

        try
        {
            var project = await projects.GetAsync(id, ct);
            ui.DrawSection(project.Name);
            var name = ui.PromptOptional("Project Name", project.Name);
            var description = ui.PromptOptional("Description", project.Description);
            var startDate = ui.PromptDateOptional($"Start Date ({ui.FormatDate(project.StartDate)})") ?? project.StartDate;
            var endDate = ui.PromptDateOptional($"End Date ({ui.FormatDate(project.EndDate)})") ?? project.EndDate;
            Console.WriteLine("Status: (1) PLANNED  (2) ACTIVE  (3) ON_HOLD  (4) COMPLETED");
            var statusInput = ui.PromptOptional("Status choice");
            var status = string.IsNullOrWhiteSpace(statusInput)
                ? StatusToInt(project.Status)
                : int.Parse(statusInput);
            var managerId = ui.PromptOptional("Assign Manager ID", project.ManagerId.ToString());
            var storyPoints = ui.PromptOptional("Total Story Points", project.TotalStoryPoints.ToString());

            ui.DrawDivider();
            if (ui.Prompt("Action [S] Save  [B] Back").ToUpperInvariant() != "S") return;

            await projects.UpdateAsync(id, new UpdateProjectRequest(
                name, description, startDate, endDate, status,
                long.Parse(managerId), int.Parse(storyPoints)), ct);
            ui.WriteSuccess("Project updated.");
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); }
        catch (FormatException) { ui.WriteError("Invalid numeric input."); }
        ui.Pause();
    }

    private async Task ManageMilestonesAsync(CancellationToken ct)
    {
        ui.ClearScreen();
        ui.DrawBox("MILESTONES");
        var projectId = ui.PromptLong("Enter Project ID");

        try
        {
            while (true)
            {
                var project = await projects.GetAsync(projectId, ct);
                var milestones = await projects.ListMilestonesAsync(projectId, ct);
                ui.ClearScreen();
                ui.DrawBox("MILESTONES");
                ui.DrawSection(project.Name);

                ui.PrintTable(
                    ["ID", "Title", "Due Date", "Story Pts", "Status"],
                    milestones.Select(m => new List<string>
                    {
                        m.Id.ToString(), m.Title, ui.FormatDate(m.DueDate), m.StoryPoints.ToString(), m.Status
                    }));

                var completed = milestones.Where(m => m.Status.Equals("Done", StringComparison.OrdinalIgnoreCase)).Sum(m => m.StoryPoints);
                var total = milestones.Sum(m => m.StoryPoints);
                Console.WriteLine();
                Console.WriteLine($"Total: {total} SP   |   Completed: {completed} SP   |   Remaining: {total - completed} SP");
                Console.WriteLine();
                Console.WriteLine("1. Add Milestone");
                Console.WriteLine("2. Update Milestone Status");
                Console.WriteLine("3. Back");

                switch (ui.Prompt("Enter option"))
                {
                    case "1":
                        await AddMilestoneAsync(projectId, ct);
                        break;
                    case "2":
                        await UpdateMilestoneStatusAsync(projectId, milestones, ct);
                        break;
                    case "3":
                        return;
                }
            }
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task AddMilestoneAsync(long projectId, CancellationToken ct)
    {
        var title = ui.Prompt("Milestone Title");
        var dueDate = ui.PromptDate("Due Date (DD-MM-YYYY)");
        var storyPoints = ui.PromptInt("Story Points", 0);
        try
        {
            await projects.AddMilestoneAsync(projectId, new AddMilestoneRequest(title, dueDate, storyPoints), ct);
            ui.WriteSuccess("Milestone added.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private async Task UpdateMilestoneStatusAsync(long projectId, IReadOnlyList<Milestone> milestones, CancellationToken ct)
    {
        if (milestones.Count == 0) { ui.WriteError("No milestones."); ui.Pause(); return; }
        var milestoneId = ui.PromptLong("Enter milestone ID");
        var milestone = milestones.FirstOrDefault(m => m.Id == milestoneId);
        if (milestone is null) { ui.WriteError("Milestone not found."); ui.Pause(); return; }
        Console.WriteLine("New Status: (1) NOT_STARTED  (2) IN_PROGRESS  (3) DONE");
        var status = ui.PromptInt("Enter choice", 1, 3);
        try
        {
            await projects.UpdateMilestoneStatusAsync(projectId, milestone.Id, status, ct);
            ui.WriteSuccess("Milestone updated.");
            ui.Pause();
        }
        catch (ApiException ex) { ui.WriteError(ex.Message); ui.Pause(); }
    }

    private static int StatusToInt(string status) => status.ToUpperInvariant() switch
    {
        "PLANNED" => 1,
        "ACTIVE" => 2,
        "ONHOLD" or "ON_HOLD" => 3,
        "COMPLETED" => 4,
        _ => 1
    };
}
