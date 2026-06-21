using FluentAssertions;
using PRM.Infrastructure.Auth;

namespace PRM.UnitTests.Infrastructure;

public class BCryptPasswordHasherTests
{
    [Fact]
    public void HashAndVerify_RoundTripsValidPassword()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("SecurePass1");

        hasher.Verify("SecurePass1", hash).Should().BeTrue();
        hasher.Verify("WrongPass1", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_ReturnsFalseForBlankHash()
    {
        new BCryptPasswordHasher().Verify("SecurePass1", "").Should().BeFalse();
    }
}
