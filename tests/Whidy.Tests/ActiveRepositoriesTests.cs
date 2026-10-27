using System.Text.Json;
using Whidy.AzureDevOps.Models;
using Whidy.Core;

namespace Whidy.Tests;

public class ActiveRepositoriesTests
{
    private static readonly AzProject Project = new("p1", "Project");

    private static AzRepository Repo(string name, bool? disabled)
        => new($"id-{name}", name, Project, disabled);

    [Fact]
    public void ActiveRepositories_DropsDisabledOnly()
    {
        var repos = new[] { Repo("on", false), Repo("off", true), Repo("unknown", null) };

        var active = EventFetcher.ActiveRepositories(repos);

        Assert.Equal(["on", "unknown"], active.Select(r => r.Name));
    }

    [Fact]
    public void Deserialize_ReadsIsDisabledAndToleratesItsAbsence()
    {
        const string json = """
            [
              {"id":"1","name":"a","project":{"id":"p","name":"P"},"isDisabled":true},
              {"id":"2","name":"b","project":{"id":"p","name":"P"}}
            ]
            """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var repos = JsonSerializer.Deserialize<List<AzRepository>>(json, options)!;

        Assert.True(repos[0].IsDisabled);
        Assert.Null(repos[1].IsDisabled);
    }
}
