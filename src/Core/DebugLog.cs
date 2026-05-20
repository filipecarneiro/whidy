namespace Whidy.Core;

/// <summary>
/// Lightweight debug output. Writes dimmed lines to stderr so normal stdout stays clean.
/// Enable once at startup via <see cref="Enabled"/>.
/// </summary>
internal static class DebugLog
{
    public static bool Enabled { get; set; }

    public static void Write(string message)
    {
        if (!Enabled) return;
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Error.WriteLine($"[debug] {message}");
        Console.ForegroundColor = prev;
    }

    public static void Section(string title)
    {
        if (!Enabled) return;
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Error.WriteLine($"[debug] ── {title} ──");
        Console.ForegroundColor = prev;
    }
}
