using System.Globalization;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.AI;

public sealed class AiConfiguration
{
    public string Provider { get; init; } = "hybrid";
    public string? ExtractionProvider { get; init; }
    public string? Model { get; init; }
    public string OllamaBaseUrl { get; init; } = "";
    public string OllamaModel { get; init; } = "";
    public string GroqBaseUrl { get; init; } = "https://api.groq.com/openai/v1";
    public string GroqModel { get; init; } = "llama-3.3-70b-versatile";
    public string? GroqApiKey { get; init; }
    public int MaxRetries { get; init; } = 1;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(120);
    public bool DeterministicFallback { get; init; } = true;
    public AiParameters Parameters { get; init; } = new();
    public AiTarget? Fallback { get; init; }

    public static AiConfiguration FromEnvironment()
    {
        string? Read(string name) => Environment.GetEnvironmentVariable(name)?.Trim();
        int Integer(string name, int fallback, int min, int max)
        {
            var value = Read(name);
            if (string.IsNullOrEmpty(value)) return fallback;
            if (!int.TryParse(value, out var result) || result < min || result > max)
                throw new InvalidOperationException($"{name} must be between {min} and {max}.");
            return result;
        }
        var failureMode = Read("AI_FAILURE_MODE") ?? "deterministic";
        if (failureMode is not ("deterministic" or "error"))
            throw new InvalidOperationException("AI_FAILURE_MODE must be deterministic or error.");
        var temperatureText = Read("AI_TEMPERATURE") ?? "0.1";
        if (!double.TryParse(temperatureText, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature)
            || !double.IsFinite(temperature) || temperature < 0 || temperature > 2)
            throw new InvalidOperationException("AI_TEMPERATURE must be between 0 and 2.");
        var fallbackProvider = Read("AI_FALLBACK_PROVIDER");
        var fallbackModel = Read("AI_FALLBACK_MODEL");
        if (string.IsNullOrWhiteSpace(fallbackProvider) != string.IsNullOrWhiteSpace(fallbackModel))
            throw new InvalidOperationException("Set both AI_FALLBACK_PROVIDER and AI_FALLBACK_MODEL, or neither.");
        return new AiConfiguration
        {
            Provider = (Read("AI_PROVIDER") ?? "hybrid").ToLowerInvariant(),
            ExtractionProvider = Read("AI_EXTRACTION_PROVIDER")?.ToLowerInvariant() is { Length: > 0 } extraction ? extraction : null,
            Model = Read("AI_MODEL") is { Length: > 0 } model ? model : null,
            OllamaBaseUrl = Read("OLLAMA_BASE_URL") ?? "",
            OllamaModel = Read("OLLAMA_MODEL") ?? "",
            GroqBaseUrl = Read("GROQ_BASE_URL") ?? "https://api.groq.com/openai/v1",
            GroqModel = Read("GROQ_MODEL") ?? "llama-3.3-70b-versatile",
            GroqApiKey = Read("GROQ_API_KEY"),
            MaxRetries = Integer("AI_MAX_RETRIES", 1, 0, 3),
            RequestTimeout = TimeSpan.FromSeconds(Integer("AI_REQUEST_TIMEOUT_SECONDS",
                Integer("OLLAMA_REQUEST_TIMEOUT_SECONDS", 120, 1, 3600), 1, 3600)),
            DeterministicFallback = failureMode == "deterministic",
            Parameters = new(temperature, Integer("AI_MAX_OUTPUT_TOKENS", 2048, 128, 8192)),
            Fallback = string.IsNullOrWhiteSpace(fallbackProvider) ? null : new(fallbackProvider!.ToLowerInvariant(), fallbackModel!)
        };
    }

    public AiTarget Target(string operation)
    {
        var provider = operation == "extraction" && ExtractionProvider != null ? ExtractionProvider
            : Provider == "hybrid" ? operation == "extraction" ? "groq" : "ollama" : Provider;
        var model = Provider == provider && Model != null ? Model : provider == "ollama" ? OllamaModel : GroqModel;
        return new(provider, model);
    }

    public void ValidateTarget(AiTarget target)
    {
        if (target.Provider is not ("groq" or "ollama"))
            throw new InvalidOperationException("AI provider must be ollama or groq (AI_PROVIDER also supports hybrid).");
        if (string.IsNullOrWhiteSpace(target.Model) || target.Model.Length > 200)
            throw new InvalidOperationException($"A valid model is required for {target.Provider}.");
        var url = target.Provider == "ollama" ? OllamaBaseUrl : GroqBaseUrl;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException($"A valid HTTP(S) base URL without embedded credentials is required for {target.Provider}.");
        if (target.Provider == "groq" && string.IsNullOrWhiteSpace(GroqApiKey))
            throw new InvalidOperationException("Groq was selected but GROQ_API_KEY is not configured. Use AI_PROVIDER=ollama for key-free operation.");
        if (target.Provider == "groq" && uri.Scheme != "https")
            throw new InvalidOperationException("GROQ_BASE_URL must use HTTPS to protect credentials.");
    }

    public void ValidateSelectedProviders()
    {
        if (Provider is not ("hybrid" or "ollama" or "groq")) throw new InvalidOperationException("Unknown AI_PROVIDER.");
        ValidateTarget(Target("extraction"));
        ValidateTarget(Target("decision-analysis"));
        if (Fallback != null) ValidateTarget(Fallback);
    }
}

public sealed class AiProviderResolver(IEnumerable<IAiProvider> providers, AiConfiguration configuration) : IAiProviderResolver
{
    private readonly Dictionary<string, IAiProvider> _providers = providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
    public IAiProvider Resolve(string provider) => _providers.TryGetValue(provider, out var result)
        ? result : throw new AiException("unknown_provider", "The requested AI provider is not registered.");
    public AiTarget DefaultTarget(string operation) => configuration.Target(operation);
    public AiTarget? FallbackTarget => configuration.Fallback;
    public int MaxRetries => configuration.MaxRetries;
    public TimeSpan RequestTimeout => configuration.RequestTimeout;
    public bool DeterministicFallback => configuration.DeterministicFallback;
    public AiParameters Parameters => configuration.Parameters;
}
