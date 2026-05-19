using Whidy.AzureDevOps;
using Whidy.Commands;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class WorkItemFetcher
{
    public static async Task<List<WorkItemSummary>> FetchAsync(
        AzureDevOpsClient client, UserIdentity identity, DateRange dateRange)
    {
        var from = dateRange.Start.ToString("yyyy-MM-dd");
        var to   = dateRange.End.ToString("yyyy-MM-dd");

        var wiql = $"""
            SELECT [System.Id]
            FROM WorkItems
            WHERE [System.AssignedTo] = '{identity.Email}'
            AND [System.ChangedDate] >= '{from}'
            AND [System.ChangedDate] <= '{to}'
            ORDER BY [System.ChangedDate] DESC
            """;

        var refs = await client.QueryWorkItemsAsync(wiql);
        if (refs.Count == 0) return [];

        var items = await client.GetWorkItemsAsync(refs.Select(r => r.Id));
        return items
            .Select(i => new WorkItemSummary(
                i.Id,
                i.Fields.Title,
                i.Fields.State,
                i.Fields.WorkItemType,
                $"{client.OrgUrl}/_workitems/edit/{i.Id}"))
            .ToList();
    }
}
