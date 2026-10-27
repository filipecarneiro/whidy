using System.Net;
using Whidy.AzureDevOps;

namespace Whidy.Tests;

public class HttpErrorMessageTests
{
    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(status) { RequestMessage = request });
    }

    private static async Task<AzureDevOpsException> FailWith(HttpStatusCode status)
    {
        var client = new AzureDevOpsClient(new HttpClient(new StatusHandler(status)), "https://dev.azure.com/org", "token");
        var from = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

        return await Assert.ThrowsAsync<AzureDevOpsException>(
            () => client.GetTestRunsAsync("proj", from, from.AddDays(1)));
    }

    [Fact]
    public async Task Unauthorized_NamesEndpointAndAllRequiredScopes()
    {
        var ex = await FailWith(HttpStatusCode.Unauthorized);

        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("/proj/_apis/test/runs", ex.Message);
        Assert.Contains("Test Management (read)", ex.Message);
        Assert.Contains("expired", ex.Message);
    }

    [Fact]
    public async Task Unauthorized_DoesNotLeakQueryString()
    {
        var ex = await FailWith(HttpStatusCode.Unauthorized);

        Assert.DoesNotContain("minLastUpdatedDate", ex.Message);
    }

    [Fact]
    public async Task Forbidden_ListsAllRequiredScopes()
    {
        var ex = await FailWith(HttpStatusCode.Forbidden);

        Assert.Equal(403, ex.StatusCode);
        Assert.Contains(AzureDevOpsClient.RequiredScopes, ex.Message);
    }

    [Fact]
    public async Task OtherStatus_NamesStatusAndEndpoint()
    {
        var ex = await FailWith(HttpStatusCode.InternalServerError);

        Assert.Equal(500, ex.StatusCode);
        Assert.Contains("(500)", ex.Message);
        Assert.Contains("/proj/_apis/test/runs", ex.Message);
    }
}
