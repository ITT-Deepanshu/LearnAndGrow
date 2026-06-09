using FluentAssertions;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.Domain.Factories;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Domain;

public class EntityBehaviorTests
{
    [Fact]
    public void User_Create_SetsForcePasswordChange()
    {
        var user = UserFactory.CreateAccount("new.user", "new@prm.local", "New", "hash", UserRole.Employee, 1, TestFixtures.FixedUtc);
        user.ForcePasswordChange.Should().BeTrue();
    }

    [Fact]
    public void Allocation_Create_RejectsInvalidUtilisation()
    {
        var act = () => Allocation.Create(1, 1, 0, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), 1, TestFixtures.FixedUtc);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Allocation_End_SetsEndedAt()
    {
        var allocation = TestFixtures.CreateAllocation(1, 201, 50, new DateOnly(2026, 3, 1), new DateOnly(2026, 6, 30));
        allocation.End(new DateOnly(2026, 5, 14), 1, TestFixtures.FixedUtc);
        allocation.EndedAt.Should().Be(new DateOnly(2026, 5, 14));
    }

    [Fact]
    public void Employee_RecomputeStatus_SetsBenchWhenZeroUtilisation()
    {
        var employee = TestFixtures.CreateEmployee();
        employee.RecomputeStatus(0);
        employee.Status.Should().Be(EmployeeStatus.Bench);
    }

    [Fact]
    public void Employee_RecomputeStatus_SetsPartialWhenBetweenZeroAnd100()
    {
        var employee = TestFixtures.CreateEmployee();
        employee.RecomputeStatus(50);
        employee.Status.Should().Be(EmployeeStatus.PartiallyAllocated);
    }

    [Fact]
    public void Timesheet_Submit_RejectsNonMondayWeek()
    {
        var act = () => Timesheet.Submit(1, new DateOnly(2026, 5, 13), [], 40, 1, TestFixtures.FixedUtc);
        act.Should().Throw<ValidationException>().WithMessage("*Monday*");
    }
}
