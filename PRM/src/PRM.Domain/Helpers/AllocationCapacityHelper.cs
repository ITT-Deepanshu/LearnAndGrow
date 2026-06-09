using PRM.Domain.Entities;

namespace PRM.Domain.Helpers;

public static class AllocationCapacityHelper
{
    public static decimal CalculateTotalUtilisation(
        IEnumerable<Allocation> allocations,
        DateOnly from,
        DateOnly to) =>
        allocations
            .Where(a => a.OverlapsWith(from, to))
            .Sum(a => a.UtilisationPercentage);

    public static bool WouldExceedCapacity(
        IEnumerable<Allocation> existingAllocations,
        DateOnly from,
        DateOnly to,
        decimal newUtilisation) =>
        CalculateTotalUtilisation(existingAllocations, from, to) + newUtilisation > 100;
}
