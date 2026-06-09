namespace PRM.Application.Interfaces.Common;

public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
