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

        DebugLog.Section("EventFetcher");
        DebugLog.Write($"Projects     : {projects.Count}");
        DebugLog.Write($"Repositories : {reposByProject.Values.Sum(r => r.Count)} across {reposByProject.Count} project(s)");
        DebugLog.Write($"Identity ID  : {identity.Id}");

        // Fetch all event types in parallel
        var commitTask    = FetchCommitsAsync(client, identity.Id, reposByProject, from, to);
        var prCreatedTask = client.GetPullRequestsByCreatorAsync(identity.Id, from, to);
        var prReviewedTask = client.GetPullRequestsByReviewerAsync(identity.Id, from, to);
        var buildTask      = FetchBuildsAsync(client, identity.Email, projects, from, to);
        var deploymentTask = FetchDeploymentsAsync(client, projects, from, to);
        var testRunTask    = FetchTestRunsAsync(client, projects, from, to);

        await Task.WhenAll(commitTask, prCreatedTask, prReviewedTask, buildTask, deploymentTask, testRunTask);

        var commits     = await commitTask;
        var prsCreated  = await prCreatedTask;
        var prsReviewed = await prReviewedTask;
        var builds      = await buildTask;
        var deployments = await deploymentTask;
        var testRuns    = await testRunTask;

        DebugLog.Write($"Commits (raw): {commits.Count} (filtered to pushedBy identity)");
        foreach (var (repo, commit) in commits)
            DebugLog.Write($"  {commit.Author.Date:HH:mm} [{repo.Name}] {commit.Comment.Split('\n')[0].Trim()} — {commit.Author.Name} <{commit.Author.Email}> pushed by {commit.Push?.PushedBy?.DisplayName}");

        DebugLog.Write($"PRs created  : {prsCreated.Count}");
        foreach (var pr in prsCreated)
            DebugLog.Write($"  !{pr.PullRequestId} [{pr.Status}] {pr.Title} ({pr.Repository.Name}, {pr.CreationDate:HH:mm})");

        DebugLog.Write($"PRs reviewed : {prsReviewed.Count}");
        foreach (var pr in prsReviewed)
        {
            var reviewer = pr.Reviewers?.FirstOrDefault(r => r.Id == identity.Id);
            var vote = reviewer?.Vote switch { >= 5 => "approved", <= -1 => "rejected", _ => "commented" };
            DebugLog.Write($"  !{pr.PullRequestId} [{pr.Status}] {pr.Title} ({pr.Repository.Name}) — {vote}");
        }

        DebugLog.Write($"Builds       : {builds.Count}");
        foreach (var b in builds)
            DebugLog.Write($"  #{b.Id} [{b.Result ?? b.Status}] {b.Definition.Name} — build {b.BuildNumber} ({b.FinishTime?.ToString("HH:mm") ?? "in progress"})");

        DebugLog.Write($"Deployments  : {deployments.Count}");
        foreach (var (proj, d) in deployments)
            DebugLog.Write($"  [{d.DeploymentStatus}] {d.Release.Name} → {d.ReleaseEnvironment.Name} ({proj}, {d.CompletedOn?.ToString("HH:mm") ?? "in progress"})");

        DebugLog.Write($"Test runs    : {testRuns.Count}");
        foreach (var (proj, r) in testRuns)
            DebugLog.Write($"  [{r.State}] {r.Name} — {r.PassedTests}/{r.TotalTests} passed ({proj}, {r.CompletedDate?.ToString("HH:mm") ?? "running"})");

        // Combine created and reviewed PRs (deduplicate by ID)
        var allPrs = prsCreated
            .Concat(prsReviewed)
            .GroupBy(pr => pr.PullRequestId)
            .Select(g => g.First())
            .ToList();

        // Fetch PR threads for all PRs in the date range (in parallel, capped to avoid overload)
        var threadEvents = await FetchPrThreadEventsAsync(client, identity.Id, allPrs, from, to);

        DebugLog.Write($"PR threads   : {threadEvents.Count} comment event(s) from {allPrs.Count} PR(s)");
        foreach (var ev in threadEvents)
            DebugLog.Write($"  {ev.Timestamp:HH:mm} [{ev.Repository}] !{ev.PullRequestId} {ev.Title}");

        // Normalize everything into WorkEvent list
        var events = new List<WorkEvent>();
        events.AddRange(EventNormalizer.FromCommits(commits));
        events.AddRange(EventNormalizer.FromPullRequests(prsCreated, identity.Id));
        events.AddRange(EventNormalizer.FromReviewerPrs(prsReviewed, identity.Id, from, to));
        events.AddRange(threadEvents);
        events.AddRange(EventNormalizer.FromBuilds(builds));
        events.AddRange(EventNormalizer.FromDeployments(deployments, identity.Id));
        events.AddRange(EventNormalizer.FromTestRuns(testRuns, identity.Id));

        // Deduplicate: same commit fetched from multiple repos or branches
        var deduped = events
            .GroupBy(e => e.Type == EventType.Commit
                ? $"commit:{e.Repository}:{e.Title}"
                : $"{e.Type}:{e.Repository}:{e.PullRequestId}:{e.Timestamp:yyyyMMddHHmm}")
            .Select(g => g.First())
            .ToList();

        // Filter strictly to the requested date range
        var result = deduped
            .Where(e => e.Timestamp >= from && e.Timestamp <= to)
            .OrderBy(e => e.Timestamp)
            .ToList();

        DebugLog.Write($"After dedup  : {deduped.Count} → after date filter: {result.Count}");

        return result;
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
        string pusherIdentityId,
        Dictionary<string, List<AzRepository>> reposByProject,
        DateTimeOffset from, DateTimeOffset to)
    {
        var tasks = reposByProject
            .SelectMany(kv => kv.Value.Select(repo => new { Project = kv.Key, Repo = repo }))
            .Select(async x =>
            {
                var commits = await client.GetCommitsAsync(x.Project, x.Repo.Id, from, to);
                return commits
                    .Where(c => c.Push?.PushedBy?.Id == pusherIdentityId)
                    .Select(c => (Repo: x.Repo, Commit: c));
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
        // An empty email would cause the API to return builds for all users.
        if (string.IsNullOrWhiteSpace(requestedForEmail))
            return [];

        var tasks = projects.Select(p => client.GetBuildsAsync(p.Name, requestedForEmail, from, to));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private static async Task<List<(string, AzDeployment)>> FetchDeploymentsAsync(
        AzureDevOpsClient client,
        List<AzProject> projects,
        DateTimeOffset from, DateTimeOffset to)
    {
        var tasks = projects.Select(async p =>
        {
            try
            {
                var items = await client.GetDeploymentsAsync(p.Name, from, to);
                return items.Select(d => (p.Name, d));
            }
            catch (AzureDevOpsException ex) when (ex.StatusCode is 404 or 500)
            {
                // Project doesn't have Release Management enabled — skip silently
                return Enumerable.Empty<(string, AzDeployment)>();
            }
        });
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private static async Task<List<(string, AzTestRun)>> FetchTestRunsAsync(
        AzureDevOpsClient client,
        List<AzProject> projects,
        DateTimeOffset from, DateTimeOffset to)
    {
        var tasks = projects.Select(async p =>
        {
            try
            {
                var items = await client.GetTestRunsAsync(p.Name, from, to);
                return items.Select(r => (p.Name, r));
            }
            catch (AzureDevOpsException ex) when (ex.StatusCode is 404 or 500)
            {
                // Project doesn't have Test Management enabled — skip silently
                return Enumerable.Empty<(string, AzTestRun)>();
            }
        });
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
