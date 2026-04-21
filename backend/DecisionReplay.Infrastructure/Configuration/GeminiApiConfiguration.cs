namespace DecisionReplay.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed configuration for the Gemini API.
/// Bind from appsettings.json section "GeminiApi" via IOptions&lt;GeminiApiConfiguration&gt;.
/// </summary>
public class GeminiApiConfiguration
{
    public const string SectionName = "GeminiApi";

    public string Model { get; set; } = "gemini-2.0-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 2;

    // Generation settings
    public double Temperature { get; set; } = 0.7;
    public int MaxOutputTokens { get; set; } = 2048;
    public double TopP { get; set; } = 0.95;
    public int TopK { get; set; } = 40;

    public GeminiRateLimitingConfig RateLimiting { get; set; } = new();
    public GeminiRetryPolicyConfig RetryPolicy { get; set; } = new();
}

public class GeminiRateLimitingConfig
{
    public int MaxRequestsPerMinute { get; set; } = 15;
    public int MaxConcurrentRequests { get; set; } = 4;
    public int MaxInstanceConcurrentRequests { get; set; } = 8;
}

public class GeminiRetryPolicyConfig
{
    public int BaseDelayMs { get; set; } = 1000;
    public int MaxDelayMs { get; set; } = 10000;
}
