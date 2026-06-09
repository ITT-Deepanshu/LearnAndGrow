using PRM.Domain.Entities;

namespace PRM.Domain.Factories;

public static class AllocationFactory
{
    public static Allocation Create(
        long employeeId,
        long projectId,
        decimal utilisationPercentage,
        DateOnly fromDate,
        DateOnly toDate,
        long actorId,
        DateTime utcNow) =>
        Allocation.Create(employeeId, projectId, utilisationPercentage, fromDate, toDate, actorId, utcNow);
}
