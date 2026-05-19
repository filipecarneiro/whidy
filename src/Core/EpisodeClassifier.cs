using Whidy.Configuration;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class EpisodeClassifier
{
    /// <summary>
    /// Classifies each episode as Debugging, Review, or Coding per priority order in the spec.
    /// </summary>
    public static void Classify(List<Episode> episodes, InsightSettings settings)
    {
        foreach (var episode in episodes)
            episode.Type = ClassifyOne(episode, settings);
    }

    private static EpisodeType ClassifyOne(Episode episode, InsightSettings settings)
    {
        var buildEvents = episode.Events.Where(e => e.Type == EventType.Build).ToList();
        var prEvents = episode.Events.Where(e =>
            e.Type is EventType.PullRequest or EventType.PrComment or EventType.PrApproval).ToList();
        var commitEvents = episode.Events.Where(e => e.Type == EventType.Commit).ToList();

        // 1. Debugging: any failed/partial build, failed deployment, or test run with failures
        if (buildEvents.Any(b =>
            b.Outcome is BuildOutcome.Failed or BuildOutcome.PartiallySucceeded))
            return EpisodeType.Debugging;

        var deploymentEvents = episode.Events.Where(e => e.Type == EventType.Deployment).ToList();
        if (deploymentEvents.Any(d => d.Outcome == BuildOutcome.Failed))
            return EpisodeType.Debugging;

        var testRunEvents = episode.Events.Where(e => e.Type == EventType.TestRun).ToList();
        if (testRunEvents.Any(t => t.FailedTests.GetValueOrDefault() > 0))
            return EpisodeType.Debugging;

        var pipelineRetriggers = buildEvents
            .Where(b => b.PipelineName != null)
            .GroupBy(b => b.PipelineName)
            .Any(g => g.Count() > settings.DebuggingBuildRetriggerThreshold);

        if (pipelineRetriggers)
            return EpisodeType.Debugging;

        // 2. Review: majority of events are PR-related
        if (prEvents.Count > episode.Events.Count / 2.0)
            return EpisodeType.Review;

        // 3. Coding (default)
        return EpisodeType.Coding;
    }
}
