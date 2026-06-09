using System.Text;
using PRM.Application.Interfaces.Ai;

namespace PRM.Infrastructure.Ai;

internal static class AiPromptBuilder
{
    public static string BuildSkillMatchPrompt(SkillMatchAiRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are an assistant that ranks IT employees for a project requirement.");
        builder.AppendLine("Use only the provided data. Return JSON:");
        builder.AppendLine("""{"candidates":[{"employeeId":<int>,"reason":"<max 30 words>","suggestedUtilisation":<number?>}]}""");
        builder.AppendLine("Order by best fit first. Never invent skills or employees.");
        builder.AppendLine();
        builder.AppendLine($"Project: {request.ProjectName}");
        builder.AppendLine($"Manager wrote: \"{request.RequirementText}\"");
        builder.AppendLine("Candidates:");
        foreach (var candidate in request.Candidates)
        {
            builder.AppendLine(
                $"- id={candidate.EmployeeId}, name={candidate.Name}, dept={candidate.Department}, " +
                $"free={candidate.FreeUtilisation}% ({candidate.FreeHoursPerWeek} hrs/wk), " +
                $"skills=[{string.Join("; ", candidate.Skills)}], " +
                $"recent tags=[{string.Join(", ", candidate.RecentActivityTags)}]");
        }

        return builder.ToString();
    }

    public static string BuildRiskSummaryPrompt(RiskSummaryAiRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are a project risk analyst. Write a 60-180 word plain-English summary.");
        builder.AppendLine("Return JSON: {\"paragraph\":\"<summary>\"}");
        builder.AppendLine("Use only the provided data. Be factual and concise.");
        builder.AppendLine();
        builder.AppendLine($"Project: {request.ProjectName}");
        builder.AppendLine("Milestones:");
        foreach (var milestone in request.Milestones)
            builder.AppendLine($"- {milestone.Title}: due {milestone.DueDate:yyyy-MM-dd}, status {milestone.Status}");

        builder.AppendLine("Allocated people:");
        foreach (var person in request.AllocatedPeople)
            builder.AppendLine($"- {person}");

        builder.AppendLine("Recent effort (last completed week):");
        foreach (var effort in request.RecentEffort)
            builder.AppendLine($"- {effort.EmployeeName}: logged {effort.LoggedHours} hrs (expected {effort.ExpectedHours} hrs)");

        return builder.ToString();
    }
}
