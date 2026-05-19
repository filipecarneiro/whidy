using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Whidy.AzureDevOps.Models;

namespace Whidy.AzureDevOps;

public class AzureDevOpsClient
{
    private readonly HttpClient _http;
    private readonly string _orgUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string OrgUrl => _orgUrl;

    public AzureDevOpsClient(HttpClient http, string orgUrl, string pat)
    {
        _http = http;
        _orgUrl = orgUrl.TrimEnd('/');

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ── Connection Data (identity) ────────────────────────────────────────────

    public async Task<ConnectionDataResponse> GetConnectionDataAsync()
    {
        // connectionData is not a versioned API — no api-version parameter
        var url = $"{_orgUrl}/_apis/connectionData";
        return await GetAsync<ConnectionDataResponse>(url);
    }

    // ── Projects ──────────────────────────────────────────────────────────────

    public async Task<List<AzProject>> GetProjectsAsync()
    {
        var results = new List<AzProject>();
        string? continuationToken = null;

        do
        {
            var url = $"{_orgUrl}/_apis/projects?api-version=7.1&$top=100"
                + (continuationToken != null ? $"&continuationToken={Uri.EscapeDataString(continuationToken)}" : "");
            var (response, token) = await GetWithContinuationAsync<ProjectsResponse>(url);
            results.AddRange(response.Value);
            continuationToken = token;
        }
        while (continuationToken != null);

        return results;
    }

    // ── Repositories ──────────────────────────────────────────────────────────

    public async Task<List<AzRepository>> GetRepositoriesAsync(string projectName)
    {
        var url = $"{_orgUrl}/{Uri.EscapeDataString(projectName)}/_apis/git/repositories?api-version=7.1";
        var response = await GetAsync<RepositoriesResponse>(url);
        return response.Value;
    }

    // ── Commits ───────────────────────────────────────────────────────────────

    public async Task<List<AzCommit>> GetCommitsAsync(
        string projectName, string repoId, string authorEmail,
        DateTimeOffset from, DateTimeOffset to)
    {
        var results = new List<AzCommit>();
        int skip = 0;
        const int top = 1000;

        while (true)
        {
            var url = $"{_orgUrl}/{Uri.EscapeDataString(projectName)}/_apis/git/repositories/{Uri.EscapeDataString(repoId)}/commits"
                + $"?api-version=7.1"
                + $"&searchCriteria.authorEmail={Uri.EscapeDataString(authorEmail)}"
                + $"&searchCriteria.fromDate={Uri.EscapeDataString(from.ToString("o"))}"
                + $"&searchCriteria.toDate={Uri.EscapeDataString(to.ToString("o"))}"
                + $"&$top={top}&$skip={skip}";

            var response = await GetAsync<CommitsResponse>(url);
            results.AddRange(response.Value);

            if (response.Value.Count < top) break;
            skip += top;
        }

        return results;
    }

    // ── Pull Requests ─────────────────────────────────────────────────────────

    public async Task<List<AzPullRequest>> GetPullRequestsByCreatorAsync(
        string userId, DateTimeOffset from, DateTimeOffset to)
        => await GetPullRequestsAsync($"searchCriteria.creatorId={Uri.EscapeDataString(userId)}", from, to);

    public async Task<List<AzPullRequest>> GetPullRequestsByReviewerAsync(
        string userId, DateTimeOffset from, DateTimeOffset to)
        => await GetPullRequestsAsync($"searchCriteria.reviewerId={Uri.EscapeDataString(userId)}", from, to);

    private async Task<List<AzPullRequest>> GetPullRequestsAsync(
        string filterParam, DateTimeOffset from, DateTimeOffset to)
    {
        var results = new List<AzPullRequest>();
        int skip = 0;
        const int top = 1000;

        while (true)
        {
            var url = $"{_orgUrl}/_apis/git/pullrequests?api-version=7.1"
                + $"&{filterParam}"
                + $"&searchCriteria.minTime={Uri.EscapeDataString(from.ToString("o"))}"
                + $"&searchCriteria.maxTime={Uri.EscapeDataString(to.ToString("o"))}"
                + $"&searchCriteria.status=all"
                + $"&$top={top}&$skip={skip}";

            var response = await GetAsync<PullRequestsResponse>(url);
            results.AddRange(response.Value);

            if (response.Value.Count < top) break;
            skip += top;
        }

        return results;
    }

    // ── PR Threads ────────────────────────────────────────────────────────────

    public async Task<List<AzPRThread>> GetPRThreadsAsync(
        string projectName, string repoId, int prId)
    {
        var url = $"{_orgUrl}/{Uri.EscapeDataString(projectName)}/_apis/git/repositories/{Uri.EscapeDataString(repoId)}/pullRequests/{prId}/threads?api-version=7.1";
        var response = await GetAsync<PRThreadsResponse>(url);
        return response.Value;
    }

    // ── Builds ────────────────────────────────────────────────────────────────

    public async Task<List<AzBuild>> GetBuildsAsync(
        string projectName, string requestedForEmail,
        DateTimeOffset from, DateTimeOffset to)
    {
        var results = new List<AzBuild>();
        int skip = 0;
        const int top = 1000;

        while (true)
        {
            var url = $"{_orgUrl}/{Uri.EscapeDataString(projectName)}/_apis/build/builds?api-version=7.1"
                + $"&requestedFor={Uri.EscapeDataString(requestedForEmail)}"
                + $"&minTime={Uri.EscapeDataString(from.ToString("o"))}"
                + $"&maxTime={Uri.EscapeDataString(to.ToString("o"))}"
                + $"&statusFilter=completed"
                + $"&$top={top}&$skip={skip}";

            var response = await GetAsync<BuildsResponse>(url);
            results.AddRange(response.Value);

            if (response.Value.Count < top) break;
            skip += top;
        }

        return results;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string url)
    {
        var (result, _) = await GetWithContinuationAsync<T>(url);
        return result;
    }

    private async Task<(T Response, string? ContinuationToken)> GetWithContinuationAsync<T>(string url)
    {
        using var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new AzureDevOpsException(401,
                    "I couldn't connect to Azure DevOps with this token. Please provide a new PAT."),
                HttpStatusCode.Forbidden => new AzureDevOpsException(403,
                    "This token doesn't have the required permissions. Please create a new PAT with: Code (read), Pull Request Threads (read), Build (read), Work Items (read)."),
                HttpStatusCode.TooManyRequests => new AzureDevOpsException(429,
                    "Azure DevOps is temporarily limiting requests. Please wait a moment and try again."),
                _ => new AzureDevOpsException((int)response.StatusCode,
                    $"Azure DevOps returned an unexpected error ({(int)response.StatusCode}). Please try again.")
            };
        }

        string? continuationToken = null;
        if (response.Headers.TryGetValues("x-ms-continuationtoken", out var tokens))
            continuationToken = tokens.FirstOrDefault();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Azure DevOps API.");

        return (result, continuationToken);
    }

    public async Task ValidatePatAsync()
    {
        await GetConnectionDataAsync();
    }

    // ── Work Items ────────────────────────────────────────────────────────────

    public async Task<List<WiqlWorkItemRef>> QueryWorkItemsAsync(string wiql)
    {
        var url = $"{_orgUrl}/_apis/wit/wiql?api-version=7.1";
        var response = await PostAsync<WiqlRequest, WiqlResponse>(url, new WiqlRequest(wiql));
        return response.WorkItems ?? [];
    }

    public async Task<List<AzWorkItem>> GetWorkItemsAsync(IEnumerable<int> ids)
    {
        const string fields = "System.Title,System.State,System.WorkItemType";
        var results = new List<AzWorkItem>();

        // API allows at most 200 IDs per request
        foreach (var batch in ids.Chunk(200))
        {
            var idList = string.Join(",", batch);
            var url = $"{_orgUrl}/_apis/wit/workitems?ids={idList}&fields={Uri.EscapeDataString(fields)}&api-version=7.1";
            var response = await GetAsync<WorkItemsResponse>(url);
            results.AddRange(response.Value);
        }

        return results;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var json = JsonSerializer.Serialize(body, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            throw response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => new AzureDevOpsException(401,
                    "I couldn't connect to Azure DevOps with this token. Please provide a new PAT."),
                HttpStatusCode.Forbidden => new AzureDevOpsException(403,
                    "This token doesn't have the required permissions. Please create a new PAT with: Code (read), Pull Request Threads (read), Build (read), Work Items (read)."),
                HttpStatusCode.TooManyRequests => new AzureDevOpsException(429,
                    "Azure DevOps is temporarily limiting requests. Please wait a moment and try again."),
                _ => new AzureDevOpsException((int)response.StatusCode,
                    $"Azure DevOps returned an unexpected error ({(int)response.StatusCode}). Please try again.")
            };
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("Empty response from Azure DevOps API.");
    }
}
