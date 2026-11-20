namespace DecisionReplay.Domain.ValueObjects;

public sealed record AiTarget(string Provider, string Model);
public sealed record AiParameters(double Temperature = 0.1, int MaxOutputTokens = 2048);
public sealed record AiGenerationRequest(string SystemPrompt, string UserPrompt, string Model,
    AiParameters Parameters, bool JsonOutput = true, string? JsonSchema = null);
public sealed record AiGenerationResponse(string Content, int? InputTokens = null, int? OutputTokens = null,
    string? ModelRevision = null, bool SchemaEnforced = false);
public sealed record AiAvailability(string Status, string Provider, string Model, string? Error = null, string? ModelRevision = null);
public sealed record PromptTemplate(string Operation, string Version, string System, string User, string Hash);
public sealed record AiExecutionContext(string OwnerId, string RequestId, Guid? DecisionId = null,
    int? ContextVersion = null, Guid? ReplayId = null, Guid? ExperimentId = null);
public sealed class AiExecutionScope
{
    public AiExecutionContext Context { get; set; } = new("system", Guid.NewGuid().ToString());
}

public sealed class AiAttempt
{
    public int Number { get; set; }
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public string? ModelRevision { get; set; }
    public bool SchemaEnforced { get; set; }
    public double InferenceMs { get; set; }
    public double ValidationMs { get; set; }
    public string? ErrorCode { get; set; }
    public string? ValidationError { get; set; }
    public string? Output { get; set; }
    public string? RepairPromptVersion { get; set; }
    public string? RepairPromptHash { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}

public sealed class AiExecution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public string RequestId { get; set; } = "";
    public Guid? DecisionId { get; set; }
    public int? ContextVersion { get; set; }
    public Guid? ReplayId { get; set; }
    public Guid? ExperimentId { get; set; }
    public string Operation { get; set; } = "";
    public string PromptVersion { get; set; } = "";
    public string PromptHash { get; set; } = "";
    public string InputHash { get; set; } = "";
    public string? OutputSchemaHash { get; set; }
    public AiParameters Parameters { get; set; } = new();
    public double? RequestTimeoutSeconds { get; set; }
    public int? MaxRetries { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public double LatencyMs { get; set; }
    public double PromptConstructionMs { get; set; }
    public string Status { get; set; } = "running";
    public string? ErrorCode { get; set; }
    public int RetryCount { get; set; }
    public bool UsedFallback { get; set; }
    public List<AiAttempt> Attempts { get; set; } = new();
    public string? Output { get; set; }
}

public sealed class AiException(string code, string message, bool transient = false, Exception? inner = null)
    : Exception(message, inner)
{
    public string Code { get; } = code;
    public bool IsTransient { get; } = transient;
    public TimeSpan? RetryAfter { get; init; }
}

public sealed record ValidatedAiResult<T>(T Value, Guid ExecutionId);

public sealed class GroundedClaim
{
    [System.Text.Json.Serialization.JsonRequired] public string Text { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired] public List<string> EvidenceIds { get; set; } = new();
    [System.Text.Json.Serialization.JsonRequired] public bool IsHypothesis { get; set; }
}

public sealed class AnalysedAlternative
{
    [System.Text.Json.Serialization.JsonRequired] public string Name { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired] public string Tradeoff { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired] public List<string> EvidenceIds { get; set; } = new();
    [System.Text.Json.Serialization.JsonRequired] public bool IsHypothesis { get; set; }
}

public sealed class DecisionAiAnalysis
{
    [System.Text.Json.Serialization.JsonRequired] public List<GroundedClaim> Assumptions { get; set; } = new();
    [System.Text.Json.Serialization.JsonRequired] public List<GroundedClaim> Risks { get; set; } = new();
    [System.Text.Json.Serialization.JsonRequired] public List<string> MissingInformation { get; set; } = new();
    [System.Text.Json.Serialization.JsonRequired] public List<AnalysedAlternative> Alternatives { get; set; } = new();
    [System.Text.Json.Serialization.JsonRequired] public string Analysis { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired] public double Confidence { get; set; }
}

public sealed class OutcomeReflection
{
    [System.Text.Json.Serialization.JsonRequired] public string DecisionProcessAssessment { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired] public string OutcomeAssessment { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired] public string LuckAndUncertainty { get; set; } = "";
    [System.Text.Json.Serialization.JsonRequired] public List<string> Lessons { get; set; } = new();
}

