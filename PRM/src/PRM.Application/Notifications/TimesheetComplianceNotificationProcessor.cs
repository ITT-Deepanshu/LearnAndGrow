using Microsoft.Extensions.Logging;
using PRM.Application.Interfaces.Common;
using PRM.Application.Interfaces.Notifications;
using PRM.Application.Interfaces.Persistence;
using PRM.Application.Notifications.Templates;
using PRM.Domain.Entities;
using PRM.Domain.Enums;
using PRM.Domain.Helpers;

namespace PRM.Application.Notifications;

public sealed class TimesheetComplianceNotificationProcessor(
    IClock clock,
    IEmailService emailService,
    IEmailTemplateRenderer templateRenderer,
    IResourceProfileRepository employeeRepository,
    ITimesheetRepository timesheetRepository,
    IUnitOfWork unitOfWork,
    ILogger<TimesheetComplianceNotificationProcessor> logger) : ITimesheetComplianceNotificationProcessor
{
    private const long SystemActorId = 0;

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var utcNow = clock.UtcNow;
        var weekStart = WeekHelper.GetLastCompletedWeekMonday(today);
        var workingDaysAfterDeadline = WeekHelper.CountWorkingDaysAfter(weekStart.AddDays(6), today);

        if (workingDaysAfterDeadline == 0)
            return;

        var eligibleEmployeeIds = (await employeeRepository.ListAsync(null, null, null, cancellationToken))
            .Where(IsEligibleEmployee)
            .Select(e => e.Id)
            .ToList();

        unitOfWork.ClearChangeTracker();

        foreach (var employeeId in eligibleEmployeeIds)
        {
            var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null)
                continue;

            await ProcessEmployeeAsync(employee, weekStart, workingDaysAfterDeadline, utcNow, cancellationToken);
        }
    }

    private async Task ProcessEmployeeAsync(
        ResourceProfile employee,
        DateOnly weekStart,
        int workingDaysAfterDeadline,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (!await TryEnsureTrackedMissedWeekAsync(employee, weekStart, utcNow, cancellationToken))
            return;

        var missedWeek = employee.MissedTimesheetWeekStart!.Value;

        if (workingDaysAfterDeadline == 1 && employee.TimesheetReminderCount == 0)
        {
            await SendReminderAsync(employee, missedWeek, reminderNumber: 1, cancellationToken);
            employee.RecordTimesheetReminderSent(SystemActorId, utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (workingDaysAfterDeadline == 2 && employee.TimesheetReminderCount == 1)
        {
            await SendReminderAsync(employee, missedWeek, reminderNumber: 2, cancellationToken);
            employee.RecordTimesheetReminderSent(SystemActorId, utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        if (workingDaysAfterDeadline >= 3 && !employee.TimesheetSubmissionFrozen)
        {
            employee.FreezeTimesheetSubmission(SystemActorId, utcNow);
            await SendFreezeNotificationsAsync(employee, missedWeek, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> TryEnsureTrackedMissedWeekAsync(
        ResourceProfile employee,
        DateOnly weekStart,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (employee.MissedTimesheetWeekStart == weekStart)
            return true;

        if (employee.MissedTimesheetWeekStart is not null)
            return false;

        var timesheet = await timesheetRepository.GetByEmployeeWeekAsync(employee.Id, weekStart, cancellationToken);
        if (timesheet?.Status != TimesheetStatus.Missed)
            return false;

        employee.SyncMissedTimesheetWeek(weekStart, SystemActorId, utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task SendReminderAsync(
        ResourceProfile employee,
        DateOnly weekStart,
        int reminderNumber,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employee.User.Email))
        {
            logger.LogWarning(
                "Skipping timesheet reminder {ReminderNumber} for employee {EmployeeId} — email missing.",
                reminderNumber,
                employee.Id);
            return;
        }

        await emailService.SendAsync(
            new EmailMessage(
                employee.User.Email,
                NotificationSubjects.TimesheetReminder(reminderNumber, weekStart),
                templateRenderer.RenderTimesheetReminder(new TimesheetReminderEmailModel(
                    employee.FullName,
                    weekStart,
                    reminderNumber)),
                employee.FullName),
            cancellationToken);
    }

    private async Task SendFreezeNotificationsAsync(
        ResourceProfile employee,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(employee.User.Email))
        {
            await emailService.SendAsync(
                new EmailMessage(
                    employee.User.Email,
                    NotificationSubjects.TimesheetFrozenEmployee,
                    templateRenderer.RenderTimesheetFrozenEmployee(new TimesheetFrozenEmployeeEmailModel(
                        employee.FullName,
                        weekStart)),
                    employee.FullName),
                cancellationToken);
        }

        if (employee.Manager is null || string.IsNullOrWhiteSpace(employee.Manager.Email))
            return;

        var managerName = employee.Manager.ResourceProfile?.FullName ?? employee.Manager.Username;
        await emailService.SendAsync(
            new EmailMessage(
                employee.Manager.Email,
                NotificationSubjects.TimesheetFrozenManager(employee.FullName),
                templateRenderer.RenderTimesheetFrozenManager(new TimesheetFrozenManagerEmailModel(
                    employee.FullName,
                    managerName,
                    weekStart)),
                managerName),
            cancellationToken);
    }

    private static bool IsEligibleEmployee(ResourceProfile employee) =>
        employee.Status != ResourceProfileStatus.Inactive && employee.User.IsActive;
}
