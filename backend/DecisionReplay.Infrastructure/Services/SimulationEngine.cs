using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Pure re-scoring engine that applies parameter adjustments to a base
/// ProjectInput and returns the feasibility delta. No AI dependency.
/// </summary>
public class SimulationEngine : ISimulationEngine
{
    private readonly IFeasibilityEngine _feasibility;

    public SimulationEngine(IFeasibilityEngine feasibility)
    {
        _feasibility = feasibility;
    }

    public SimulationResult Simulate(ProjectInput baseInput, SimulationAdjustments adjustments)
    {
        var adjustedFeatures = baseInput.Features
            .Concat(adjustments.AddFeatures ?? Enumerable.Empty<string>())
            .Where(f => !(adjustments.RemoveFeatures ?? Enumerable.Empty<string>())
                .Any(r => string.Equals(r, f, StringComparison.OrdinalIgnoreCase)))
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList();

        var adjustedInput = new ProjectInput(
            baseInput.ProjectType,
            adjustedFeatures,
            adjustments.BudgetUsd      ?? baseInput.BudgetUsd,
            adjustments.TimelineMonths ?? baseInput.TimelineMonths,
            adjustments.TeamSize       ?? baseInput.TeamSize,
            baseInput.RawInput,
            isStructured: true);

        var baseFeasibility    = _feasibility.Evaluate(baseInput);
        var adjustedFeasibility = _feasibility.Evaluate(adjustedInput);

        var scoreDelta  = Math.Round(adjustedFeasibility.Score - baseFeasibility.Score, 1);
        var verdictDelta = (baseFeasibility.Verdict == adjustedFeasibility.Verdict)
            ? $"No change ({adjustedFeasibility.Verdict})"
            : $"{baseFeasibility.Verdict} → {adjustedFeasibility.Verdict}";

        var impact = BuildImpactSummary(baseInput, adjustedInput, baseFeasibility, adjustedFeasibility, adjustments);

        return new SimulationResult
        {
            AdjustedInput   = adjustedInput,
            NewFeasibility  = adjustedFeasibility,
            ScoreDelta      = scoreDelta,
            VerdictDelta    = verdictDelta,
            ImpactSummary   = impact,
        };
    }

    private static List<string> BuildImpactSummary(
        ProjectInput orig, ProjectInput adj,
        FeasibilityResult origF, FeasibilityResult adjF,
        SimulationAdjustments changes)
    {
        var lines = new List<string>();

        if (orig.BudgetUsd != adj.BudgetUsd)
            lines.Add($"Budget changed from ${orig.BudgetUsd:N0} to ${adj.BudgetUsd:N0}; " +
                      $"budget-fit score: {origF.BudgetFitScore:F0} → {adjF.BudgetFitScore:F0}");

        if (orig.TimelineMonths != adj.TimelineMonths)
            lines.Add($"Timeline changed from {orig.TimelineMonths} to {adj.TimelineMonths} months; " +
                      $"timeline-fit score: {origF.TimelineFitScore:F0} → {adjF.TimelineFitScore:F0}");

        if (orig.TeamSize != adj.TeamSize)
            lines.Add($"Team size changed from {orig.TeamSize} to {adj.TeamSize}; " +
                      $"team-capacity score: {origF.TeamCapacityScore:F0} → {adjF.TeamCapacityScore:F0}");

        if (changes.AddFeatures?.Any() == true)
            lines.Add($"Added features: {string.Join(", ", changes.AddFeatures)}");

        if (changes.RemoveFeatures?.Any() == true)
            lines.Add($"Removed features: {string.Join(", ", changes.RemoveFeatures)}");

        var scoreDir = adjF.Score >= origF.Score ? "improved" : "declined";
        lines.Add($"Overall score {scoreDir} by {Math.Abs(adjF.Score - origF.Score):F1} points " +
                  $"({origF.Score:F0} → {adjF.Score:F0}).");

        return lines;
    }
}
