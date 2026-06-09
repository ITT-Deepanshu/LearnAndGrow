using FluentAssertions;
using NSubstitute;
using PRM.Application.Features.SystemConfig.Commands;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.SystemConfig;

public class UpdateMaxWeeklyHoursCommandHandlerTests
{
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateMaxWeeklyHoursCommandHandlerTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Admin);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _config.GetAsync(Arg.Any<CancellationToken>())
            .Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
    }

    private UpdateMaxWeeklyHoursCommandHandler CreateHandler() => new(
        _config, _audit, _current, _uow, _clock);

    [Fact]
    public async Task Handle_UpdatesMaxWeeklyHours()
    {
        await CreateHandler().Handle(new UpdateMaxWeeklyHoursCommand(45), CancellationToken.None);

        var config = await _config.GetAsync(CancellationToken.None);
        config.MaxWeeklyHours.Should().Be(45);
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenNotAdmin()
    {
        _current.Role.Returns(UserRole.Manager);

        var act = () => CreateHandler().Handle(new UpdateMaxWeeklyHoursCommand(45), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
