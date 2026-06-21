using FluentAssertions;
using PRM.Domain.Helpers;

namespace PRM.UnitTests.Domain;

public class WeekHelperTests
{
    [Theory]
    [InlineData("2026-05-12", "2026-05-11")]
    [InlineData("2026-05-14", "2026-05-11")]
    [InlineData("2026-05-18", "2026-05-18")]
    public void GetMondayOfWeek_ReturnsMonday(string input, string expected)
    {
        var date = DateOnly.Parse(input);
        WeekHelper.GetMondayOfWeek(date).Should().Be(DateOnly.Parse(expected));
    }

    [Fact]
    public void IsMonday_ReturnsTrueForMonday()
    {
        WeekHelper.IsMonday(new DateOnly(2026, 5, 11)).Should().BeTrue();
        WeekHelper.IsMonday(new DateOnly(2026, 5, 12)).Should().BeFalse();
    }

    [Fact]
    public void IsFutureWeek_ReturnsTrueForUpcomingWeek()
    {
        var today = new DateOnly(2026, 5, 14);
        WeekHelper.IsFutureWeek(new DateOnly(2026, 5, 18), today).Should().BeTrue();
        WeekHelper.IsFutureWeek(new DateOnly(2026, 5, 11), today).Should().BeFalse();
    }

    [Fact]
    public void CountWorkingDaysAfter_ReturnsZeroBeforeDeadlinePasses()
    {
        var periodEnd = new DateOnly(2026, 6, 7);
        WeekHelper.CountWorkingDaysAfter(periodEnd, periodEnd).Should().Be(0);
    }

    [Fact]
    public void CountWorkingDaysAfter_CountsWeekdaysAfterPeriodEnd()
    {
        var periodEnd = new DateOnly(2026, 6, 7);
        WeekHelper.CountWorkingDaysAfter(periodEnd, new DateOnly(2026, 6, 10)).Should().Be(3);
    }
}
