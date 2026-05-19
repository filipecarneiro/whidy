using System.Text.RegularExpressions;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class LabelGenerator
{
    private const int MaxLength = 60;

    // Auto-generated merge commits have no useful label content
    private static readonly Regex AutoMergeCommit = new(
        @"^merged?\s+(?:(?:pull\s+request|pr)\s+#?\d+|branch\s+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Conventional Commits 1.0.0: <type>[(<scope>)][!]: <description>
    // Type is any noun — spec does not mandate a closed list. Known types map to
    // human verbs; unknown types fall through to the imperative-prefix check.
    private static readonly Regex ConventionalCommit = new(
        @"^(?<type>[a-zA-Z][a-zA-Z0-9\-]*)(?:\((?<scope>[^)]+)\))?(?<breaking>!)?:\s*(?<msg>\S.+)$",
        RegexOptions.Compiled);

    // Common imperative verbs at the start of a commit message
    private static readonly (Regex Pattern, string Verb)[] VerbPrefixes =
    [
        (new Regex(@"^(?:add|adds|added)\s+",         RegexOptions.IgnoreCase | RegexOptions.Compiled), "Added"),
        (new Regex(@"^(?:fix|fixes|fixed)\s+",         RegexOptions.IgnoreCase | RegexOptions.Compiled), "Fixed"),
        (new Regex(@"^(?:remove|removes|removed|delete|deletes|deleted)\s+",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled), "Removed"),
        (new Regex(@"^(?:refactor|refactors|refactored)\s+",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled), "Refactored"),
        (new Regex(@"^(?:update|updates|updated|upgrade|upgrades|upgraded)\s+",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled), "Updated"),
        (new Regex(@"^(?:revert|reverts|reverted)\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Reverted"),
        (new Regex(@"^(?:improve|improves|improved)\s+",RegexOptions.IgnoreCase | RegexOptions.Compiled), "Improved"),
        (new Regex(@"^(?:migrate|migrates|migrated)\s+",RegexOptions.IgnoreCase | RegexOptions.Compiled), "Migrated"),
        (new Regex(@"^(?:enable|enables|enabled)\s+",  RegexOptions.IgnoreCase | RegexOptions.Compiled), "Enabled"),
        (new Regex(@"^(?:disable|disables|disabled)\s+",RegexOptions.IgnoreCase | RegexOptions.Compiled), "Disabled"),
        (new Regex(@"^(?:rename|renames|renamed)\s+",  RegexOptions.IgnoreCase | RegexOptions.Compiled), "Renamed"),
        (new Regex(@"^(?:move|moves|moved)\s+",        RegexOptions.IgnoreCase | RegexOptions.Compiled), "Moved"),
        (new Regex(@"^(?:replace|replaces|replaced)\s+",RegexOptions.IgnoreCase | RegexOptions.Compiled), "Replaced"),
        (new Regex(@"^(?:implement|implements|implemented)\s+",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled), "Implemented"),
        (new Regex(@"^(?:introduce|introduces|introduced)\s+",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled), "Added"),
        (new Regex(@"^(?:extract|extracts|extracted)\s+",
                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled), "Extracted"),
    ];

    public static void GenerateLabels(List<Episode> episodes,
        IReadOnlyDictionary<int, string>? workItemTitles = null)
    {
        foreach (var episode in episodes)
            episode.Label = GenerateLabel(episode, workItemTitles);
    }

    private static string GenerateLabel(Episode episode, IReadOnlyDictionary<int, string>? wiTitles)
    {
        return episode.Type switch
        {
            EpisodeType.Coding    => CodingLabel(episode, wiTitles),
            EpisodeType.Debugging => DebuggingLabel(episode, wiTitles),
            EpisodeType.Review    => ReviewLabel(episode),
            _                     => $"Work in {episode.Repository}"
        };
    }

    private static string CodingLabel(Episode episode, IReadOnlyDictionary<int, string>? wiTitles)
    {
        string EffTitle(WorkEvent e) =>
            !string.IsNullOrWhiteSpace(e.Title) ? e.Title
            : (e.WorkItemId.HasValue && wiTitles?.TryGetValue(e.WorkItemId.Value, out var t) == true ? t : "");

        var commits = episode.Events
            .Where(e => e.Type == EventType.Commit)
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        // Prefer the most recent commit that isn't an auto-generated merge commit
        var best = commits.FirstOrDefault(e =>
        {
            var title = EffTitle(e);
            return !string.IsNullOrWhiteSpace(title) && !AutoMergeCommit.IsMatch(title);
        });

        // If every commit in this episode is a merge commit (or there are none),
        // fall back to a deployment / test run event title if available, then the repo name.
        if (best is null)
        {
            var altTitle = episode.Events
                .Where(e => e.Type is EventType.Deployment or EventType.TestRun
                            && !string.IsNullOrWhiteSpace(e.Title))
                .OrderByDescending(e => e.Timestamp)
                .Select(e => e.Title!)
                .FirstOrDefault();
            return Truncate(altTitle ?? $"Work in {episode.Repository}");
        }

        var effTitle = EffTitle(best);

        // When the title came from a work item fallback (commit was just an ID),
        // WI titles are task descriptions — don't run them through verb parsing.
        // Append the AB# ref so the source is always traceable.
        if (string.IsNullOrWhiteSpace(best.Title))
            return Truncate($"{effTitle} (AB#{best.WorkItemId})");

        var (verb, message) = ParseCommitVerb(effTitle);
        return Truncate($"{verb} {LowerFirst(message)}");
    }

    private static string DebuggingLabel(Episode episode, IReadOnlyDictionary<int, string>? wiTitles)
    {
        string EffTitle(WorkEvent e) =>
            !string.IsNullOrWhiteSpace(e.Title) ? e.Title
            : (e.WorkItemId.HasValue && wiTitles?.TryGetValue(e.WorkItemId.Value, out var t) == true ? t : "");

        var hint = episode.Events
            .Where(e => e.Type is EventType.Commit or EventType.Build)
            .OrderByDescending(e => e.Timestamp)
            .Select(EffTitle)
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t) && !AutoMergeCommit.IsMatch(t));

        return hint is not null
            ? Truncate($"Fixed issues in {episode.Repository} ({LowerFirst(hint)})")
            : Truncate($"Fixed issues in {episode.Repository}");
    }

    private static string ReviewLabel(Episode episode)
    {
        var prTitle = episode.Events
            .Where(e => e.Type is EventType.PullRequest or EventType.PrApproval or EventType.PrComment
                        && !string.IsNullOrWhiteSpace(e.Title))
            .OrderByDescending(e => e.Timestamp)
            .Select(e => e.Title!)
            .FirstOrDefault();

        return prTitle is not null
            ? Truncate($"Reviewed: {prTitle}")
            : Truncate($"Reviewed changes in {episode.Repository}");
    }

    // ── Commit verb parsing ───────────────────────────────────────────────────

    private static (string Verb, string Message) ParseCommitVerb(string title)
    {
        // 1. Conventional Commits 1.0.0: <type>[(<scope>)][!]: <description>
        var cc = ConventionalCommit.Match(title);
        if (cc.Success)
        {
            var type     = cc.Groups["type"].Value.ToLowerInvariant();
            var scope    = cc.Groups["scope"].Value.Trim();
            var breaking = cc.Groups["breaking"].Success;
            var msg      = cc.Groups["msg"].Value.Trim();

            var verb = ConventionalTypeToVerb(type);
            if (verb is not null)
            {
                // Strip redundant leading verb echoed in the description
                // e.g. "feat: Add X" → "Added X", not "Added Add X"
                msg = StripRedundantLeadingVerb(msg, verb);

                // Append scope and/or breaking-change context
                if (!string.IsNullOrEmpty(scope) && breaking)
                    msg = $"{msg} ({scope}, breaking change)";
                else if (!string.IsNullOrEmpty(scope))
                    msg = $"{msg} ({scope})";
                else if (breaking)
                    msg = $"{msg} (breaking change)";

                return (verb, msg);
            }
            // Unknown type — fall through to imperative-prefix check
        }

        // 2. Common imperative verb at the start of the message
        foreach (var (pattern, verb) in VerbPrefixes)
        {
            var m = pattern.Match(title);
            if (m.Success)
                return (verb, title[m.Length..].TrimStart());
        }

        return ("Implemented", title);
    }

    // Maps Conventional Commits type tokens to past-tense human verbs.
    // Returns null for unknown types so the imperative-prefix check can run.
    private static string? ConventionalTypeToVerb(string type) => type switch
    {
        "fix" or "bugfix" or "hotfix" or "patch"                          => "Fixed",
        "feat" or "feature"                                               => "Added",
        "docs" or "doc"                                                   => "Updated",
        "refactor" or "rework"                                            => "Refactored",
        "test" or "tests" or "spec"                                       => "Added tests for",
        "perf" or "performance" or "opt" or "optimize"                   => "Improved",
        "revert"                                                          => "Reverted",
        "remove" or "delete"                                              => "Removed",
        "build" or "ci" or "cd" or "chore" or "style" or "infra"
            or "tooling" or "deps" or "dependencies"                      => "Updated",
        "security" or "sec"                                               => "Fixed",
        _                                                                 => null
    };

    private static string StripRedundantLeadingVerb(string msg, string verb)
    {
        // e.g. verb="Added", msg starts with "add " → strip → avoid "Added add X"
        string[] patterns = verb.ToLowerInvariant() switch
        {
            "added"      => [@"^adds?\s+", @"^added\s+"],
            "fixed"      => [@"^fix(?:es|ed)?\s+"],
            "updated"    => [@"^update[sd]?\s+", @"^upgraded?\s+"],
            "refactored" => [@"^refactored?\s+"],
            "improved"   => [@"^improve[sd]?\s+"],
            "removed"    => [@"^removed?\s+", @"^delete[sd]?\s+"],
            _            => []
        };

        foreach (var p in patterns)
        {
            var stripped = Regex.Replace(msg, p, "", RegexOptions.IgnoreCase).TrimStart();
            if (!string.IsNullOrWhiteSpace(stripped) && stripped != msg)
                return stripped;
        }
        return msg;
    }

    private static string Truncate(string text)
        => text.Length <= MaxLength ? text : text[..MaxLength];

    private static string LowerFirst(string text)
        => string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text[1..];
}
