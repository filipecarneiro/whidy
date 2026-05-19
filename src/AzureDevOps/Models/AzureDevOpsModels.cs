using System.Text.Json.Serialization;

namespace Whidy.AzureDevOps.Models;

// ── Connection Data (org-scoped identity) ────────────────────────────────────

public record ConnectionDataResponse(
    [property: JsonPropertyName("authenticatedUser")] AzAuthenticatedUser AuthenticatedUser);

public record AzAuthenticatedUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("providerDisplayName")] string ProviderDisplayName,
    [property: JsonPropertyName("properties")] AzUserProperties? Properties);

public record AzUserProperties(
    [property: JsonPropertyName("Account")] AzTypedValue? Account);

public record AzTypedValue(
    [property: JsonPropertyName("$value")] string? Value);

// ── Projects ─────────────────────────────────────────────────────────────────

public record ProjectsResponse(
    [property: JsonPropertyName("value")] List<AzProject> Value,
    [property: JsonPropertyName("count")] int Count);

public record AzProject(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

// ── Repositories ─────────────────────────────────────────────────────────────

public record RepositoriesResponse(
    [property: JsonPropertyName("value")] List<AzRepository> Value,
    [property: JsonPropertyName("count")] int Count);

public record AzRepository(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("project")] AzProject Project);

// ── Commits ───────────────────────────────────────────────────────────────────

public record CommitsResponse(
    [property: JsonPropertyName("value")] List<AzCommit> Value,
    [property: JsonPropertyName("count")] int Count);

public record AzCommit(
    [property: JsonPropertyName("commitId")] string CommitId,
    [property: JsonPropertyName("author")] AzGitUserDate Author,
    [property: JsonPropertyName("comment")] string Comment);

public record AzGitUserDate(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("date")] DateTimeOffset Date);

// ── Pull Requests ─────────────────────────────────────────────────────────────

public record PullRequestsResponse(
    [property: JsonPropertyName("value")] List<AzPullRequest> Value,
    [property: JsonPropertyName("count")] int Count);

public record AzPullRequest(
    [property: JsonPropertyName("pullRequestId")] int PullRequestId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdBy")] AzIdentityRef CreatedBy,
    [property: JsonPropertyName("creationDate")] DateTimeOffset CreationDate,
    [property: JsonPropertyName("closedDate")] DateTimeOffset? ClosedDate,
    [property: JsonPropertyName("repository")] AzRepository Repository,
    [property: JsonPropertyName("reviewers")] List<AzReviewer>? Reviewers);

public record AzIdentityRef(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("uniqueName")] string? UniqueName);

public record AzReviewer(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("uniqueName")] string? UniqueName,
    [property: JsonPropertyName("vote")] int Vote);

// ── PR Threads ────────────────────────────────────────────────────────────────

public record PRThreadsResponse(
    [property: JsonPropertyName("value")] List<AzPRThread> Value);

public record AzPRThread(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("publishedDate")] DateTimeOffset PublishedDate,
    [property: JsonPropertyName("comments")] List<AzPRComment>? Comments);

public record AzPRComment(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("publishedDate")] DateTimeOffset PublishedDate,
    [property: JsonPropertyName("author")] AzIdentityRef Author,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("commentType")] string CommentType);

// ── Work Items ────────────────────────────────────────────────────────────────────────────

public record WiqlRequest(
    [property: JsonPropertyName("query")] string Query);

public record WiqlResponse(
    [property: JsonPropertyName("workItems")] List<WiqlWorkItemRef>? WorkItems);

public record WiqlWorkItemRef(
    [property: JsonPropertyName("id")] int Id);

public record WorkItemsResponse(
    [property: JsonPropertyName("value")] List<AzWorkItem> Value);

public record AzWorkItem(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("fields")] WorkItemFields Fields);

public record WorkItemFields(
    [property: JsonPropertyName("System.Title")] string Title,
    [property: JsonPropertyName("System.State")] string State,
    [property: JsonPropertyName("System.WorkItemType")] string WorkItemType);

// ── Builds ────────────────────────────────────────────────────────────────────

public record BuildsResponse(
    [property: JsonPropertyName("value")] List<AzBuild> Value,
    [property: JsonPropertyName("count")] int Count);

public record AzBuild(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("buildNumber")] string BuildNumber,
    [property: JsonPropertyName("definition")] AzBuildDefinition Definition,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("result")] string? Result,
    [property: JsonPropertyName("finishTime")] DateTimeOffset? FinishTime,
    [property: JsonPropertyName("queueTime")] DateTimeOffset? QueueTime,
    [property: JsonPropertyName("requestedFor")] AzIdentityRef? RequestedFor,
    [property: JsonPropertyName("project")] AzProject? Project);

public record AzBuildDefinition(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

// ── Releases / Deployments ────────────────────────────────────────────────────────────

public record AzDeploymentsResponse(
    [property: JsonPropertyName("value")] List<AzDeployment> Value);

public record AzDeployment(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("release")] AzReleaseRef Release,
    [property: JsonPropertyName("releaseDefinition")] AzReleaseDefinitionRef ReleaseDefinition,
    [property: JsonPropertyName("releaseEnvironment")] AzReleaseEnvironmentRef ReleaseEnvironment,
    [property: JsonPropertyName("requestedFor")] AzIdentityRef RequestedFor,
    [property: JsonPropertyName("startedOn")] DateTimeOffset? StartedOn,
    [property: JsonPropertyName("completedOn")] DateTimeOffset? CompletedOn,
    [property: JsonPropertyName("deploymentStatus")] string DeploymentStatus);

public record AzReleaseRef(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

public record AzReleaseDefinitionRef(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

public record AzReleaseEnvironmentRef(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

// ── Test Runs ──────────────────────────────────────────────────────────────────────

public record AzTestRunsResponse(
    [property: JsonPropertyName("value")] List<AzTestRun> Value);

public record AzTestRun(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("owner")] AzIdentityRef Owner,
    [property: JsonPropertyName("startedDate")] DateTimeOffset? StartedDate,
    [property: JsonPropertyName("completedDate")] DateTimeOffset? CompletedDate,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("totalTests")] int TotalTests,
    [property: JsonPropertyName("passedTests")] int PassedTests);
