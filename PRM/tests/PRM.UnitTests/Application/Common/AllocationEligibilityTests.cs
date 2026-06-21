using FluentAssertions;
using PRM.Application.Common;
using PRM.Domain.Enums;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Common;

public class AllocationEligibilityTests
{
    [Fact]
    public void IsEligibleActiveResource_ReturnsTrueForActiveResourceRole()
    {
        var profile = TestFixtures.CreateResourceProfile(userId: 2, id: 10, status: ResourceProfileStatus.Bench);
        AllocationEligibility.IsEligibleActiveResource(profile).Should().BeTrue();
    }

    [Fact]
    public void IsEligibleActiveResource_ReturnsFalseForManagerRole()
    {
        var profile = TestFixtures.CreateResourceProfile(userId: 3, id: 11);
        TestFixtures.SetProperty(profile, "User", TestFixtures.CreateUser(UserRole.Manager, id: 3));
        AllocationEligibility.IsEligibleActiveResource(profile).Should().BeFalse();
    }

    [Fact]
    public void IsEligibleActiveResource_ReturnsFalseForInactiveUser()
    {
        var profile = TestFixtures.CreateResourceProfile(userId: 4, id: 12);
        TestFixtures.SetProperty(profile, "User", TestFixtures.CreateUser(UserRole.Resource, isActive: false, id: 4));
        AllocationEligibility.IsEligibleActiveResource(profile).Should().BeFalse();
    }

    [Fact]
    public void IsEligibleActiveResource_ReturnsFalseForInactiveProfile()
    {
        var profile = TestFixtures.CreateResourceProfile(userId: 5, id: 13);
        profile.Deactivate(1, TestFixtures.FixedUtc);
        AllocationEligibility.IsEligibleActiveResource(profile).Should().BeFalse();
    }
}
