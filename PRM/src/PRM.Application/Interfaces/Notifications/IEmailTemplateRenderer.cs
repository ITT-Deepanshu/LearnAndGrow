using PRM.Application.Notifications.Templates;

namespace PRM.Application.Interfaces.Notifications;

/// <summary>
/// Renders HTML bodies for notification emails. Delivery is handled separately by <see cref="IEmailService"/>.
/// </summary>
public interface IEmailTemplateRenderer
{
    string RenderTimesheetReminder(TimesheetReminderEmailModel model);

    string RenderTimesheetFrozenEmployee(TimesheetFrozenEmployeeEmailModel model);

    string RenderTimesheetFrozenManager(TimesheetFrozenManagerEmailModel model);

    string RenderProjectAtRisk(ProjectAtRiskEmailModel model);
}
