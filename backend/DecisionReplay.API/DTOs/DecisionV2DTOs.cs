namespace DecisionReplay.API.DTOs;

/// <summary>
/// Request DTO for natural language decision creation
/// SOLID: Single Responsibility - Only represents request data
/// Clean Architecture: API layer DTO
/// </summary>
public record NaturalLanguageDecisionRequest(
    string Input,      // Free-form natural language input
    string CreatedBy
)
{
    public bool AnalyzeNow { get; init; } = true;
}

/// <summary>
/// Response DTO for decision with full context
/// </summary>
public record DecisionV2Response(
    Guid Id,
    string NaturalLanguageInput,
    Dictionary<string, object> InferredAttributes,
    string? DomainType,
    string Status,
    string Outcome,
    DateTime CreatedAt,
    DateTime? LastModifiedAt,
    string CreatedBy,
    AnalysisResponse? Analysis = null,
    double? FeasibilityScore = null
);

/// <summary>
/// Response DTO for decision analysis
/// </summary>
public record AnalysisResponse(
    Guid AnalysisId,
    double FeasibilityScore,
    string FeasibilityVerdict,
    string ExecutiveSummary,
    CurrentPlanAnalysisResponse? CurrentPlanAnalysis,
    List<string> Pros,
    List<string> Cons,
    OptimizedSolutionResponse? OptimizedSolution,
    List<string> OptimizedPros,
    List<string> OptimizedCons,
    List<RiskResponse> Risks,
    List<string> Assumptions,
    List<string> Recommendations,
    double ConfidenceLevel,
    DateTime GeneratedAt,
    string ModelUsed,
    ChartDataResponse? ChartData
);

public record CurrentPlanAnalysisResponse(
    string TimelineAssessment,
    string ScopeAssessment,
    string BudgetAssessment,
    string ResourceAssessment
);

public record OptimizedSolutionResponse(
    string ImprovedTimeline,
    string ClarifiedScope,
    string BudgetOptimization,
    string ResourceStrategy,
    double SuccessProbability
);

public record RiskResponse(
    string Description,
    string Impact,
    string? Mitigation
);

/// <summary>
/// Response DTO for decision schema
/// </summary>
public record SchemaResponse(
    Guid Id,
    string DomainType,
    Dictionary<string, string> Fields,
    DateTime GeneratedAt
);

/// <summary>
/// Response DTO for replay result
/// </summary>
public record ReplayResponse(
    Guid ReplayId,
    DateTime ReplayedAt,
    List<ChangeResponse> Changes,
    bool HasSignificantChanges,
    AnalysisResponse? OriginalAnalysis,
    AnalysisResponse? UpdatedAnalysis,
    double FeasibilityDelta,
    string ImpactSummary,
    Dictionary<string, object> VisualizationData
);

public record ChangeResponse(
    string Field,
    string? OldValue,
    string? NewValue,
    bool IsSignificant,
    string ChangeType
);

/// <summary>
/// Chart data for frontend visualization
/// </summary>
public record ChartDataResponse(
    List<FeasibilityTimelineData> Timeline,
    List<ResourceAllocationData> Performance,
    List<PlanningFactorData> RiskHeatmap
);

public record FeasibilityTimelineData(
    string Time,
    double FeasibilityScore,
    double TimelinePressure,
    double ResourceAdequacy,
    double ScopeComplexity
);

public record ResourceAllocationData(
    string Resource,
    double Allocated,
    double Required,
    double Gap
);

public record PlanningFactorData(
    string Factor,
    double Impact,
    string Status,
    string Trend,
    string? Description
);

/// <summary>
/// Request for replaying a decision with updated input
/// </summary>
public record ReplayDecisionRequest(
    string UpdatedInput
);

/// <summary>
/// Request DTO for updating a decision
/// Supports updating context (natural language input) and status
/// </summary>
public record UpdateDecisionV2Request(
    string? UpdatedInput,  // New natural language input (triggers context update)
    string? Status         // New status: Draft, InReview, Finalized
);
