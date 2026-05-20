using Whidy.AzureDevOps;
using Whidy.Core.Models;

namespace Whidy.Core;

public static class IdentityResolver
{
    public static async Task<UserIdentity> ResolveAsync(AzureDevOpsClient client)
    {
        DebugLog.Section("Identity");

        var data = await client.GetConnectionDataAsync();
        var user = data.AuthenticatedUser;

        // connectionData.properties.Account.$value is the email on Azure DevOps Services,
        // but on Azure DevOps Server on-prem it may be a domain username like "DOMAIN\user".
        var accountValue = user.Properties?.Account?.Value ?? string.Empty;
        string email;
        string emailSource;
        if (accountValue.Contains('@'))
        {
            email = accountValue;
            emailSource = "connectionData";
        }
        else
        {
            DebugLog.Write($"Account value '{accountValue}' is not an email — falling back to Identities API");
            email = await client.GetIdentityEmailAsync(user.Id);
            emailSource = string.IsNullOrWhiteSpace(email) ? "unresolved" : "Identities API";
        }

        DebugLog.Write($"Display name : {user.ProviderDisplayName}");
        DebugLog.Write($"ID           : {user.Id}");
        DebugLog.Write($"Email        : {(string.IsNullOrWhiteSpace(email) ? "(not resolved — commit/build filtering disabled)" : email)}");
        DebugLog.Write($"Email source : {emailSource}");

        return new UserIdentity
        {
            Id = user.Id,
            DisplayName = user.ProviderDisplayName,
            Email = email
        };
    }
}
