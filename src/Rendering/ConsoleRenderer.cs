using Whidy.Commands;
using Whidy.Core.Models;

namespace Whidy.Rendering;

public static class ConsoleRenderer
{
    public static void Render(DateRange actual, List<Episode> episodes, List<Insight> insights,
        List<WorkItemSummary>? workItems = null)
    {
        var header = HeaderResolver.Resolve(actual);
        Console.WriteLine();
        Console.WriteLine(header);
        Console.WriteLine();

        // Group episodes by repository for display
        var byRepo = episodes
            .GroupBy(e => e.Repository)
            .OrderByDescending(g => g.Sum(ep => ep.Events.Count));

        // Determine the focus repo (the one with the most events) — used to append the focus line
        var totalEvents = episodes.Sum(e => e.Events.Count);
        var focusInsight = insights.FirstOrDefault(i => i.Rule == "FocusDetection");

        foreach (var repoGroup in byRepo)
        {
            Console.WriteLine(repoGroup.Key);

            // If this repo triggered focus detection, print the focus line
            if (focusInsight is not null)
            {
                var repoEventCount = repoGroup.Sum(ep => ep.Events.Count);
                if (totalEvents > 0 && repoEventCount == byRepo.Max(g => g.Sum(ep => ep.Events.Count)))
                    Console.WriteLine("You spent most of your time here.");
            }

            Console.WriteLine();

            var shownLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var episode in repoGroup)
            {
                // Skip episodes whose label duplicates one already shown for this repo
                if (shownLabels.Add(episode.Label))
                    Console.WriteLine($"\u2022 {episode.Label}");
            }

            Console.WriteLine();
        }

        if (insights.Count > 0)
        {
            Console.WriteLine("📊 Insights");
            foreach (var insight in insights)
                Console.WriteLine($"• {insight.Sentence}");
            Console.WriteLine();
        }

        if (workItems is { Count: > 0 })
        {
            Console.WriteLine("Work Items");
            foreach (var item in workItems)
            {
                var link = $"\x1b]8;;{item.Url}\x1b\\#{item.Id}\x1b]8;;\x1b\\";
                Console.WriteLine($"\u2022 {link}  {item.Title}  [{item.State}]");
            }
            Console.WriteLine();
        }
    }

    public static void PrintHelp(string version)
    {
        Console.WriteLine($"whidy {version} — reconstruct your workday from development activity");
        Console.WriteLine();
        Console.WriteLine("USAGE");
        Console.WriteLine("  whidy [command]");
        Console.WriteLine();
        Console.WriteLine("COMMANDS");
        Console.WriteLine("  (none)              Report for yesterday (default)");
        Console.WriteLine("  yesterday           Report for yesterday");
        Console.WriteLine("  today               Report for today so far");
        Console.WriteLine("  monday|tuesday|...  Report for the most recent past occurrence of that weekday");
        Console.WriteLine("  YYYY-MM-DD          Report for a specific date");
        Console.WriteLine("  YYYY-MM-DD YYYY-MM-DD  Report for a date range (inclusive)");
        Console.WriteLine("  last-week           Report for the previous full calendar week");
        Console.WriteLine("  last-month          Report for the previous full calendar month");
        Console.WriteLine("  --help              Show this help");
        Console.WriteLine("  --version           Show version");
        Console.WriteLine();
        Console.WriteLine("OPTIONS");
        Console.WriteLine("  --work-items, --wi  Include work items assigned to you with activity in the date range");
    }
}
