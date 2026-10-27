using System.Net;
using System.Text;
using System.Web;
using Whidy.AzureDevOps;

namespace Whidy.Tests;

public class TestRunsWindowingTests
{
    private sealed class RecordingHandler(Func<HttpRequestMessage, string> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(respond(request), Encoding.UTF8, "application/json")
            });
        }
    }

    private static string Run(int id)
        => $$"""{"id":{{id}},"name":"r{{id}}","owner":{"id":"u","displayName":"U"},"state":"Completed","totalTests":1,"passedTests":1}""";

    private static (DateTime Min, DateTime Max) Window(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        return (DateTime.Parse(query["minLastUpdatedDate"]!).ToUniversalTime(),
                DateTime.Parse(query["maxLastUpdatedDate"]!).ToUniversalTime());
    }

    [Fact]
    public async Task GetTestRunsAsync_LongRange_RequestsWindowsOfAtMostSevenDays()
    {
        var handler = new RecordingHandler(_ => """{"value":[]}""");
        var client = new AzureDevOpsClient(new HttpClient(handler), "https://dev.azure.com/org", "token");
        var from = new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

        await client.GetTestRunsAsync("proj", from, from.AddDays(8).AddHours(23));

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, uri =>
        {
            var (min, max) = Window(uri);
            Assert.True(max - min <= TimeSpan.FromDays(7));
        });
    }

    [Fact]
    public async Task GetTestRunsAsync_RunInTwoWindows_IsReturnedOnce()
    {
        var handler = new RecordingHandler(_ => $$"""{"value":[{{Run(1)}},{{Run(2)}}]}""");
        var client = new AzureDevOpsClient(new HttpClient(handler), "https://dev.azure.com/org", "token");
        var from = new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);

        var runs = await client.GetTestRunsAsync("proj", from, from.AddDays(9));

        Assert.Equal([1, 2], runs.Select(r => r.Id).OrderBy(i => i));
    }

    [Fact]
    public async Task GetTestRunsAsync_ShortRange_MakesOneRequest()
    {
        var handler = new RecordingHandler(_ => """{"value":[]}""");
        var client = new AzureDevOpsClient(new HttpClient(handler), "https://dev.azure.com/org", "token");
        var from = new DateTimeOffset(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);

        await client.GetTestRunsAsync("proj", from, from.AddDays(1));

        Assert.Single(handler.Requests);
    }
}
