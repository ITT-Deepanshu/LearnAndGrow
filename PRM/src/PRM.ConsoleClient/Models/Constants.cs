namespace PRM.ConsoleClient.Models;

public static class Constants
{
    public const string ApiUrlEnvVar = "PRM_API_URL";
    public const string DefaultApiUrl = "https://localhost:7088";
    public const int DefaultMaxWeeklyHours = 40;

    public static readonly IReadOnlyList<(int Id, string Name, bool IsCustom)> ActivityTags =
    [
        (1, "Backend API Development", false),
        (2, "Microservices / Architecture", false),
        (3, "Database Design & Queries", false),
        (4, "WebSocket / Real-time Features", false),
        (5, "Frontend Development", false),
        (6, "Code Review / Mentoring", false),
        (7, "Bug Fixing", false),
        (8, "DevOps / Deployment", false),
        (9, "Testing & QA", false),
        (10, "Documentation", false),
        (11, "Other", true)
    ];
}
