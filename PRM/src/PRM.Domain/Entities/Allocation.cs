using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class Allocation : AuditableEntity
{
    private Allocation() { }

    public long ResourceProfileId { get; private set; }
    public long ProjectId { get; private set; }
    public decimal UtilisationPercentage { get; private set; }
    public DateOnly FromDate { get; private set; }
    public DateOnly ToDate { get; private set; }
    public DateOnly? EndedAt { get; private set; }

    public ResourceProfile ResourceProfile { get; private set; } = null!;
    public Project Project { get; private set; } = null!;

    public bool IsActiveOn(DateOnly date) =>
        EndedAt is null && date >= FromDate && date <= ToDate;

    public bool OverlapsWith(DateOnly from, DateOnly to) =>
        EndedAt is null && FromDate <= to && ToDate >= from;

    public static Allocation Create(
        long resourceProfileId,
        long projectId,
        decimal utilisationPercentage,
        DateOnly fromDate,
        DateOnly toDate,
        long actorId,
        DateTime utcNow)
    {
        if (utilisationPercentage <= 0 || utilisationPercentage > 100)
            throw new ValidationException("Utilisation must be between 0 and 100 percent.");
        if (fromDate > toDate)
            throw new ValidationException("From date must be before or equal to to date.");

        var allocation = new Allocation
        {
            ResourceProfileId = resourceProfileId,
            ProjectId = projectId,
            UtilisationPercentage = utilisationPercentage,
            FromDate = fromDate,
            ToDate = toDate
        };
        allocation.SetCreated(actorId, utcNow);
        return allocation;
    }

    public void End(DateOnly endDate, long actorId, DateTime utcNow)
    {
        if (EndedAt is not null)
            throw new ConflictException("Allocation has already been ended.");

        if (endDate < FromDate)
            throw new BusinessRuleException("End date cannot be before allocation start date.");

        EndedAt = endDate;
        if (endDate < ToDate)
            ToDate = endDate;
        SetModified(actorId, utcNow);
    }
}
