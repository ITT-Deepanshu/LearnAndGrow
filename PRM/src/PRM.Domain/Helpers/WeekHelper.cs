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

    /// <summary>Counts Mon–Fri on or after the day following <paramref name="periodEnd"/> through <paramref name="today"/>.</summary>
    public static int CountWorkingDaysAfter(DateOnly periodEnd, DateOnly today)
    {
        if (today <= periodEnd)
            return 0;

        var count = 0;
        for (var date = periodEnd.AddDays(1); date <= today; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        }

        return count;
    }
}
