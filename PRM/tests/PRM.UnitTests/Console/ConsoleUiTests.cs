using FluentAssertions;
using PRM.ConsoleClient.Services;

namespace PRM.UnitTests.Console;

public class ConsoleUiTests
{
    private readonly ConsoleUi _ui = new();

    [Fact]
    public void GetLastMonday_ReturnsMondayForWednesday()
    {
        var wednesday = new DateOnly(2026, 6, 10);
        _ui.GetLastMonday(wednesday).Should().Be(new DateOnly(2026, 6, 8));
    }

    [Fact]
    public void FormatDate_UsesDdMmYyyy()
    {
        _ui.FormatDate(new DateOnly(2026, 6, 10)).Should().Be("10-06-2026");
    }

    [Theory]
    [InlineData("GREEN", "🟢 Green")]
    [InlineData("RED", "🔴 Red")]
    [InlineData("ON_TRACK", "🟢 ON TRACK")]
    public void HealthEmoji_MapsKnownStatuses(string input, string expected)
    {
        _ui.HealthEmoji(input).Should().Be(expected);
    }
}
