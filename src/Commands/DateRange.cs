namespace Whidy.Commands;

public enum DateRangeKind
{
    Yesterday,
    Today,
    Weekday,
    SpecificDate,
    DateInterval,
    LastWeek,
    LastMonth
}

/// <summary>Represents a resolved date range and how it was specified (for header generation).</summary>
public record DateRange(DateOnly Start, DateOnly End, DateRangeKind Kind);
