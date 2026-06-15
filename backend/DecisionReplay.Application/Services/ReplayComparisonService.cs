using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class ReplayComparisonService
{
    public ReplayComparison Compare(
        DecisionVersion previous,
        DecisionVersion current,
        DecisionDomainTemplate template)
    {
        var fields = template.ReplaySensitiveFields
            .Concat(previous.StructuredData.Fields.Keys)
            .Concat(current.StructuredData.Fields.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var changes = fields
            .Select(field => new DecisionFieldChange
            {
                Field = field,
                From = previous.StructuredData.Get(field),
                To = current.StructuredData.Get(field)
            })
            .Where(change => !string.Equals(change.From, change.To, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var comparison = new ReplayComparison
        {
            PreviousVersion = previous.Version,
            NewVersion = current.Version,
            ChangedFields = changes,
            ScoreDelta = Math.Round(
                current.Feasibility.FeasibilityScore - previous.Feasibility.FeasibilityScore,
                1),
            RiskDelta = $"{previous.Feasibility.RiskLevel} \u2192 {current.Feasibility.RiskLevel}",
            PlanChanges = ComparePlans(previous.Plan, current.Plan)
        };
        comparison.MainReason = BuildMainReason(comparison, previous, current);
        return comparison;
    }

    private static List<PlanChange> ComparePlans(ActionPlan? previous, ActionPlan? current)
    {
        var changes = new List<PlanChange>();
        if (previous == null || current == null) return changes;

        if (Math.Abs(previous.TimelineMonths - current.TimelineMonths) > .01)
        {
            changes.Add(new PlanChange
            {
                Type = "timeline_change",
                OldValue = $"{previous.TimelineMonths:0.#} months",
                NewValue = $"{current.TimelineMonths:0.#} months",
                Impact = current.TimelineMonths > previous.TimelineMonths
                    ? "Delivery phases were extended, increasing execution buffer."
                    : "Delivery phases were compressed, increasing schedule pressure."
            });
        }

        if (previous.Phases.Count != current.Phases.Count)
        {
            changes.Add(new PlanChange
            {
                Type = "phase_structure_change",
                OldValue = $"{previous.Phases.Count} phases",
                NewValue = $"{current.Phases.Count} phases",
                Impact = "The plan structure was regenerated for the updated timeline."
            });
        }

        var oldTasks = previous.Phases.Sum(phase => phase.Tasks.Count);
        var newTasks = current.Phases.Sum(phase => phase.Tasks.Count);
        if (oldTasks != newTasks)
        {
            changes.Add(new PlanChange
            {
                Type = "task_count_change",
                OldValue = oldTasks.ToString(),
                NewValue = newTasks.ToString(),
                Impact = "Plan tasks changed to reflect the updated structured decision."
            });
        }

        return changes;
    }

    private static string BuildMainReason(
        ReplayComparison comparison,
        DecisionVersion previous,
        DecisionVersion current)
    {
        if (comparison.ChangedFields.Count == 0)
            return "No replay-sensitive structured fields changed.";

        var largestFactorChange = current.Feasibility.FactorBreakdown
            .Select(currentFactor =>
            {
                var previousFactor = previous.Feasibility.FactorBreakdown.FirstOrDefault(item =>
                    string.Equals(item.Factor, currentFactor.Factor, StringComparison.OrdinalIgnoreCase));
                return new
                {
                    currentFactor.Factor,
                    Delta = currentFactor.Score - (previousFactor?.Score ?? currentFactor.Score)
                };
            })
            .OrderByDescending(item => Math.Abs(item.Delta))
            .FirstOrDefault();

        return largestFactorChange == null
            ? $"The replay changed: {string.Join(", ", comparison.ChangedFields.Select(change => change.Field))}."
            : $"{largestFactorChange.Factor.Replace('_', ' ')} changed by {largestFactorChange.Delta:+0.0;-0.0;0.0} points after updating {string.Join(", ", comparison.ChangedFields.Select(change => change.Field))}.";
    }
}
