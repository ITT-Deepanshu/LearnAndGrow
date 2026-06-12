using FluentAssertions;
using PRM.ConsoleClient.Helpers;
using PRM.ConsoleClient.Models;
using PRM.ConsoleClient.Services;

namespace PRM.UnitTests.Console;

public class ProjectListHelperTests
{
    private static readonly IReadOnlyList<ProjectListItem> Projects =
    [
        new(1, "NGAT", "Sameera", new DateOnly(2026, 6, 30), "Active", "Green"),
        new(3, "PRM", "Sameera", new DateOnly(2026, 6, 13), "OnHold", "Yellow")
    ];

    [Fact]
    public void ResolveProjectId_ByNumericId()
    {
        var ui = new ConsoleUi();
        ProjectListHelper.ResolveProjectId(Projects, "1", ui).Should().Be(1);
    }

    [Fact]
    public void ResolveProjectId_ByExactNameCaseInsensitive()
    {
        var ui = new ConsoleUi();
        ProjectListHelper.ResolveProjectId(Projects, "ngat", ui).Should().Be(1);
    }

    [Fact]
    public void ResolveProjectId_ByPartialName()
    {
        var ui = new ConsoleUi();
        ProjectListHelper.ResolveProjectId(Projects, "PR", ui).Should().Be(3);
    }

    [Fact]
    public void ResolveProjectId_ReturnsNullWhenNotFound()
    {
        var ui = new ConsoleUi();
        ProjectListHelper.ResolveProjectId(Projects, "missing", ui).Should().BeNull();
    }
}
