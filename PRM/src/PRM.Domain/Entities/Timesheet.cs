using PRM.Domain.Enums;
using PRM.Domain.Exceptions;
using PRM.SharedKernel;

namespace PRM.Domain.Entities;

public class Timesheet : AuditableEntity
{
    private readonly List<TimesheetEntry> _entries = [];

    private Timesheet() { }

    public long EmployeeId { get; private set; }
    public DateOnly WeekStart { get; private set; }
    public decimal TotalHours { get; private set; }
    public TimesheetStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }

    public Employee Employee { get; private set; } = null!;
    public IReadOnlyCollection<TimesheetEntry> Entries => _entries.AsReadOnly();

    public static Timesheet CreateMissed(long employeeId, DateOnly weekStart, long actorId, DateTime utcNow)
    {
        var timesheet = new Timesheet
        {
            EmployeeId = employeeId,
            WeekStart = weekStart,
            TotalHours = 0,
            Status = TimesheetStatus.Missed
        };
        timesheet.SetCreated(actorId, utcNow);
        return timesheet;
    }

    public static Timesheet Submit(
        long employeeId,
        DateOnly weekStart,
        IEnumerable<TimesheetEntry> entries,
        int maxWeeklyHours,
        long actorId,
        DateTime utcNow)
    {
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
            throw new ValidationException("Week start must be a Monday.");

        var entryList = entries.ToList();
        if (entryList.Count == 0)
            throw new ValidationException("At least one timesheet entry is required.");

        var totalHours = entryList.Sum(e => e.Hours);
        if (totalHours > maxWeeklyHours)
            throw new BusinessRuleException($"Total hours cannot exceed {maxWeeklyHours}.");

        var timesheet = new Timesheet
        {
            EmployeeId = employeeId,
            WeekStart = weekStart,
            TotalHours = totalHours,
            Status = TimesheetStatus.Submitted,
            SubmittedAt = utcNow
        };
        timesheet.SetCreated(actorId, utcNow);
        timesheet._entries.AddRange(entryList);
        return timesheet;
    }
}
