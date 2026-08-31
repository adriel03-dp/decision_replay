using System.ComponentModel.DataAnnotations;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.API.DTOs;

public sealed record AnalyzeDecisionRequest
{
    [Required, MinLength(10), MaxLength(20_000)]
    public string Input { get; init; } = string.Empty;
}

public sealed record ReplayDecisionEngineRequest
{
    [Required, MinLength(10), MaxLength(20_000)]
    public string UpdatedInput { get; init; } = string.Empty;
}

public sealed record AnalyzeStructuredDecisionRequest
{
    [Required, MaxLength(100)] public string Domain { get; init; } = "project_planning";
    [Required, MinLength(10), MaxLength(20_000)] public string Goal { get; init; } = string.Empty;
    [MaxLength(200)] public string Title { get; init; } = string.Empty;
    [Required] public Dictionary<string, string> Fields { get; init; } = new();
}

public sealed record DecisionEngineResponse(
    Guid DecisionId,
    int Version,
    string Domain,
    string Title,
    string Goal,
    string NaturalLanguageInput,
    Dictionary<string, string> StructuredFields,
    double FeasibilityScore,
    string RiskLevel,
    IReadOnlyList<FactorBreakdown> FactorBreakdown,
    IReadOnlyList<DecisionRisk> Risks,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<string> Recommendations,
    string Explanation,
    ActionPlan? Plan,
    IReadOnlyList<AuditTrailEntry> AuditTrail,
    DateTime CreatedAt,
    string RulesVersion = "1.0",
    string ScoreMeaning = "Heuristic feasibility index; not a success probability.",
    EconomicEstimate? Economics = null,
    ImprovementProposal? Improvement = null,
    IReadOnlyList<string>? ValidationWarnings = null);

public sealed record DecisionEngineSummaryResponse(
    Guid DecisionId,
    int Version,
    string Domain,
    string Title,
    double FeasibilityScore,
    string RiskLevel,
    int MissingFieldCount,
    int RiskCount,
    DateTime UpdatedAt);

public sealed record ReplayDecisionResponse(
    DecisionEngineResponse Decision,
    ReplayComparison Comparison);
