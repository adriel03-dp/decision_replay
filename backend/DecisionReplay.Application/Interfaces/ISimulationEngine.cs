using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

public interface ISimulationEngine
{
    SimulationResult Simulate(ProjectInput baseInput, SimulationAdjustments adjustments);
}

public class SimulationAdjustments
{
    public decimal? BudgetUsd      { get; init; }
    public double?  TimelineMonths { get; init; }
    public int?     TeamSize       { get; init; }
    public IReadOnlyList<string> AddFeatures    { get; init; } = new List<string>();
    public IReadOnlyList<string> RemoveFeatures { get; init; } = new List<string>();
}

public class SimulationResult
{
    public ProjectInput      AdjustedInput  { get; init; } = null!;
    public FeasibilityResult NewFeasibility { get; init; } = null!;
    public double            ScoreDelta     { get; init; }
    public string            VerdictDelta   { get; init; } = "";
    public IReadOnlyList<string> ImpactSummary { get; init; } = new List<string>();
}
