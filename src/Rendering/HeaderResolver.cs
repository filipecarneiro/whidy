using System.Globalization;
using Whidy.Commands;

namespace Whidy.Rendering;

public static class HeaderResolver
{
    private static readonly string[] Weekdays =
        ["MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY", "SUNDAY"];

    /// <summary>
    /// Derives the human-readable report header from the actual date range returned,
    /// not the argument used to request it.
    /// </summary>
    public static string Resolve(DateRange actual)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var start = actual.Start;
        var end = actual.End;

        // Single-day periods
        if (start == end)
        {
            if (start == today)
                return "TODAY";

            if (start == today.AddDays(-1))
                return "YESTERDAY";

            var daysAgo = today.DayNumber - start.DayNumber;

            if (daysAgo is >= 2 and <= 6)
                return Weekdays[((int)start.DayOfWeek + 6) % 7]; // shift so Monday=0

            return FormatDate(start);
        }

        // Multi-day: last-week
        if (actual.Kind == DateRangeKind.LastWeek)
            return "LAST WEEK";

        // Multi-day: last-month
        if (actual.Kind == DateRangeKind.LastMonth)
            return "LAST MONTH";

        // Explicit date range
        return $"{FormatDate(start)} to {FormatDate(end)}";
    }

    private static string FormatDate(DateOnly date)
    {
        // Use the OS short date order (D/M/Y or M/D/Y), but always numeric and English
        var pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;

        // Detect order from pattern: does 'M' come before 'd'?
        var mIndex = pattern.IndexOf('M');
        var dIndex = pattern.IndexOf('d');

        return mIndex < dIndex
            ? $"{date.Month:D2}-{date.Day:D2}-{date.Year}"
            : $"{date.Day:D2}-{date.Month:D2}-{date.Year}";
    }
}
