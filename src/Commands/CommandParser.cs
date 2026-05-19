namespace Whidy.Commands;

public static class CommandParser
{
    private static readonly string[] WeekdayNames = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

    public static DateRange Parse(string[] args)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (args.Length == 0 || (args.Length == 1 && args[0].Equals("yesterday", StringComparison.OrdinalIgnoreCase)))
            return new DateRange(today.AddDays(-1), today.AddDays(-1), DateRangeKind.Yesterday);

        if (args.Length == 1 && args[0].Equals("today", StringComparison.OrdinalIgnoreCase))
            return new DateRange(today, today, DateRangeKind.Today);

        if (args.Length == 1 && args[0].Equals("last-week", StringComparison.OrdinalIgnoreCase))
        {
            var monday = today.AddDays(-(int)today.DayOfWeek - 6); // previous Monday
            if (today.DayOfWeek == DayOfWeek.Monday) monday = today.AddDays(-7);
            else monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7 - 7 + 7);
            monday = PreviousWeekMonday(today);
            var sunday = monday.AddDays(6);
            return new DateRange(monday, sunday, DateRangeKind.LastWeek);
        }

        if (args.Length == 1 && args[0].Equals("last-month", StringComparison.OrdinalIgnoreCase))
        {
            var firstOfLastMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
            var lastOfLastMonth = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
            return new DateRange(firstOfLastMonth, lastOfLastMonth, DateRangeKind.LastMonth);
        }

        if (args.Length == 1 && Array.Exists(WeekdayNames, w => w.Equals(args[0], StringComparison.OrdinalIgnoreCase)))
        {
            var targetDay = Enum.Parse<DayOfWeek>(args[0], ignoreCase: true);
            var date = MostRecentPastWeekday(today, targetDay);
            return new DateRange(date, date, DateRangeKind.Weekday);
        }

        if (args.Length == 1 && DateOnly.TryParseExact(args[0], "yyyy-MM-dd", out var specificDate))
            return new DateRange(specificDate, specificDate, DateRangeKind.SpecificDate);

        if (args.Length == 2
            && DateOnly.TryParseExact(args[0], "yyyy-MM-dd", out var rangeStart)
            && DateOnly.TryParseExact(args[1], "yyyy-MM-dd", out var rangeEnd))
        {
            if (rangeEnd < rangeStart)
                throw new ArgumentException("End date must be on or after start date.");
            return new DateRange(rangeStart, rangeEnd, DateRangeKind.DateInterval);
        }

        throw new ArgumentException($"Unrecognised argument(s): {string.Join(" ", args)}. Run 'whidy --help' for usage.");
    }

    private static DateOnly MostRecentPastWeekday(DateOnly today, DayOfWeek target)
    {
        var daysBack = ((int)today.DayOfWeek - (int)target + 7) % 7;
        if (daysBack == 0) daysBack = 7; // never return today
        return today.AddDays(-daysBack);
    }

    private static DateOnly PreviousWeekMonday(DateOnly today)
    {
        // Start of the current week (Monday)
        var daysFromMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var thisMonday = today.AddDays(-daysFromMonday);
        return thisMonday.AddDays(-7);
    }
}
