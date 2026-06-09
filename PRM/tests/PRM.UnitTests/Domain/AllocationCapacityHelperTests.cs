using FluentAssertions;
using PRM.Domain.Helpers;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Domain;

public class AllocationCapacityHelperTests
{
    [Fact]
    public void CalculateTotalUtilisation_SumsOverlappingAllocations()
    {
        var allocations = new[]
        {
            TestFixtures.CreateAllocation(1, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30), 1),
            TestFixtures.CreateAllocation(1, 202, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 7, 31), 2)
        };

        var total = AllocationCapacityHelper.CalculateTotalUtilisation(
            allocations,
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 14));

        total.Should().Be(100);
    }

    [Fact]
    public void WouldExceedCapacity_ReturnsTrueWhenOver100()
    {
        var allocations = new[]
        {
            TestFixtures.CreateAllocation(1, 201, 80, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))
        };

        AllocationCapacityHelper.WouldExceedCapacity(
            allocations,
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 14),
            30).Should().BeTrue();
    }

    [Fact]
    public void WouldExceedCapacity_ReturnsFalseWhenWithinLimit()
    {
        var allocations = new[]
        {
            TestFixtures.CreateAllocation(1, 201, 50, new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30))
        };

        AllocationCapacityHelper.WouldExceedCapacity(
            allocations,
            new DateOnly(2026, 5, 14),
            new DateOnly(2026, 5, 14),
            50).Should().BeFalse();
    }
}
