using Whidy.AzureDevOps.Models;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class EventNormalizer
{
    public static IEnumerable<WorkEvent> FromCommits(
        List<(AzRepository Repo, AzCommit Commit)> commits)
    {
        return commits.Select(x =>
        {
            var raw = x.Commit.Comment.Split('\n')[0].Trim();
            return new WorkEvent
            {
                Timestamp   = x.Commit.Author.Date,
                Type        = EventType.Commit,
                Repository  = x.Repo.Name,
                ProjectName = x.Repo.Project.Name,
                Title       = StripTicketPrefix(raw),
                WorkItemId  = ExtractTicketId(raw)
            };
        });
    }

    public static IEnumerable<WorkEvent> FromPullRequests(
        List<AzPullRequest> prs, string userId)
    {
        return prs
            .Where(pr => pr.CreatedBy.Id == userId)
            .Select(pr => new WorkEvent
            {
                Timestamp     = pr.CreationDate,
                Type          = EventType.PullRequest,
                Repository    = pr.Repository.Name,
                ProjectName   = pr.Repository.Project.Name,
                Title         = StripTicketPrefix(pr.Title),
                PullRequestId = pr.PullRequestId,
                WorkItemId    = ExtractTicketId(pr.Title)
            });
    }

    public static IEnumerable<WorkEvent> FromReviewerPrs(
        List<AzPullRequest> prs, string userId,
        DateTimeOffset from, DateTimeOffset to)
    {
        foreach (var pr in prs)
        {
            var reviewer = pr.Reviewers?.FirstOrDefault(r => r.Id == userId);
            if (reviewer is null || reviewer.Vote == 0) continue;

            // Use the PR closed date as a proxy for when the approval/review happened;
            // fall back to the creation date if the PR is still open
            var timestamp = pr.ClosedDate ?? pr.CreationDate;
            if (timestamp < from || timestamp > to) continue;

            var type = reviewer.Vote >= 5 ? EventType.PrApproval : EventType.PrComment;

            yield return new WorkEvent
            {
                Timestamp = timestamp,
                Type = type,
                Repository = pr.Repository.Name,
                ProjectName = pr.Repository.Project.Name,
                Title = StripTicketPrefix(pr.Title),
                PullRequestId = pr.PullRequestId
            };
        }
    }

    public static IEnumerable<WorkEvent> FromPrThreads(
        List<AzPRThread> threads, string userId,
        AzPullRequest pr, DateTimeOffset from, DateTimeOffset to)
    {
        foreach (var thread in threads)
        {
            if (thread.Comments is null) continue;

            foreach (var comment in thread.Comments)
            {
                if (comment.Author.Id != userId) continue;
                if (comment.CommentType == "system") continue;
                if (string.IsNullOrWhiteSpace(comment.Content)) continue;
                if (comment.PublishedDate < from || comment.PublishedDate > to) continue;

                yield return new WorkEvent
                {
                    Timestamp = comment.PublishedDate,
                    Type = EventType.PrComment,
                    Repository = pr.Repository.Name,
                    ProjectName = pr.Repository.Project.Name,
                    Title = StripTicketPrefix(pr.Title),
                    PullRequestId = pr.PullRequestId
                };
            }
        }
    }

    public static IEnumerable<WorkEvent> FromBuilds(List<AzBuild> builds)
    {
        foreach (var build in builds)
        {
            if (build.FinishTime is null) continue; // skip in-progress

            var outcome = build.Result switch
            {
                "succeeded" => BuildOutcome.Succeeded,
                "failed" => BuildOutcome.Failed,
                "partiallySucceeded" => BuildOutcome.PartiallySucceeded,
                "canceled" => BuildOutcome.Canceled,
                _ => (BuildOutcome?)null
            };

            if (outcome is null) continue;

            yield return new WorkEvent
            {
                Timestamp = build.FinishTime.Value,
                Type = EventType.Build,
                Repository = build.Definition.Name,
                ProjectName = build.Project?.Name ?? string.Empty,
                Title = $"Build {build.BuildNumber} — {build.Definition.Name}",
                Outcome = outcome,
                PipelineName = build.Definition.Name
            };
        }
    }

    public static IEnumerable<WorkEvent> FromDeployments(
        IEnumerable<(string ProjectName, AzDeployment Deployment)> items, string userId)
    {
        foreach (var (projectName, d) in items)
        {
            if (!d.CompletedOn.HasValue) continue;
            if (d.RequestedFor.Id != userId) continue;

            var failed = d.DeploymentStatus?.Equals("failed", StringComparison.OrdinalIgnoreCase) == true;
            var title = failed
                ? $"Failed deployment of {d.Release.Name} to {d.ReleaseEnvironment.Name}"
                : $"Deployed {d.Release.Name} to {d.ReleaseEnvironment.Name}";

            yield return new WorkEvent
            {
                Timestamp   = d.CompletedOn.Value,
                Type        = EventType.Deployment,
                Repository  = d.ReleaseDefinition.Name,
                ProjectName = projectName,
                Title       = title,
                Outcome     = failed ? BuildOutcome.Failed : BuildOutcome.Succeeded
            };
        }
    }

    public static IEnumerable<WorkEvent> FromTestRuns(
        IEnumerable<(string ProjectName, AzTestRun Run)> items, string userId)
    {
        foreach (var (projectName, r) in items)
        {
            if (!r.CompletedDate.HasValue) continue;
            if (r.TotalTests == 0) continue;
            if (r.Owner.Id != userId) continue;

            var failed = r.TotalTests - r.PassedTests;
            var title = failed > 0
                ? $"Ran {r.TotalTests} tests — {failed} failed"
                : $"Ran {r.TotalTests} tests — all passed";

            yield return new WorkEvent
            {
                Timestamp   = r.CompletedDate.Value,
                Type        = EventType.TestRun,
                Repository  = projectName,
                ProjectName = projectName,
                Title       = title,
                FailedTests = failed
            };
        }
    }

    private static readonly System.Text.RegularExpressions.Regex TicketPrefixRegex =
        new(@"^(\[[\w\-]+\]|AB#\d+)\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex TrailingTicketRef =
        new(@"\s+(?:AB#|#)\d+\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static readonly System.Text.RegularExpressions.Regex TicketIdRegex =
        new(@"AB#(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Compiled);

    private static int? ExtractTicketId(string text)
    {
        var m = TicketIdRegex.Match(text);
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }

    public static string StripTicketPrefix(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var cleaned = TicketPrefixRegex.Replace(text.Trim(), string.Empty).Trim();
        cleaned = TrailingTicketRef.Replace(cleaned, string.Empty).Trim();
        return cleaned;
    }
}
