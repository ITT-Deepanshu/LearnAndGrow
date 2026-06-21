using System.Net;
using System.Text;
using PRM.Application.Interfaces.Notifications;
using PRM.Application.Notifications.Templates;

namespace PRM.Infrastructure.Email;

public sealed class HtmlEmailTemplateRenderer : IEmailTemplateRenderer
{
    public string RenderTimesheetReminder(TimesheetReminderEmailModel model) =>
        Wrap(
            $"Timesheet reminder {model.ReminderNumber}",
            $"""
            <p>Hi {Encode(model.EmployeeName)},</p>
            <p>This is reminder <strong>#{model.ReminderNumber}</strong> that your timesheet for the week starting
            <strong>{model.WeekStart:dd MMM yyyy}</strong> has not been submitted.</p>
            <p>Please log in to PRM and submit your hours as soon as possible.</p>
            """);

    public string RenderTimesheetFrozenEmployee(TimesheetFrozenEmployeeEmailModel model) =>
        Wrap(
            "Timesheet submission restricted",
            $"""
            <p>Hi {Encode(model.EmployeeName)},</p>
            <p>Your timesheet for the week starting <strong>{model.WeekStart:dd MMM yyyy}</strong> is still missing
            after two email reminders.</p>
            <p>Your account can still be used to <strong>view</strong> data, but you cannot create, update,
            or submit timesheet entries until your manager restores access.</p>
            """);

    public string RenderTimesheetFrozenManager(TimesheetFrozenManagerEmailModel model) =>
        Wrap(
            $"Action required: {model.EmployeeName} timesheet frozen",
            $"""
            <p>Hi {Encode(model.ManagerName)},</p>
            <p><strong>{Encode(model.EmployeeName)}</strong> did not submit a timesheet for the week starting
            <strong>{model.WeekStart:dd MMM yyyy}</strong> after two reminders.</p>
            <p>Their timesheet submission access has been restricted. They can still log in and view data,
            but cannot submit timesheets until you restore access from the PRM manager console.</p>
            """);

    public string RenderProjectAtRisk(ProjectAtRiskEmailModel model) =>
        Wrap(
            $"Project at risk: {model.ProjectName}",
            $"""
            <p>Hi {Encode(model.ManagerName)},</p>
            <p>The Project Health Scheduler has marked <strong>{Encode(model.ProjectName)}</strong> as
            <strong>{Encode(model.HealthLabel)}</strong>.</p>
            <h3>Project details</h3>
            <ul>
              <li><strong>Project:</strong> {Encode(model.ProjectName)}</li>
              <li><strong>Manager:</strong> {Encode(model.ManagerName)}</li>
              <li><strong>Health:</strong> {Encode(model.HealthLabel)}</li>
            </ul>
            <h3>Milestones</h3>
            {BuildMilestoneList(model.Milestones)}
            <h3>Risk flags</h3>
            {BuildBulletList(model.RiskFlags)}
            <h3>AI risk summary</h3>
            <p>{Encode(model.AiRiskSummary)}</p>
            <h3>Suggested help (bench resources)</h3>
            {BuildBulletList(model.SuggestedBenchResources)}
            """);

    private static string BuildMilestoneList(IReadOnlyList<ProjectMilestoneEmailModel> milestones)
    {
        if (milestones.Count == 0)
            return "<p>No milestones defined.</p>";

        var builder = new StringBuilder("<ul>");
        foreach (var milestone in milestones)
        {
            builder.Append("<li>")
                .Append(Encode(milestone.Title))
                .Append(" — due ")
                .Append(milestone.DueDate.ToString("dd MMM yyyy"))
                .Append(" (")
                .Append(milestone.Status)
                .Append(")</li>");
        }

        builder.Append("</ul>");
        return builder.ToString();
    }

    private static string BuildBulletList(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
            return "<p>None identified.</p>";

        var builder = new StringBuilder("<ul>");
        foreach (var item in items)
            builder.Append("<li>").Append(Encode(item)).Append("</li>");
        builder.Append("</ul>");
        return builder.ToString();
    }

    private static string Wrap(string title, string body) =>
        $"""
        <!DOCTYPE html>
        <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#222;">
        <h2 style="color:#1a5276;">{Encode(title)}</h2>
        {body}
        <hr/>
        <p style="font-size:12px;color:#666;">Sent automatically by PRM.</p>
        </body></html>
        """;

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
