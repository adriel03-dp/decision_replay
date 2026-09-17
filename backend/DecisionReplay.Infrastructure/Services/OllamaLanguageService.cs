using System.Net.Http.Json;
using System.Text.Json;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>Optional wording only; numeric assessment and plan structure remain backend-owned.</summary>
public sealed class OllamaLanguageService
{
    private const string AccuracyInstruction = """
        Optimize for maximum decision accuracy from the supplied information.
        Treat missing or ambiguous inputs as explicit uncertainty, not permission to invent facts.
        Use director-level judgment: concise, strategic, commercially aware, and execution-focused.
        The output should help the user improve the plan toward the strongest feasible version.
        """;

    // Shared across request scopes: at most one inference from this API process at a time.
    private static readonly SemaphoreSlim InferenceGate = new(1, 1);
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLanguageService> _logger;
    private readonly string? _baseUrl;
    private readonly string? _model;
    private readonly TimeSpan _timeout;

    public OllamaLanguageService(IHttpClientFactory factory, ILogger<OllamaLanguageService> logger)
    {
        _httpClient = factory.CreateClient(nameof(OllamaLanguageService));
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        _logger = logger;
        _baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL")?.Trim().TrimEnd('/');
        _model = Environment.GetEnvironmentVariable("OLLAMA_MODEL")?.Trim();
        var timeout = Environment.GetEnvironmentVariable("OLLAMA_REQUEST_TIMEOUT_SECONDS") ?? "120";
        if (!double.TryParse(timeout, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds)
            || !double.IsFinite(seconds) || seconds <= 0 || seconds > 3600)
            throw new InvalidOperationException("OLLAMA_REQUEST_TIMEOUT_SECONDS must be between 0 and 3600 seconds (exclusive of 0).");
        _timeout = TimeSpan.FromSeconds(seconds);
        if (!string.IsNullOrWhiteSpace(_baseUrl)
            && (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var uri)
                || (uri.Scheme != "http" && uri.Scheme != "https")))
            throw new InvalidOperationException("OLLAMA_BASE_URL must be an absolute HTTP or HTTPS URL.");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrWhiteSpace(_model);

    public async Task<OllamaAvailability> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new("not_configured", _model, "Set OLLAMA_BASE_URL and OLLAMA_MODEL.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        try
        {
            await VerifyModelAsync(timeout.Token);
            return new("healthy", _model, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OllamaException ex) { return new(ex.Code, _model, ex.Message); }
        catch (OperationCanceledException) { return new("timeout", _model, "Ollama availability check timed out."); }
        catch (HttpRequestException) { return new("runtime_unavailable", _model, "Cannot reach the configured Ollama runtime."); }
        catch (JsonException) { return new("invalid_response", _model, "Ollama returned an invalid model list."); }
    }

    private async Task VerifyModelAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new OllamaException("runtime_unavailable", $"Ollama model listing returned HTTP {(int)response.StatusCode}.");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
            throw new OllamaException("invalid_response", "Ollama returned an invalid model list.");
        if (!models.EnumerateArray().Any(model => model.ValueKind == JsonValueKind.Object &&
                ((model.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String && name.GetString() == _model)
                || (model.TryGetProperty("model", out var tag) && tag.ValueKind == JsonValueKind.String && tag.GetString() == _model))))
            throw new OllamaException("missing_model", $"Configured Ollama model '{_model}' is not available in the existing runtime.");
    }
    public async Task<string> ExplainResultAsync(
        StructuredDecisionData decision,
        FeasibilityAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return string.Empty;

        var immutableResult = new
        {
            decision.Domain,
            decision.Title,
            decision.Goal,
            assessment.FeasibilityScore,
            riskLevel = assessment.RiskLevel.ToString(),
            factors = assessment.FactorBreakdown.Select(factor => new
            {
                factor.Factor,
                factor.Score,
                factor.Weight,
                factor.WeightedScore,
                factor.Reason,
                factor.Assumption,
                confidence = factor.Confidence.ToString()
            }),
            risks = assessment.Risks,
            improvementAdvice = assessment.Recommendations
        };

        var prompt = $$"""
            {{AccuracyInstruction}}

            Write a director-level plan improvement brief from this immutable decision result:
            {{JsonSerializer.Serialize(immutableResult)}}

            Required structure:
            1. Start with the current plan grade and what it means in one direct sentence.
            2. Explain the top weaknesses holding the plan back.
            3. Translate the supplied improvement advice into specific next moves.
            4. End with what would most improve feasibility in the next version.

            Accuracy rules:
            - Preserve every numeric score, weight, risk level, and recommendation exactly.
            - Explain why the weakest factors constrain the plan.
            - Clearly label assumptions and missing evidence.
            - Do not add new scores, new risks, unsupported financial claims, or invented facts.
            - Do not guarantee success or claim the plan is 100% certain.
            - Use 3 to 5 concise paragraphs with practical, executive wording.
            """;

        try
        {
            return await SendAsync(
                "You are a director-level decision advisor. Improve user understanding without changing supplied calculations.",
                prompt,
                jsonMode: false,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OLLAMA] Result explanation failed");
            return string.Empty;
        }
    }

    public async Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(
        StructuredDecisionData decision,
        FeasibilityAssessment assessment,
        ActionPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return Array.Empty<PlanTaskEnhancement>();

        var immutablePlan = new
        {
            decision.Domain,
            decision.Goal,
            decision.Fields,
            score = assessment.FeasibilityScore,
            riskLevel = assessment.RiskLevel.ToString(),
            risks = assessment.Risks.Select(risk => new { risk.Factor, risk.Message, risk.Mitigation }),
            plan.TimelineMonths,
            plan.StartDate,
            plan.EndDate,
            tasks = plan.Phases.SelectMany(phase => phase.Tasks.Select(task => new
            {
                task.TaskId,
                phase = phase.PhaseName,
                phase.StartWeek,
                phase.EndWeek,
                task.TaskName,
                task.Description,
                task.SuccessCriteria
            }))
        };

        var prompt = $$"""
            {{AccuracyInstruction}}

            Upgrade the wording for this existing action plan into a director-level execution roadmap:
            {{JsonSerializer.Serialize(immutablePlan)}}

            Return JSON only:
            {
              "tasks": [
                {
                  "taskId": "existing task GUID",
                  "description": "specific practical wording",
                  "riskMitigation": "practical mitigation tied to supplied risks"
                }
              ]
            }

            Restrictions:
            - Return exactly one entry per supplied taskId.
            - Do not create, remove, reorder, or rename tasks or phases.
            - Do not change dates, weeks, effort, dependencies, scores, or success criteria.
            - Do not invent unsupported financial or market claims.
            - Each description must state the exact action, intended output, and what a strong version looks like.
            - Each riskMitigation must state the flaw it addresses and the practical control to reduce it.
            - Keep each field below 450 characters.
            """;

        try
        {
            var content = await SendAsync(
                "You are a director-level execution planning advisor. Strengthen task wording without changing plan structure.",
                prompt,
                jsonMode: true,
                cancellationToken);
            var parsed = JsonSerializer.Deserialize<PlanEnhancementResponse>(content, JsonOptions());
            return parsed?.Tasks ?? new List<PlanTaskEnhancement>();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OLLAMA] Plan wording enhancement failed");
            return Array.Empty<PlanTaskEnhancement>();
        }
    }

    public async Task<string> SummarizeReplayAsync(
        ReplayComparison comparison,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return string.Empty;

        var prompt = $$"""
            {{AccuracyInstruction}}

            Summarize this immutable plan replay comparison in two director-level sentences:
            {{JsonSerializer.Serialize(comparison)}}

            Preserve all values. Explain whether the revised plan became stronger or weaker and what most likely drove the change.
            Do not add changes, scores, risks, recommendations, or unsupported claims.
            """;
        try
        {
            return await SendAsync(
                "You are a director-level plan improvement analyst. Summarize replay differences without changing the data.",
                prompt,
                jsonMode: false,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OLLAMA] Replay summary failed");
            return string.Empty;
        }
    }

    private async Task<string> SendAsync(string systemPrompt, string userPrompt, bool jsonMode,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        var entered = false;
        try
        {
            await InferenceGate.WaitAsync(timeout.Token);
            entered = true;
            await VerifyModelAsync(timeout.Token);
            var payload = new Dictionary<string, object>
            {
                ["model"] = _model!,
                ["messages"] = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                ["stream"] = false,
                ["options"] = new { temperature = .1 }
            };
            if (jsonMode) payload["format"] = "json";
            using var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/chat", payload, timeout.Token);
            if (!response.IsSuccessStatusCode)
                throw new OllamaException("inference_error", $"Ollama inference returned HTTP {(int)response.StatusCode}.");
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new OllamaException("inference_error", "Ollama returned an invalid message envelope.");
            if (document.RootElement.TryGetProperty("error", out _))
                throw new OllamaException("inference_error", "Ollama reported an inference error.");
            if (!document.RootElement.TryGetProperty("message", out var message)
                || message.ValueKind != JsonValueKind.Object
                || !message.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(content.GetString()))
                throw new OllamaException("inference_error", "Ollama returned no message content.");
            return content.GetString()!.Trim();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex) { throw new OllamaException("timeout", "Ollama request timed out, including queue wait.", ex); }
        catch (HttpRequestException ex) { throw new OllamaException("runtime_unavailable", "Cannot reach the configured Ollama runtime.", ex); }
        catch (JsonException ex) { throw new OllamaException("inference_error", "Ollama returned invalid JSON.", ex); }
        finally { if (entered) InferenceGate.Release(); }
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };
    private sealed class PlanEnhancementResponse
    {
        public List<PlanTaskEnhancement> Tasks { get; init; } = new();
    }
}

public sealed record OllamaAvailability(string Status, string? Model, string? Error);

public sealed class OllamaException : Exception
{
    public string Code { get; }
    public OllamaException(string code, string message, Exception? inner = null) : base(message, inner) => Code = code;
}
