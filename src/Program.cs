using Whidy.AzureDevOps;
using Whidy.Commands;
using Whidy.Configuration;
using Whidy.Core;
using Whidy.Rendering;
using Whidy.Setup;

const string Version = "0.1.0";

// ── --version / --help ───────────────────────────────────────────────────────

if (args.Length == 1 && args[0] == "--version")
{
    Console.WriteLine($"whidy {Version}");
    return 0;
}

if (args.Length == 1 && args[0] == "--help")
{
    ConsoleRenderer.PrintHelp(Version);
    return 0;
}

// ── Parse flags ─────────────────────────────────────────────────────────────

var showWorkItems = args.Contains("--work-items", StringComparer.OrdinalIgnoreCase)
                 || args.Contains("--wi", StringComparer.OrdinalIgnoreCase);
args = args.Where(a => !a.Equals("--work-items", StringComparison.OrdinalIgnoreCase)
                    && !a.Equals("--wi", StringComparison.OrdinalIgnoreCase)).ToArray();

// ── Parse arguments ──────────────────────────────────────────────────────────

DateRange requestedRange;
try
{
    requestedRange = CommandParser.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

// ── Load configuration ───────────────────────────────────────────────────────

var configLoader = new ConfigurationLoader();
var (config, isFirstRun) = await configLoader.LoadAsync();

using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

if (isFirstRun)
{
    var setup = new FirstRunSetup(configLoader);
    var newConfig = await setup.RunAsync(httpClient);
    if (newConfig is null) return 1;
    config = newConfig;
}

var appSettings = configLoader.LoadAppSettings();

// ── Run the pipeline ─────────────────────────────────────────────────────────

var client = new AzureDevOpsClient(httpClient, config.AzureDevOps.Url, config.AzureDevOps.Pat);

try
{
    // Resolve identity
    var identity = await IdentityResolver.ResolveAsync(client);

    // Fetch events — with yesterday lookback if needed
    var (events, actualRange) = await FetchWithLookbackAsync(client, identity, requestedRange, appSettings);

    if (events.Count == 0)
    {
        Console.WriteLine(EmptyMessage(requestedRange));
        return 0;
    }

    // Group into episodes
    var episodes = EpisodeGrouper.Group(events, appSettings.EpisodeWindowMinutes);

    // Fetch work items — always, for label enrichment; display only with --wi
    var workItems = await WorkItemFetcher.FetchAsync(client, identity, actualRange);
    var workItemTitles = workItems.ToDictionary(w => w.Id, w => w.Title);

    // Classify + label
    EpisodeClassifier.Classify(episodes, appSettings.Insights);
    LabelGenerator.GenerateLabels(episodes, workItemTitles);

    // Extract insights
    var insights = InsightEngine.Extract(events, episodes, appSettings.Insights);

    // Render
    ConsoleRenderer.Render(actualRange, episodes, insights, showWorkItems ? workItems : null);

    return 0;
}
catch (AzureDevOpsException ex) when (ex.StatusCode == 401)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine("Update your token by deleting the config file and running 'whidy' again.");
    Console.Error.WriteLine($"Config location: {configLoader.ConfigFilePath}");
    return 1;
}
catch (AzureDevOpsException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
catch (HttpRequestException)
{
    Console.Error.WriteLine("Couldn't reach Azure DevOps. Check your internet connection and try again.");
    return 1;
}

// ── Helpers ───────────────────────────────────────────────────────────────────

static async Task<(List<Whidy.Core.Models.WorkEvent> Events, DateRange ActualRange)> FetchWithLookbackAsync(
    AzureDevOpsClient client,
    Whidy.Core.Models.UserIdentity identity,
    DateRange requested,
    AppSettings settings)
{
    if (requested.Kind != DateRangeKind.Yesterday)
    {
        var events = await EventFetcher.FetchAsync(client, identity, requested);
        return (events, requested);
    }

    // Yesterday lookback: try up to 7 days back
    for (int daysBack = 1; daysBack <= 7; daysBack++)
    {
        var date = DateOnly.FromDateTime(DateTime.Now).AddDays(-daysBack);
        var range = new DateRange(date, date,
            daysBack == 1 ? DateRangeKind.Yesterday : DateRangeKind.Weekday);

        var events = await EventFetcher.FetchAsync(client, identity, range);
        if (events.Count > 0)
            return (events, range);
    }

    return ([], requested);
}

static string EmptyMessage(DateRange requested)
{
    if (requested.Kind == DateRangeKind.Yesterday)
        return "No activity found in the last 7 days. Have you been on a break?";

    var period = requested.Kind switch
    {
        DateRangeKind.Today => "today",
        DateRangeKind.Weekday => requested.Start.DayOfWeek.ToString().ToLowerInvariant(),
        DateRangeKind.LastWeek => "last week",
        DateRangeKind.LastMonth => "last month",
        DateRangeKind.SpecificDate => requested.Start.ToString("yyyy-MM-dd"),
        DateRangeKind.DateInterval =>
            $"{requested.Start:yyyy-MM-dd} to {requested.End:yyyy-MM-dd}",
        _ => "the requested period"
    };

    return $"No activity found for {period}. Try a different date, or check that you have commits or pull requests for that time.";
}
