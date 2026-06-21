using FluentAssertions;
using NSubstitute;
using PRM.Application.Employees;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.ResourceProfiles;

public class AddEmployeeSkillServiceTests
{
    private readonly IResourceProfileRepository _employees = Substitute.For<IResourceProfileRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AddEmployeeSkillServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Admin);
        TestFixtures.SetupPermissions(_current, UserRole.Admin);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _employees.GetByIdWithSkillsAsync(10, Arg.Any<CancellationToken>())
            .Returns(TestFixtures.CreateResourceProfile());
    }

    private EmployeeService CreateService() => new(
        _employees, Substitute.For<IUserRepository>(), _audit, _current, _uow, _clock);

    [Fact]
    public async Task AddSkillAsync_AddsSkillForAdmin()
    {
        var dto = new AddResourceProfileSkillDto("C#", SkillCategory.Backend, SkillProficiency.Advanced);

        var result = await CreateService().AddSkillAsync(10, dto, CancellationToken.None);

        result.Name.Should().Be("C#");
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddSkillAsync_ThrowsWhenNotAdmin()
    {
        _current.Role.Returns(UserRole.Manager);
        TestFixtures.SetupPermissions(_current, UserRole.Manager);
        var dto = new AddResourceProfileSkillDto("C#", SkillCategory.Backend, SkillProficiency.Advanced);

        var act = () => CreateService().AddSkillAsync(10, dto, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task AddSkillAsync_ThrowsWhenEmployeeNotFound()
    {
        _employees.GetByIdWithSkillsAsync(99, Arg.Any<CancellationToken>()).Returns((ResourceProfile?)null);
        var dto = new AddResourceProfileSkillDto("C#", SkillCategory.Backend, SkillProficiency.Advanced);

        var act = () => CreateService().AddSkillAsync(99, dto, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
