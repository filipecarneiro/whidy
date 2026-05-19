using System.Text.Json;
using System.Text.Json.Serialization;

namespace Whidy.Configuration;

public class UserConfig
{
    [JsonPropertyName("azureDevOps")]
    public AzureDevOpsConfig AzureDevOps { get; set; } = new();
}

public class AzureDevOpsConfig
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("pat")]
    public string Pat { get; set; } = string.Empty;
}

public class AppSettings
{
    [JsonPropertyName("episodeWindowMinutes")]
    public int EpisodeWindowMinutes { get; set; } = 90;

    [JsonPropertyName("insights")]
    public InsightSettings Insights { get; set; } = new();
}

public class InsightSettings
{
    [JsonPropertyName("focusDetectionThreshold")]
    public double FocusDetectionThreshold { get; set; } = 0.6;

    [JsonPropertyName("contextSwitchingEpisodeThreshold")]
    public int ContextSwitchingEpisodeThreshold { get; set; } = 4;

    [JsonPropertyName("workTypeBalanceThreshold")]
    public double WorkTypeBalanceThreshold { get; set; } = 0.6;

    [JsonPropertyName("intensitySpikeEventCount")]
    public int IntensitySpikeEventCount { get; set; } = 5;

    [JsonPropertyName("intensitySpikeWindowMinutes")]
    public int IntensitySpikeWindowMinutes { get; set; } = 30;

    [JsonPropertyName("failureHeavyDayThreshold")]
    public double FailureHeavyDayThreshold { get; set; } = 0.5;

    [JsonPropertyName("debuggingBuildRetriggerThreshold")]
    public int DebuggingBuildRetriggerThreshold { get; set; } = 2;
}
