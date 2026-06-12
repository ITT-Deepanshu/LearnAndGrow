using FluentAssertions;
using PRM.Application.Ai;

namespace PRM.UnitTests.Application.Ai;

public class HoursPerWeekParserTests
{
    [Theory]
    [InlineData("about 10 hrs/week for UI testing", 10)]
    [InlineData("Need 20 hours weekly", 20)]
    [InlineData("full-time backend developer for 3 months", null)]
    [InlineData(null, null)]
    public void Parse_ExtractsWeeklyHours(string? input, int? expected)
    {
        HoursPerWeekParser.Parse(input).Should().Be(expected);
    }
}
