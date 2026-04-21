namespace DecisionReplay.Domain.ValueObjects;

/// <summary>
/// Output of the deterministic feasibility engine.
/// All scoring is rule-based and reproducible; no AI dependency.
/// </summary>
public class FeasibilityResult
{
    public double Score { get; init; }               // 0-100
    public string Verdict { get; init; } = "";       // Feasible | Risky | NeedsAdjustment

    // Dimension scores (0-100 each)
    public double BudgetFitScore    { get; init; }
    public double TimelineFitScore  { get; init; }
    public double TeamCapacityScore { get; init; }
    public double ComplexityScore   { get; init; }   // 100 = simple, 0 = extremely complex

    // Estimates produced by the engine
    public decimal EstimatedCostUsd    { get; init; }
    public double  EstimatedMonths     { get; init; }
    public int     RequiredTeamSize    { get; init; }

    public IReadOnlyList<FeasibilityIssue>     Issues               { get; init; } = new List<FeasibilityIssue>();
    public IReadOnlyList<SuggestedAdjustment>  SuggestedAdjustments { get; init; } = new List<SuggestedAdjustment>();
    public IReadOnlyList<string>               Explainability       { get; init; } = new List<string>();

    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public class FeasibilityIssue
{
    public string Dimension { get; init; } = "";
    public string Message   { get; init; } = "";
    public string Severity  { get; init; } = "Low";  // Low | Medium | High | Critical
}

public class SuggestedAdjustment
{
    public string  Parameter          { get; init; } = "";
    public string  Description        { get; init; } = "";
    public string? QuantitativeImpact { get; init; }
}
