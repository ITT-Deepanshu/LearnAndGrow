namespace PRM.Domain.Helpers;

public static class WeekHelper
{
    public static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-offset);
    }

    public static DateOnly GetLastCompletedWeekMonday(DateOnly today)
    {
        var thisMonday = GetMondayOfWeek(today);
        return thisMonday.AddDays(-7);
    }

    public static bool IsMonday(DateOnly date) => date.DayOfWeek == DayOfWeek.Monday;

    public static bool IsFutureWeek(DateOnly weekStart, DateOnly today) =>
        weekStart > GetMondayOfWeek(today);
}
