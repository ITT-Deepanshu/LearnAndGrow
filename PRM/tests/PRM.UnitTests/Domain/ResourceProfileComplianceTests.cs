using FluentAssertions;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.UnitTests.Common;

namespace PRM.UnitTests.Domain;

public class ResourceProfileComplianceTests
{
    [Fact]
    public void SyncMissedTimesheetWeek_ResetsReminderCountForNewWeek()
    {
        var profile = TestFixtures.CreateResourceProfile();
        profile.SyncMissedTimesheetWeek(new DateOnly(2026, 5, 11), 0, TestFixtures.FixedUtc);
        profile.RecordTimesheetReminderSent(0, TestFixtures.FixedUtc);
        profile.RecordTimesheetReminderSent(0, TestFixtures.FixedUtc);

        profile.SyncMissedTimesheetWeek(new DateOnly(2026, 5, 18), 0, TestFixtures.FixedUtc);

        profile.MissedTimesheetWeekStart.Should().Be(new DateOnly(2026, 5, 18));
        profile.TimesheetReminderCount.Should().Be(0);
    }

    [Fact]
    public void SyncMissedTimesheetWeek_IsIdempotentForSameWeek()
    {
        var profile = TestFixtures.CreateResourceProfile();
        profile.SyncMissedTimesheetWeek(new DateOnly(2026, 5, 11), 0, TestFixtures.FixedUtc);
        profile.RecordTimesheetReminderSent(0, TestFixtures.FixedUtc);

        profile.SyncMissedTimesheetWeek(new DateOnly(2026, 5, 11), 0, TestFixtures.FixedUtc);

        profile.TimesheetReminderCount.Should().Be(1);
    }

    [Fact]
    public void FreezeAndRestoreTimesheetSubmission_UpdatesFlags()
    {
        var profile = TestFixtures.CreateResourceProfile();
        profile.RecordTimesheetReminderSent(0, TestFixtures.FixedUtc);

        profile.FreezeTimesheetSubmission(0, TestFixtures.FixedUtc);
        profile.TimesheetSubmissionFrozen.Should().BeTrue();

        profile.RestoreTimesheetSubmission(0, TestFixtures.FixedUtc);
        profile.TimesheetSubmissionFrozen.Should().BeFalse();
        profile.TimesheetReminderCount.Should().Be(0);
    }

    [Fact]
    public void ClearTimesheetCompliance_ClearsAllTrackingFields()
    {
        var profile = TestFixtures.CreateResourceProfile();
        profile.SyncMissedTimesheetWeek(new DateOnly(2026, 5, 11), 0, TestFixtures.FixedUtc);
        profile.RecordTimesheetReminderSent(0, TestFixtures.FixedUtc);
        profile.FreezeTimesheetSubmission(0, TestFixtures.FixedUtc);

        profile.ClearTimesheetCompliance(0, TestFixtures.FixedUtc);

        profile.TimesheetSubmissionFrozen.Should().BeFalse();
        profile.TimesheetReminderCount.Should().Be(0);
        profile.MissedTimesheetWeekStart.Should().BeNull();
    }
}

public class ProjectAtRiskNotificationTests
{
    [Fact]
    public void SetHealth_ClearsAtRiskNotificationWhenHealthImproves()
    {
        var project = TestFixtures.CreateProject();
        project.SetHealth(HealthStatus.Red, "At risk", 0, TestFixtures.FixedUtc);
        project.MarkAtRiskNotificationSent(TestFixtures.FixedUtc, 0);

        project.SetHealth(HealthStatus.Yellow, "Attention", 0, TestFixtures.FixedUtc);

        project.AtRiskNotificationSentAt.Should().BeNull();
        project.Health.Should().Be(HealthStatus.Yellow);
    }

    [Fact]
    public void MarkAtRiskNotificationSent_SetsTimestamp()
    {
        var project = TestFixtures.CreateProject();
        project.SetHealth(HealthStatus.Red, "At risk", 0, TestFixtures.FixedUtc);

        project.MarkAtRiskNotificationSent(TestFixtures.FixedUtc, 0);

        project.AtRiskNotificationSentAt.Should().Be(TestFixtures.FixedUtc);
    }
}
