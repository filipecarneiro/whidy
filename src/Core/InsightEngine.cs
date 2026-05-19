using Whidy.Configuration;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class InsightEngine
{
    public static List<Insight> Extract(
        List<WorkEvent> events,
        List<Episode> episodes,
        InsightSettings settings)
    {
        var insights = new List<Insight>();

        if (events.Count == 0 || episodes.Count == 0)
            return insights;

        AddIfNotNull(insights, FocusDetection(events, settings));
        AddIfNotNull(insights, ContextSwitching(episodes, settings));
        AddIfNotNull(insights, WorkTypeBalance(episodes, settings));
        AddIfNotNull(insights, IntensitySpike(events, settings));
        AddIfNotNull(insights, FailureHeavyDay(events, settings));

        return insights;
    }

    // ── Focus Detection ───────────────────────────────────────────────────────

    private static Insight? FocusDetection(List<WorkEvent> events, InsightSettings settings)
    {
        if (events.Count == 0) return null;

        var byRepo = events.GroupBy(e => e.Repository).ToList();
        var topRepo = byRepo.OrderByDescending(g => g.Count()).First();
        var share = (double)topRepo.Count() / events.Count;

        if (share < settings.FocusDetectionThreshold) return null;

        return new Insight
        {
            Sentence = $"You spent most of your time in {topRepo.Key}",
            Rule = "FocusDetection"
        };
    }

    // ── Context Switching ─────────────────────────────────────────────────────

    private static Insight? ContextSwitching(List<Episode> episodes, InsightSettings settings)
    {
        var distinctRepoEpisodes = episodes
            .Select(e => e.Repository)
            .Distinct()
            .Count();

        if (episodes.Count <= settings.ContextSwitchingEpisodeThreshold) return null;
        if (distinctRepoEpisodes <= 1) return null;

        return new Insight
        {
            Sentence = "You switched contexts several times during the day",
            Rule = "ContextSwitching"
        };
    }

    // ── Work Type Balance ─────────────────────────────────────────────────────

    private static Insight? WorkTypeBalance(List<Episode> episodes, InsightSettings settings)
    {
        if (episodes.Count == 0) return null;

        var counts = episodes.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());
        var total = episodes.Count;

        foreach (var (type, count) in counts)
        {
            var share = (double)count / total;
            if (share < settings.WorkTypeBalanceThreshold) continue;

            var sentence = type switch
            {
                EpisodeType.Debugging =>
                    "You spent most of your time fixing issues rather than adding features",
                EpisodeType.Review =>
                    "You mostly reviewed code (more code read than written)",
                EpisodeType.Coding =>
                    "You focused on new work (high commit activity, low failure rate)",
                _ => null
            };

            if (sentence is null) continue;

            return new Insight { Sentence = sentence, Rule = "WorkTypeBalance" };
        }

        return null;
    }

    // ── Intensity Spike ───────────────────────────────────────────────────────

    private static Insight? IntensitySpike(List<WorkEvent> events, InsightSettings settings)
    {
        var window = TimeSpan.FromMinutes(settings.IntensitySpikeWindowMinutes);
        var sorted = events.OrderBy(e => e.Timestamp).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var windowEnd = sorted[i].Timestamp + window;
            var count = sorted.Skip(i).TakeWhile(e => e.Timestamp <= windowEnd).Count();

            if (count >= settings.IntensitySpikeEventCount)
                return new Insight
                {
                    Sentence = "You hit a flow state, a concentrated burst of focused activity",
                    Rule = "IntensitySpike"
                };
        }

        return null;
    }

    // ── Failure-Heavy Day ─────────────────────────────────────────────────────

    private static Insight? FailureHeavyDay(List<WorkEvent> events, InsightSettings settings)
    {
        var builds = events.Where(e => e.Type == EventType.Build).ToList();
        if (builds.Count == 0) return null;

        var failures = builds.Count(b =>
            b.Outcome is BuildOutcome.Failed or BuildOutcome.PartiallySucceeded);

        if ((double)failures / builds.Count < settings.FailureHeavyDayThreshold) return null;

        return new Insight
        {
            Sentence = "You spent a lot of time fighting builds and fixing regressions",
            Rule = "FailureHeavyDay"
        };
    }

    private static void AddIfNotNull(List<Insight> list, Insight? insight)
    {
        if (insight is not null) list.Add(insight);
    }
}
