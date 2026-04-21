using System.ComponentModel.DataAnnotations;

namespace DecisionReplay.API.DTOs;

// ── Requests ─────────────────────────────────────────────────────────────────

public record ProjectEvaluationRequest
{
    [Required]
    public string ProjectType { get; init; } = "General";

    [Required, MinLength(1)]
    public List<string> Features { get; init; } = new();

    [Range(0, double.MaxValue)]
    public decimal BudgetUsd { get; init; }

    [Range(0.25, 120)]
    public double TimelineMonths { get; init; }

    [Range(1, 500)]
    public int TeamSize { get; init; } = 1;

    public string? RawInput { get; init; }
    public bool RequestAiEnhancement { get; init; } = true;
}

public record SimulationRequest
{
    [Required]
    public ProjectEvaluationRequest BaseProject { get; init; } = null!;
    public decimal? BudgetUsd { get; init; }
    public double? TimelineMonths { get; init; }
    public int? TeamSize { get; init; }
    public List<string> AddFeatures { get; init; } = new();
    public List<string> RemoveFeatures { get; init; } = new();
}

// ── Core response DTOs ────────────────────────────────────────────────────────

public record ProjectEvaluationResponse(
    ProjectInputDto Input,
    FeasibilityResultDto Feasibility,
    ProjectPlanDto Plan,
    AiEnhancementDto? AiEnhancement,
    bool AiAvailable,
    DateTime GeneratedAt
);

public record ProjectInputDto(
    string ProjectType,
    List<string> Features,
    decimal BudgetUsd,
    double TimelineMonths,
    int TeamSize,
    bool IsStructured,
    string? RawInput
);

public record FeasibilityResultDto(
    double Score,
    string Verdict,
    double BudgetFitScore,
    double TimelineFitScore,
    double TeamCapacityScore,
    double ComplexityScore,
    decimal EstimatedCostUsd,
    double EstimatedMonths,
    int RequiredTeamSize,
    List<FeasibilityIssueDto> Issues,
    List<SuggestedAdjustmentDto> SuggestedAdjustments,
    List<string> Explainability,
    DateTime GeneratedAt
);

public record FeasibilityIssueDto(string Dimension, string Message, string Severity);

public record SuggestedAdjustmentDto(string Parameter, string Description, string? QuantitativeImpact);

public record ProjectPlanDto(
    double TotalMonths,
    List<TimelinePhaseDto> Phases,
    List<string> Milestones,
    DateTime GeneratedAt
);

public record TimelinePhaseDto(
    string Name,
    double StartMonth,
    double EndMonth,
    double DurationMonths,
    List<string> Tasks,
    List<string> Deliverables,
    double PercentageOfTotal
);

public record AiEnhancementDto(
    string ExecutiveSummary,
    List<string> Recommendations,
    List<string> Risks,
    List<string> Pros,
    List<string> Cons,
    double ConfidenceLevel,
    string ModelUsed,
    DateTime GeneratedAt
);

// ── Composite / list responses ────────────────────────────────────────────────

public record SavedDecisionResponse(
    Guid Id,
    ProjectEvaluationResponse? Analysis,
    DateTime CreatedAt
);

public record DecisionSummaryResponse(
    Guid Id,
    string ProjectType,
    List<string> Features,
    double? FeasibilityScore,
    string? Verdict,
    string Status,
    DateTime CreatedAt
);

public record SimulationResponse(
    ProjectInputDto AdjustedInput,
    FeasibilityResultDto NewFeasibility,
    double ScoreDelta,
    string VerdictDelta,
    List<string> ImpactSummary
);
