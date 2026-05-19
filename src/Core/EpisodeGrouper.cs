using Whidy.Core.Models;

namespace Whidy.Core;

public static class EpisodeGrouper
{
    /// <summary>
    /// Groups events into episodes per the spec:
    /// - Sort by timestamp
    /// - Events in the same repo within <paramref name="windowMinutes"/> of each other → same episode
    /// - Build/PR events attach to an existing episode for the same repo if within window; otherwise start their own
    /// </summary>
    public static List<Episode> Group(List<WorkEvent> events, int windowMinutes)
    {
        var sorted = events.OrderBy(e => e.Timestamp).ToList();
        var episodes = new List<Episode>();

        foreach (var ev in sorted)
        {
            var window = TimeSpan.FromMinutes(windowMinutes);
            var lastEventTime = ev.Timestamp;

            // Try to attach to an existing open episode for the same repo
            var match = episodes
                .Where(ep => ep.Repository == ev.Repository
                             && (ev.Timestamp - ep.Events.Last().Timestamp) < window)
                .LastOrDefault();

            if (match is not null)
            {
                match.Events.Add(ev);
            }
            else
            {
                episodes.Add(new Episode
                {
                    Repository = ev.Repository,
                    ProjectName = ev.ProjectName,
                    Events = [ev]
                });
            }
        }

        return episodes;
    }
}
