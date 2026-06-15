using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

/// <summary>
/// Adds qualitative risk analysis and an optimized delivery plan to the
/// deterministic feasibility result.
/// </summary>
public interface IAiDecisionAnalyzer
{
    bool IsConfigured { get; }

    Task<DecisionAnalysis?> AnalyzeAsync(
        ProjectInput input,
        FeasibilityResult feasibility,
        ProjectPlan plan,
        CancellationToken cancellationToken = default);
}
