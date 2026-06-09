using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.Employees.Commands;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.Employees;

public class AddEmployeeSkillCommandHandlerTests
{
    private readonly IEmployeeRepository _employees = Substitute.For<IEmployeeRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AddEmployeeSkillCommandHandlerTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Admin);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _employees.GetByIdWithSkillsAsync(10, Arg.Any<CancellationToken>())
            .Returns(TestFixtures.CreateEmployee());
    }

    private AddEmployeeSkillCommandHandler CreateHandler() => new(
        _employees, _audit, _current, _uow, _clock);

    [Fact]
    public async Task Handle_AddsSkillForAdmin()
    {
        var command = new AddEmployeeSkillCommand(10, "C#", SkillCategory.Backend, SkillProficiency.Advanced);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Name.Should().Be("C#");
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenNotAdmin()
    {
        _current.Role.Returns(UserRole.Manager);
        var command = new AddEmployeeSkillCommand(10, "C#", SkillCategory.Backend, SkillProficiency.Advanced);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenEmployeeNotFound()
    {
        _employees.GetByIdWithSkillsAsync(99, Arg.Any<CancellationToken>()).Returns((Employee?)null);
        var command = new AddEmployeeSkillCommand(99, "C#", SkillCategory.Backend, SkillProficiency.Advanced);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
