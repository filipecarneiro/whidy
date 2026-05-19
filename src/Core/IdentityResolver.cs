using Whidy.AzureDevOps;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class IdentityResolver
{
    public static async Task<UserIdentity> ResolveAsync(AzureDevOpsClient client)
    {
        var data = await client.GetConnectionDataAsync();
        var user = data.AuthenticatedUser;
        var email = user.Properties?.Account?.Value ?? string.Empty;

        return new UserIdentity
        {
            Id = user.Id,
            DisplayName = user.ProviderDisplayName,
            Email = email
        };
    }
}
