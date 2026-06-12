using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.ConsoleClient.Helpers;

internal static class ProjectListHelper
{
    public static long? PromptProjectId(
        IReadOnlyList<ProjectListItem> projects,
        ConsoleUi ui,
        string label = "Enter project ID or name")
    {
        var input = ui.Prompt(label);
        return ResolveProjectId(projects, input, ui);
    }

    public static long? ResolveProjectId(
        IReadOnlyList<ProjectListItem> projects,
        string input,
        ConsoleUi ui)
    {
        input = input.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            ui.WriteError("Project not found.");
            return null;
        }

        if (long.TryParse(input, out var id))
        {
            if (projects.Any(p => p.Id == id))
                return id;

            ui.WriteError("Project not found.");
            return null;
        }

        var match = projects.FirstOrDefault(p => p.Name.Equals(input, StringComparison.OrdinalIgnoreCase))
            ?? projects.FirstOrDefault(p => p.Name.Contains(input, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            ui.WriteError("Project not found.");
            return null;
        }

        return match.Id;
    }
}
