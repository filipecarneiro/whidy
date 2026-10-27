namespace Whidy.Core;

internal static class DateWindows
{
    /// <summary>Splits [from, to] into consecutive windows of at most <paramref name="maxSpan"/>.</summary>
    public static IReadOnlyList<(DateTimeOffset From, DateTimeOffset To)> Split(
        DateTimeOffset from, DateTimeOffset to, TimeSpan maxSpan)
    {
        if (maxSpan <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxSpan), "Window size must be positive.");
        if (to < from)
            throw new ArgumentException("End must not be before start.", nameof(to));

        var windows = new List<(DateTimeOffset, DateTimeOffset)>();
        var start = from;
        while (start < to)
        {
            var end = start + maxSpan < to ? start + maxSpan : to;
            windows.Add((start, end));
            start = end;
        }

        if (windows.Count == 0)
            windows.Add((from, to));

        return windows;
    }
}
