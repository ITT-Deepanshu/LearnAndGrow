using FluentAssertions;
using NSubstitute;
using PRM.Application.SystemConfig;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Interfaces.Scheduling;
using PRM.Application.Interfaces.Security;

using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Application.SystemConfig;

public class SystemConfigServiceTests
{
    private readonly ISystemConfigRepository _config = Substitute.For<ISystemConfigRepository>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ICurrentUser _current = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public SystemConfigServiceTests()
    {
        _current.UserId.Returns(1L);
        _current.Role.Returns(UserRole.Admin);
        _clock.UtcNow.Returns(TestFixtures.FixedUtc);
        _config.GetAsync(Arg.Any<CancellationToken>())
            .Returns(SystemConfiguration.CreateDefault(1, TestFixtures.FixedUtc));
    }

    private SystemConfigService CreateService() => new(
        _config,
        Substitute.For<IApiKeyProtector>(),
        _audit,
        _current,
        _uow,
        Substitute.For<IPrmJobScheduleManager>(),
        _clock);

    [Fact]
    public async Task UpdateSystemConfigAsync_UpdatesMaxWeeklyHours()
    {
        var dto = new UpdateSystemConfigDto(AiProviderType.Gemma, null, 60, 45);
        await CreateService().UpdateSystemConfigAsync(dto, CancellationToken.None);

        var config = await _config.GetAsync(CancellationToken.None);
        config.MaxWeeklyHours.Should().Be(45);
        await _uow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSystemConfigAsync_ThrowsWhenNotAdmin()
    {
        _current.Role.Returns(UserRole.Manager);
        var dto = new UpdateSystemConfigDto(AiProviderType.Gemma, null, 60, 45);

        var act = () => CreateService().UpdateSystemConfigAsync(dto, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
