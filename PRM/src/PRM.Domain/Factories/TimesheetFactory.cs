using PRM.Domain.Entities;

namespace PRM.Domain.Factories;

public static class TimesheetFactory
{
    public static Timesheet Submit(
        long employeeId,
        DateOnly weekStart,
        IEnumerable<TimesheetEntry> entries,
        int maxWeeklyHours,
        long actorId,
        DateTime utcNow) =>
        Timesheet.Submit(employeeId, weekStart, entries, maxWeeklyHours, actorId, utcNow);

    public static Timesheet CreateMissed(long employeeId, DateOnly weekStart, long actorId, DateTime utcNow) =>
        Timesheet.CreateMissed(employeeId, weekStart, actorId, utcNow);
}
