using PRM.Domain.Enums;

namespace PRM.Domain.ValueObjects;

public sealed class ProjectHealthResult
{
    public HealthStatus Status { get; }
    public IReadOnlyList<string> RiskFlags { get; }

    private ProjectHealthResult(HealthStatus status, IReadOnlyList<string> riskFlags)
    {
        Status = status;
        RiskFlags = riskFlags;
    }

    public static ProjectHealthResult Create(HealthStatus status, IEnumerable<string> riskFlags) =>
        new(status, riskFlags.ToList());

    public string DisplayLabel => Status switch
    {
        HealthStatus.Green => "ON TRACK",
        HealthStatus.Yellow => "ATTENTION",
        HealthStatus.Red => "AT RISK",
        _ => "UNKNOWN"
    };
}
