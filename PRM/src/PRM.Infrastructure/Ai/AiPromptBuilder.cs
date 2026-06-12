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
                $"- id={candidate.ResourceProfileId}, name={candidate.Name}, dept={candidate.Department}, " +
                $"free={candidate.FreeUtilisation}% ({candidate.FreeHoursPerWeek} hrs/wk), " +
                $"skills=[{string.Join("; ", candidate.Skills)}], " +
                $"recent tags=[{string.Join(", ", candidate.RecentActivityTags)}]");
        }

        return builder.ToString();
    }

    public static string BuildTeamSkillMatchPrompt(TeamSkillMatchAiRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("You are a team staffing assistant for an IT resource management system.");
        builder.AppendLine("You do NOT have database access. Use ONLY the bench employee list provided below.");
        builder.AppendLine();
        builder.AppendLine("Tasks:");
        builder.AppendLine("1. Parse the manager request into concrete roles (title, count, required skills with min proficiency).");
        builder.AppendLine("2. Assign the best matching bench employee to each role slot. Never assign the same employeeId twice.");
        builder.AppendLine("3. Report any unfilled slots honestly (not enough matching bench, or skill gap).");
        builder.AppendLine();
        builder.AppendLine("Proficiency levels (lowest to highest): Beginner, Intermediate, Advanced.");
        builder.AppendLine("An employee meets a skill requirement when they have that skill at or above the minimum proficiency.");
        builder.AppendLine();
        builder.AppendLine("Return JSON only (no markdown):");
        builder.AppendLine("""
            {
              "teamDefined": [
                {"roleTitle":"<title>","count":<int>,"requiredSkills":[{"name":"<skill>","minProficiency":"Beginner|Intermediate|Advanced"}]}
              ],
              "assignments": [
                {"roleTitle":"<title>","slotNumber":<int>,"employeeId":<id from list>,"why":"<short reason>"}
              ],
              "unfilled": [
                {"roleTitle":"<title>","unfilledCount":<int>,"reason":"<short reason>","detail":"<specific explanation>"}
              ]
            }
            """);
        builder.AppendLine("Never invent employees or skills. employeeId must come from the bench list.");
        builder.AppendLine();
        builder.AppendLine($"Manager request: \"{request.RequirementText}\"");
        builder.AppendLine();
        builder.AppendLine("Bench employees:");
        if (request.BenchEmployees.Count == 0)
        {
            builder.AppendLine("(none on bench)");
        }
        else
        {
            foreach (var employee in request.BenchEmployees)
            {
                builder.AppendLine(
                    $"- id={employee.ResourceProfileId}, name={employee.Name}, manager={employee.ManagerName}, " +
                    $"skills=[{string.Join(", ", employee.Skills)}]");
            }
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
