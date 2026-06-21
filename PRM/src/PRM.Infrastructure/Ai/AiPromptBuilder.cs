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
        builder.AppendLine("You do NOT have database access. Use ONLY the employee lists provided below.");
        builder.AppendLine();
        builder.AppendLine("Tasks:");
        builder.AppendLine("1. Parse the manager request into concrete roles (title, count, required skills with min proficiency).");
        builder.AppendLine("2. Assign the best matching BENCH employee to each role slot in one pass. Never assign the same employeeId twice.");
        builder.AppendLine("3. Report unfilled slots honestly with one of two reason types:");
        builder.AppendLine("   - Skill gap: nobody has the required skill (suggest hire or train).");
        builder.AppendLine("   - Allocated elsewhere: someone has the skill but is booked on other projects until a date (plan around availability).");
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
                {"roleTitle":"<title>","slotNumber":<int>,"employeeId":<id from bench list>,"why":"<short reason>"}
              ],
              "unfilled": [
                {"roleTitle":"<title>","unfilledCount":<int>,"reason":"Skill gap|Allocated elsewhere","detail":"<specific explanation>"}
              ]
            }
            """);
        builder.AppendLine("Never invent employees or skills. Assign only from the bench list.");
        builder.AppendLine();
        builder.AppendLine($"Manager request: \"{request.RequirementText}\"");
        builder.AppendLine();
        builder.AppendLine("Bench employees (assign from this pool only):");
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

        builder.AppendLine();
        builder.AppendLine("Currently allocated employees (for gap analysis only — do NOT assign from this list):");
        if (request.AllocatedEmployees.Count == 0)
        {
            builder.AppendLine("(none)");
        }
        else
        {
            foreach (var employee in request.AllocatedEmployees)
            {
                builder.AppendLine(
                    $"- id={employee.ResourceProfileId}, name={employee.Name}, skills=[{string.Join(", ", employee.Skills)}], " +
                    $"allocations=[{string.Join("; ", employee.ActiveAllocations)}]");
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
