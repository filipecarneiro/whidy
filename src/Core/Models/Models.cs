namespace Whidy.Core.Models;

public enum EventType
{
    Commit,
    PullRequest,
    PrComment,
    PrApproval,
    Build,
    Deployment,
    TestRun
}

public enum BuildOutcome
{
    Succeeded,
    Failed,
    PartiallySucceeded,
    Canceled
}

public class WorkEvent
{
    public DateTimeOffset Timestamp { get; init; }
    public EventType Type { get; init; }
    public string Repository { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    /// Only set for Build events.
    public BuildOutcome? Outcome { get; init; }

    /// Pipeline name for build events (used for retrigger detection).
    public string? PipelineName { get; init; }

    /// PR id for PR-related events.
    public int? PullRequestId { get; init; }

    /// Work item ID referenced by this event (extracted from AB#NNN before title stripping).
    public int? WorkItemId { get; init; }

    /// Failed test count for TestRun events.
    public int? FailedTests { get; init; }
}

public enum EpisodeType
{
    Coding,
    Review,
    Debugging
}

public class Episode
{
    public string Repository { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public List<WorkEvent> Events { get; init; } = [];
    public EpisodeType Type { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class Insight
{
    public string Sentence { get; init; } = string.Empty;
    public string Rule { get; init; } = string.Empty;
}

public class UserIdentity
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public record WorkItemSummary(
    int Id,
    string Title,
    string State,
    string WorkItemType,
    string Url);
