using Whidy.AzureDevOps;
using Whidy.AzureDevOps.Models;
using Whidy.Commands;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class EventFetcher
{
    public static async Task<List<WorkEvent>> FetchAsync(
        AzureDevOpsClient client,
        UserIdentity identity,
        DateRange dateRange)
    {
        // Expand the query window by one day on each side to account for timezone offsets,
        // then filter to the exact date range after fetching.
        var from = new DateTimeOffset(dateRange.Start.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
        var to = new DateTimeOffset(dateRange.End.ToDateTime(TimeOnly.MaxValue), TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));

        var projects = await client.GetProjectsAsync();

        // Build repo list per project in parallel
        var reposByProject = await FetchRepositoriesAsync(client, projects);

        // Fetch all event types in parallel
        var commitTask = FetchCommitsAsync(client, identity.Email, reposByProject, from, to);
        var prCreatedTask = client.GetPullRequestsByCreatorAsync(identity.Id, from, to);
        var prReviewedTask = client.GetPullRequestsByReviewerAsync(identity.Id, from, to);
        var buildTask = FetchBuildsAsync(client, identity.Email, projects, from, to);

        await Task.WhenAll(commitTask, prCreatedTask, prReviewedTask, buildTask);

        var commits = await commitTask;
        var prsCreated = await prCreatedTask;
        var prsReviewed = await prReviewedTask;
        var builds = await buildTask;

        // Combine created and reviewed PRs (deduplicate by ID)
        var allPrs = prsCreated
            .Concat(prsReviewed)
            .GroupBy(pr => pr.PullRequestId)
            .Select(g => g.First())
            .ToList();

        // Fetch PR threads for all PRs in the date range (in parallel, capped to avoid overload)
        var threadEvents = await FetchPrThreadEventsAsync(client, identity.Id, allPrs, from, to);

        // Normalize everything into WorkEvent list
        var events = new List<WorkEvent>();
        events.AddRange(EventNormalizer.FromCommits(commits));
        events.AddRange(EventNormalizer.FromPullRequests(prsCreated, identity.Id));
        events.AddRange(EventNormalizer.FromReviewerPrs(prsReviewed, identity.Id, from, to));
        events.AddRange(threadEvents);
        events.AddRange(EventNormalizer.FromBuilds(builds));

        // Deduplicate: same commit fetched from multiple repos or branches
        var deduped = events
            .GroupBy(e => e.Type == EventType.Commit
                ? $"commit:{e.Repository}:{e.Title}"
                : $"{e.Type}:{e.Repository}:{e.PullRequestId}:{e.Timestamp:yyyyMMddHHmm}")
            .Select(g => g.First())
            .ToList();

        // Filter strictly to the requested date range
        return deduped
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private static async Task<Dictionary<string, List<AzRepository>>> FetchRepositoriesAsync(
        AzureDevOpsClient client, List<AzProject> projects)
    {
        var tasks = projects.Select(async p =>
        {
            var repos = await client.GetRepositoriesAsync(p.Name);
            return (p.Name, repos);
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(r => r.Name, r => r.repos);
    }

    private static async Task<List<(AzRepository Repo, AzCommit Commit)>> FetchCommitsAsync(
        AzureDevOpsClient client,
        string authorEmail,
        Dictionary<string, List<AzRepository>> reposByProject,
        DateTimeOffset from, DateTimeOffset to)
    {
        var tasks = reposByProject
            .SelectMany(kv => kv.Value.Select(repo => new { Project = kv.Key, Repo = repo }))
            .Select(async x =>
            {
                var commits = await client.GetCommitsAsync(x.Project, x.Repo.Id, authorEmail, from, to);
                return commits.Select(c => (Repo: x.Repo, Commit: c));
            });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private static async Task<List<AzBuild>> FetchBuildsAsync(
        AzureDevOpsClient client,
        string requestedForEmail,
        List<AzProject> projects,
        DateTimeOffset from, DateTimeOffset to)
    {
        var tasks = projects.Select(p => client.GetBuildsAsync(p.Name, requestedForEmail, from, to));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private static async Task<List<WorkEvent>> FetchPrThreadEventsAsync(
        AzureDevOpsClient client,
        string userId,
        List<AzPullRequest> prs,
        DateTimeOffset from, DateTimeOffset to)
    {
        // Limit to 50 PRs to avoid excessive API calls
        var relevantPrs = prs.Take(50).ToList();

        var tasks = relevantPrs.Select(async pr =>
        {
            var threads = await client.GetPRThreadsAsync(
                pr.Repository.Project.Name,
                pr.Repository.Id,
                pr.PullRequestId);
            return EventNormalizer.FromPrThreads(threads, userId, pr, from, to);
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }
}
