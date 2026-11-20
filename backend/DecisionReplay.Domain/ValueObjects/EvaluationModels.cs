namespace DecisionReplay.Domain.ValueObjects;

public sealed class GoldenDataset
{
    public string Version { get; set; } = "v1";
    public string Hash { get; set; } = "";
    public List<GoldenDecisionCase> Cases { get; set; } = new();
}

public sealed class GoldenDecisionCase
{
    public string Id { get; set; } = "";
    public DecisionContextSnapshot Context { get; set; } = new();
    // Each group contains acceptable keywords, not an exact expected sentence.
    public List<List<string>> ExpectedMissingInformation { get; set; } = new();
    public List<List<string>> ExpectedRiskConcepts { get; set; } = new();
    public List<List<string>> ExpectedAlternativeConcepts { get; set; } = new();
}

public sealed class EvaluationMetrics
{
    public double StructuredValidity { get; set; }
    public double MissingInformationCoverage { get; set; }
    public double RiskConceptCoverage { get; set; }
    public double AlternativeConceptCoverage { get; set; }
    public double CitationIntegrity { get; set; }
    public int UncitedNonHypothesisClaims { get; set; }
    public double LatencyMs { get; set; }
    public double FailureRate { get; set; }
}

public sealed class EvaluationCaseResult
{
    public string CaseId { get; set; } = "";
    public Guid? ExecutionId { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public DecisionAiAnalysis? Output { get; set; }
    public EvaluationMetrics Metrics { get; set; } = new();
    // Deliberately separate from automated proxy metrics. No invented human/judge score.
    public string? HumanReview { get; set; }
}

public sealed class AiExperiment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = "";
    public string DatasetVersion { get; set; } = "";
    public string DatasetHash { get; set; } = "";
    public AiTarget Target { get; set; } = new("", "");
    public string PromptVersion { get; set; } = "v1";
    public string PromptHash { get; set; } = "";
    public string? OutputContractVersion { get; set; }
    public AiParameters Parameters { get; set; } = new();
    public double? RequestTimeoutSeconds { get; set; }
    public int? MaxRetries { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public string Status { get; set; } = "running";
    public List<EvaluationCaseResult> Results { get; set; } = new();
    public EvaluationMetrics Metrics { get; set; } = new();
    public decimal? ApiCost { get; set; }
}
