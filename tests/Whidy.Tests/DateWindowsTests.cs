using Whidy.Core;

namespace Whidy.Tests;

public class DateWindowsTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Week = TimeSpan.FromDays(7);

    [Fact]
    public void Split_RangeWithinLimit_ReturnsSingleWindow()
    {
        var end = Start.AddDays(6);

        var windows = DateWindows.Split(Start, end, Week);

        Assert.Equal([(Start, end)], windows);
    }

    [Fact]
    public void Split_RangeExactlyAtLimit_ReturnsSingleWindow()
    {
        var windows = DateWindows.Split(Start, Start + Week, Week);

        Assert.Single(windows);
    }

    [Fact]
    public void Split_NineDayRange_ReturnsTwoContiguousWindows()
    {
        var end = Start.AddDays(9);

        var windows = DateWindows.Split(Start, end, Week);

        Assert.Equal(2, windows.Count);
        Assert.Equal(Start, windows[0].From);
        Assert.Equal(windows[0].To, windows[1].From);
        Assert.Equal(end, windows[1].To);
        Assert.All(windows, w => Assert.True(w.To - w.From <= Week));
    }

    [Fact]
    public void Split_EmptyRange_ReturnsOneWindow()
    {
        var windows = DateWindows.Split(Start, Start, Week);

        Assert.Equal([(Start, Start)], windows);
    }

    [Fact]
    public void Split_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => DateWindows.Split(Start, Start.AddDays(-1), Week));
    }

    [Fact]
    public void Split_NonPositiveSpan_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DateWindows.Split(Start, Start.AddDays(1), TimeSpan.Zero));
    }
}
