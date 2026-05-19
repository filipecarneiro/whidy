using Whidy.AzureDevOps;
using Whidy.Configuration;

namespace Whidy.Setup;

public class FirstRunSetup
{
    private readonly ConfigurationLoader _loader;

    public FirstRunSetup(ConfigurationLoader loader)
        => _loader = loader;

    public async Task<UserConfig?> RunAsync(HttpClient httpClient)
    {
        Console.WriteLine();
        Console.WriteLine("Welcome to Whidy!");
        Console.WriteLine();
        Console.WriteLine("To get started, paste any Azure DevOps link — a repository, project, or pull request URL:");
        Console.Write("> ");

        var link = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(link))
        {
            Console.Error.WriteLine("No URL provided. Run 'whidy' again to retry setup.");
            return null;
        }

        var orgUrl = ExtractOrgUrl(link);
        if (orgUrl is null)
        {
            Console.Error.WriteLine("That doesn't look like a valid Azure DevOps URL. Expected something like https://dev.azure.com/my-org/...");
            return null;
        }

        Console.WriteLine();
        Console.WriteLine($"Got it. Now you'll need a Personal Access Token (PAT).");
        Console.WriteLine();
        Console.WriteLine("Open Azure DevOps → User Settings → Personal Access Tokens → New Token");
        Console.WriteLine("Required permissions: Code (read), Pull Request Threads (read), Build (read), Work Items (read)");
        Console.WriteLine();
        Console.Write("Paste your token:");
        Console.Write("\r> ");

        var pat = ReadPat();
        if (string.IsNullOrWhiteSpace(pat))
        {
            Console.Error.WriteLine("No token provided. Run 'whidy' again to retry setup.");
            return null;
        }

        // Validate immediately
        var config = new UserConfig
        {
            AzureDevOps = new AzureDevOpsConfig { Url = orgUrl, Pat = pat }
        };

        while (true)
        {
            try
            {
                var client = new AzureDevOpsClient(httpClient, orgUrl, pat);
                await client.ValidatePatAsync();
                break;
            }
            catch (AzureDevOpsException ex) when (ex.StatusCode == 401)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("That token didn't work. Please check it and try again.");
                Console.Write("> ");
                pat = ReadPat();
                if (string.IsNullOrWhiteSpace(pat))
                {
                    Console.Error.WriteLine("No token provided. Run 'whidy' again to retry setup.");
                    return null;
                }
                config = new UserConfig { AzureDevOps = new AzureDevOpsConfig { Url = orgUrl, Pat = pat } };
            }
            catch (AzureDevOpsException ex) when (ex.StatusCode == 403)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(ex.Message);
                Console.Write("> ");
                pat = ReadPat();
                if (string.IsNullOrWhiteSpace(pat))
                {
                    Console.Error.WriteLine("No token provided. Run 'whidy' again to retry setup.");
                    return null;
                }
                config = new UserConfig { AzureDevOps = new AzureDevOpsConfig { Url = orgUrl, Pat = pat } };
            }
        }

        await _loader.SaveAsync(config);
        Console.WriteLine();
        Console.WriteLine("All set. Fetching your activity...");
        Console.WriteLine();

        return config;
    }

    private static string? ExtractOrgUrl(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) return null;

        // Expected: https://dev.azure.com/org/... or https://org.visualstudio.com/...
        if (uri.Host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.TrimStart('/').Split('/');
            if (segments.Length == 0 || string.IsNullOrWhiteSpace(segments[0])) return null;
            return $"https://dev.azure.com/{segments[0]}/";
        }

        if (uri.Host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase))
            return $"https://{uri.Host}/";

        return null;
    }

    private static string ReadPat()
    {
        // Read without echoing characters to the console
        var sb = new System.Text.StringBuilder();
        Console.Write("> ");

        ConsoleKeyInfo key;
        while (true)
        {
            key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) sb.Remove(sb.Length - 1, 1);
            }
            else if (key.KeyChar != '\0')
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }
        Console.WriteLine();
        return sb.ToString();
    }
}
