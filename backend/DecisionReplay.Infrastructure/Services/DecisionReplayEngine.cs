using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Decision Replay Engine Implementation
/// 
/// SOLID Principles:
/// - Single Responsibility: Handles replay logic and change detection
/// - Open/Closed: Extensible with new change detection strategies
/// - Dependency Inversion: Depends on IAIReasoningServiceV2 abstraction
/// 
/// Clean Architecture:
/// - Infrastructure layer (uses AI service)
/// - Returns domain value objects and application layer results
/// 
/// Purpose:
/// Core "Decision Replay" functionality:
/// 1. Detects changes between original and updated contexts
/// 2. Re-evaluates decisions with new parameters
/// 3. Produces comparison data for visualization
/// 4. Shows why outcomes changed
/// </summary>
public class DecisionReplayEngine : IReplayEngine
{
    private readonly IAIReasoningServiceV2 _reasoningService;

    public DecisionReplayEngine(IAIReasoningServiceV2 reasoningService)
    {
        _reasoningService = reasoningService ?? throw new ArgumentNullException(nameof(reasoningService));
    }

    public Task<DecisionReplayResult> CompareContextsAsync(
        DecisionContext original,
        DecisionContext updated,
        CancellationToken cancellationToken = default)
    {
        var changes = DetectChanges(original, updated);
        var impactSummary = GenerateImpactSummary(changes);

        var result = new DecisionReplayResult(
            changes,
            null,
            null,
            impactSummary
        );

        // Add change visualization data
        result.AddVisualizationData("changeCount", changes.Count);
        result.AddVisualizationData("significantChangeCount", changes.Count(c => c.IsSignificant));
        result.AddVisualizationData("changesByType", changes.GroupBy(c => c.ChangeType)
            .ToDictionary(g => g.Key, g => g.Count()));

        return Task.FromResult(result);
    }

    public async Task<DecisionReplayResult> ReplayDecisionAsync(
        Guid decisionId,
        DecisionContext originalContext,
        DecisionAnalysis originalAnalysis,
        DecisionContext updatedContext,
        CancellationToken cancellationToken = default)
    {
        // Detect changes
        var changes = DetectChanges(originalContext, updatedContext);

        // Re-analyze with updated context
        var updatedAnalysis = await _reasoningService.ReAnalyzeDecisionAsync(
            originalContext,
            updatedContext,
            schema: null,
            cancellationToken: cancellationToken
        );

        // Generate impact summary
        var impactSummary = GenerateComparisonSummary(originalAnalysis, updatedAnalysis, changes);

        var result = new DecisionReplayResult(
            changes,
            originalAnalysis,
            updatedAnalysis,
            impactSummary
        );

        // Add visualization data
        AddVisualizationData(result, originalAnalysis, updatedAnalysis, changes);

        return result;
    }

    private List<ContextChange> DetectChanges(DecisionContext original, DecisionContext updated)
    {
        var changes = new List<ContextChange>();

        // Check if input text changed
        if (original.NaturalLanguageInput != updated.NaturalLanguageInput)
        {
            changes.Add(new ContextChange(
                "NaturalLanguageInput",
                original.NaturalLanguageInput,
                updated.NaturalLanguageInput,
                true,
                "Modified"
            ));
        }

        // Compare inferred attributes
        var allKeys = original.InferredAttributes.Keys
            .Union(updated.InferredAttributes.Keys)
            .ToHashSet();

        foreach (var key in allKeys)
        {
            var hasOld = original.InferredAttributes.TryGetValue(key, out var oldValue);
            var hasNew = updated.InferredAttributes.TryGetValue(key, out var newValue);

            if (!hasOld && hasNew)
            {
                changes.Add(new ContextChange(
                    key,
                    null,
                    newValue,
                    IsSignificantField(key),
                    "Added"
                ));
            }
            else if (hasOld && !hasNew)
            {
                changes.Add(new ContextChange(
                    key,
                    oldValue,
                    null,
                    IsSignificantField(key),
                    "Removed"
                ));
            }
            else if (hasOld && hasNew && !Equals(oldValue, newValue))
            {
                changes.Add(new ContextChange(
                    key,
                    oldValue,
                    newValue,
                    IsSignificantField(key),
                    "Modified"
                ));
            }
        }

        return changes;
    }

    private bool IsSignificantField(string fieldName)
    {
        // Fields that typically impact feasibility significantly
        var significantFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timeline", "resources", "constraints", "budget", "scope",
            "workforce", "materials", "deadline", "capacity"
        };

        return significantFields.Contains(fieldName);
    }

    private string GenerateImpactSummary(List<ContextChange> changes)
    {
        if (!changes.Any())
            return "No changes detected.";

        var significant = changes.Count(c => c.IsSignificant);
        var total = changes.Count;

        if (significant == 0)
            return $"{total} minor change(s) detected. Unlikely to significantly impact feasibility.";

        if (significant == total)
            return $"{significant} significant change(s) detected. Feasibility assessment will likely change substantially.";

        return $"{total} change(s) detected ({significant} significant). Re-analysis recommended.";
    }

    private string GenerateComparisonSummary(
        DecisionAnalysis? original,
        DecisionAnalysis? updated,
        List<ContextChange> changes)
    {
        if (original == null || updated == null)
            return "Unable to compare analyses.";

        var delta = updated.FeasibilityScore - original.FeasibilityScore;
        var direction = delta > 0 ? "improved" : delta < 0 ? "declined" : "unchanged";

        var verdictChanged = original.FeasibilityVerdict != updated.FeasibilityVerdict;

        var summary = $"Feasibility score {direction} by {Math.Abs(delta):F1} points " +
                     $"(from {original.FeasibilityScore:F0} to {updated.FeasibilityScore:F0}).";

        if (verdictChanged)
        {
            summary += $" Verdict changed from {original.FeasibilityVerdict} to {updated.FeasibilityVerdict}.";
        }

        if (changes.Any(c => c.IsSignificant))
        {
            var significantChanges = string.Join(", ", changes.Where(c => c.IsSignificant).Select(c => c.Field));
            summary += $" Key changes: {significantChanges}.";
        }

        return summary;
    }

    private void AddVisualizationData(
        DecisionReplayResult result,
        DecisionAnalysis original,
        DecisionAnalysis updated,
        List<ContextChange> changes)
    {
        // Timeline data (for charts)
        result.AddVisualizationData("timelineComparison", new
        {
            originalScore = original.FeasibilityScore,
            updatedScore = updated.FeasibilityScore,
            delta = result.FeasibilityDelta,
            timestamp = result.ReplayedAt
        });

        // Risk heatmap data
        var originalRiskCount = original.Risks.Count;
        var updatedRiskCount = updated.Risks.Count;
        var newRisks = updated.Risks
            .Where(r => !original.Risks.Any(or => or.Description == r.Description))
            .ToList();
        var resolvedRisks = original.Risks
            .Where(r => !updated.Risks.Any(ur => ur.Description == r.Description))
            .ToList();

        result.AddVisualizationData("riskComparison", new
        {
            originalRiskCount,
            updatedRiskCount,
            newRisks = newRisks.Select(r => new { r.Description, r.Impact }),
            resolvedRisks = resolvedRisks.Select(r => new { r.Description, r.Impact })
        });

        // Change impact visualization
        result.AddVisualizationData("changeImpact", new
        {
            changes = changes.Select(c => new
            {
                c.Field,
                c.ChangeType,
                c.IsSignificant,
                oldValue = c.OldValue?.ToString() ?? "N/A",
                newValue = c.NewValue?.ToString() ?? "N/A"
            })
        });

        // Confidence delta
        result.AddVisualizationData("confidenceDelta", new
        {
            original = original.ConfidenceLevel,
            updated = updated.ConfidenceLevel,
            delta = updated.ConfidenceLevel - original.ConfidenceLevel
        });
    }
}
