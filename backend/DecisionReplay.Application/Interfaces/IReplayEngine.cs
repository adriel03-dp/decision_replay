using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

/// <summary>
/// Interface: Decision Replay Engine
/// 
/// SOLID Principles:
/// - Single Responsibility: Handles decision re-evaluation and change detection
/// - Interface Segregation: Focused on replay functionality
/// - Dependency Inversion: Abstracts replay logic
/// 
/// Purpose:
/// Enables "Decision Replay" - re-evaluating decisions when parameters change.
/// Tracks what changed, why outcomes differ, and provides visualization data.
/// 
/// This is a core differentiator of the system.
/// </summary>
public interface IReplayEngine
{
    /// <summary>
    /// Compares two decision contexts and identifies changes
    /// </summary>
    Task<DecisionReplayResult> CompareContextsAsync(
        DecisionContext original,
        DecisionContext updated);

    /// <summary>
    /// Performs full replay: re-analyzes with new context and compares results
    /// </summary>
    Task<DecisionReplayResult> ReplayDecisionAsync(
        Guid decisionId,
        DecisionContext originalContext,
        DecisionAnalysis originalAnalysis,
        DecisionContext updatedContext);
}

/// <summary>
/// Result of a decision replay operation
/// Contains change detection and comparison data
/// </summary>
public class DecisionReplayResult
{
    public Guid ReplayId { get; private set; }
    public DateTime ReplayedAt { get; private set; }

    // Change detection
    public List<ContextChange> Changes { get; private set; }
    public bool HasSignificantChanges { get; private set; }

    // Analysis comparison
    public DecisionAnalysis? OriginalAnalysis { get; private set; }
    public DecisionAnalysis? UpdatedAnalysis { get; private set; }
    public double FeasibilityDelta { get; private set; }
    public string ImpactSummary { get; private set; }

    // Visualization data
    public Dictionary<string, object> VisualizationData { get; private set; }

    public DecisionReplayResult(
        List<ContextChange> changes,
        DecisionAnalysis? originalAnalysis,
        DecisionAnalysis? updatedAnalysis,
        string impactSummary)
    {
        ReplayId = Guid.NewGuid();
        ReplayedAt = DateTime.UtcNow;
        Changes = changes ?? new List<ContextChange>();
        HasSignificantChanges = changes?.Any(c => c.IsSignificant) ?? false;
        OriginalAnalysis = originalAnalysis;
        UpdatedAnalysis = updatedAnalysis;
        FeasibilityDelta = (updatedAnalysis?.FeasibilityScore ?? 0) - (originalAnalysis?.FeasibilityScore ?? 0);
        ImpactSummary = impactSummary;
        VisualizationData = new Dictionary<string, object>();
    }

    public void AddVisualizationData(string key, object data)
    {
        VisualizationData[key] = data;
    }
}

/// <summary>
/// Represents a detected change between two contexts
/// </summary>
public class ContextChange
{
    public string Field { get; set; }
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public bool IsSignificant { get; set; }
    public string ChangeType { get; set; } // Added, Removed, Modified

    public ContextChange(string field, object? oldValue, object? newValue, bool isSignificant, string changeType)
    {
        Field = field;
        OldValue = oldValue;
        NewValue = newValue;
        IsSignificant = isSignificant;
        ChangeType = changeType;
    }
}
