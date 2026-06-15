using DecisionReplay.Domain.Enums;

namespace DecisionReplay.Domain.ValueObjects;

public sealed class StructuredDecisionData
{
    public string Domain { get; set; } = "project_planning";
    public string Title { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public Dictionary<string, string> Fields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Assumptions { get; set; } = new();
    public List<string> MissingFields { get; set; } = new();
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    public string? Get(string field) =>
        Fields.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}

public sealed class DecisionFieldDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DecisionFieldType Type { get; init; }
    public bool Required { get; init; }
    public string? ValidationPattern { get; init; }
}

public sealed class ScoringFactorDefinition
{
    public string Name { get; init; } = string.Empty;
    public double Weight { get; init; }
    public string Evaluator { get; init; } = string.Empty;
    public IReadOnlyList<string> SourceFields { get; init; } = Array.Empty<string>();
}

public sealed class DomainRiskRule
{
    public string Factor { get; init; } = string.Empty;
    public double TriggerBelow { get; init; }
    public string Severity { get; init; } = "Medium";
    public string Message { get; init; } = string.Empty;
}

public sealed class DomainPlanTaskTemplate
{
    public string PhaseKey { get; init; } = string.Empty;
    public string TaskName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SuccessCriteria { get; init; } = string.Empty;
}

public sealed class DecisionDomainTemplate
{
    public string Domain { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public IReadOnlyList<DecisionFieldDefinition> Fields { get; init; } = Array.Empty<DecisionFieldDefinition>();
    public IReadOnlyList<ScoringFactorDefinition> ScoringFactors { get; init; } = Array.Empty<ScoringFactorDefinition>();
    public IReadOnlyList<DomainRiskRule> RiskRules { get; init; } = Array.Empty<DomainRiskRule>();
    public IReadOnlyList<string> ReplaySensitiveFields { get; init; } = Array.Empty<string>();
    public IReadOnlyList<DomainPlanTaskTemplate> PlanTasks { get; init; } = Array.Empty<DomainPlanTaskTemplate>();
}

public sealed class FactorBreakdown
{
    public string Factor { get; set; } = string.Empty;
    public double Score { get; set; }
    public double Weight { get; set; }
    public double WeightedScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Assumption { get; set; }
    public FactorConfidence Confidence { get; set; }
}

public sealed class DecisionRisk
{
    public string Code { get; set; } = string.Empty;
    public string Factor { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Message { get; set; } = string.Empty;
    public string Mitigation { get; set; } = string.Empty;
}

public sealed class FeasibilityAssessment
{
    public double FeasibilityScore { get; set; }
    public DecisionRiskLevel RiskLevel { get; set; }
    public List<FactorBreakdown> FactorBreakdown { get; set; } = new();
    public List<DecisionRisk> Risks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public string RulesVersion { get; set; } = "1.0";
}

public sealed class DecisionValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> MissingFields { get; set; } = new();
}

public sealed class AuditTrailEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AuditActionType Action { get; set; }
    public int Version { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, string> Details { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public sealed class DecisionVersion
{
    public int Version { get; set; }
    public string NaturalLanguageInput { get; set; } = string.Empty;
    public StructuredDecisionData StructuredData { get; set; } = new();
    public DecisionValidationResult Validation { get; set; } = new();
    public FeasibilityAssessment Feasibility { get; set; } = new();
    public ActionPlan? Plan { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
