using System.Text.Json;

namespace Whidy.Configuration;

public class ConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string ConfigFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Whidy",
        "config.json");

    public async Task<(UserConfig Config, bool IsFirstRun)> LoadAsync()
    {
        if (!File.Exists(ConfigFilePath))
            return (new UserConfig(), true);

        try
        {
            var json = await File.ReadAllTextAsync(ConfigFilePath);
            var config = JsonSerializer.Deserialize<UserConfig>(json, JsonOptions);
            if (config is null || string.IsNullOrWhiteSpace(config.AzureDevOps.Url) || string.IsNullOrWhiteSpace(config.AzureDevOps.Pat))
                return (new UserConfig(), true);
            return (config, false);
        }
        catch
        {
            return (new UserConfig(), true);
        }
    }

    public async Task SaveAsync(UserConfig config)
    {
        var dir = Path.GetDirectoryName(ConfigFilePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(ConfigFilePath, json);
    }

    public AppSettings LoadAppSettings()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }
}
