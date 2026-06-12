using FluentAssertions;
using PRM.ConsoleClient.Api;

namespace PRM.UnitTests.Console;

public class PrmHttpClientTests
{
    [Fact]
    public void BuildQuery_OmitsNullOrEmptyValues()
    {
        var query = PrmHttpClient.BuildQuery(
            ("status", "1"),
            ("department", null),
            ("name", ""));

        query.Should().Be("?status=1");
    }

    [Fact]
    public void BuildQuery_ReturnsEmptyWhenNoValues()
    {
        PrmHttpClient.BuildQuery(("a", null), ("b", "")).Should().BeEmpty();
    }

    [Fact]
    public void BuildQuery_EncodesSpecialCharacters()
    {
        var query = PrmHttpClient.BuildQuery(("q", "a b"));
        query.Should().Be("?q=a%20b");
    }
}
