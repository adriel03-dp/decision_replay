using System.Text.RegularExpressions;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class ImprovementConstraintPolicy
{
    public static List<string> Resolve(StructuredDecisionData decision, DecisionDomainTemplate template)
    {
        var value = decision.Get("fixed_fields");
        if (value == null) return new[] { "budget", "timeline_months" }.Where(name => template.Fields.Any(field => field.Name == name)).ToList();
        if (value.Trim().Equals("none", StringComparison.OrdinalIgnoreCase)) return new();
        var fields = DeliveryCostRules.SplitList(value).Select(name => name.ToLowerInvariant()).Distinct().ToList();
        if (fields.Count == 0 || fields.Any(name => name == "fixed_fields" || !template.Fields.Any(field => field.Name == name)))
            throw new ArgumentException("fixed_fields must list supported field names or be 'none'. Budget and timeline are fixed by default.");
        return fields;
    }

    public static void ApplyTextPolicy(StructuredDecisionData decision, DecisionDomainTemplate template, string input,
        IReadOnlyCollection<string>? requested, IReadOnlyCollection<string>? inherited = null)
    {
        if (!template.Fields.Any(field => field.Name == "fixed_fields")) return;
        // Policy is owned by explicit user input/metadata; a language model cannot unlock constraints.
        decision.Fields.Remove("fixed_fields");
        var label = Regex.Match(input, @"(?:^|[;\r\n])\s*fixed[ _]fields\s*[:=]\s*([^;\r\n]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        if (requested != null)
        {
            if (requested.Count > 25 || requested.Any(name => string.IsNullOrWhiteSpace(name) || name.Length > 100))
                throw new ArgumentException("Supply at most 25 non-empty fixed field names.");
            decision.Fields["fixed_fields"] = requested.Count == 0 ? "none" : string.Join(", ", requested.Select(name => name.Trim().ToLowerInvariant()));
        }
        else if (label.Success) decision.Fields["fixed_fields"] = label.Groups[1].Value.Trim();
        else if (inherited != null) decision.Fields["fixed_fields"] = inherited.Count == 0 ? "none" : string.Join(", ", inherited);
        else decision.Fields["fixed_fields"] = "budget, timeline_months";
    }

    public static ImprovementConstraintCheck Check(StructuredDecisionData original, StructuredDecisionData proposed,
        DecisionDomainTemplate template, FeasibilityAssessment assessment)
    {
        var result = new ImprovementConstraintCheck { FixedFields = Resolve(original, template) };
        foreach (var field in result.FixedFields)
            if (!string.Equals(original.Get(field), proposed.Get(field), StringComparison.OrdinalIgnoreCase))
                result.Reasons.Add($"Fixed field '{field}' cannot change in an improvement proposal.");
        if (result.FixedFields.Contains("budget"))
        {
            var hasBudget = DecisionValidationService.TryNumber(original.Get("budget"), out var budget) && budget >= 0;
            if (hasBudget) result.BudgetCeiling = budget;
            if (!hasBudget || assessment.Economics == null)
            { result.NeedsInformation = true; result.Reasons.Add("The fixed budget needs a valid amount and a supported delivery-cost estimate."); }
            else
            {
                if (assessment.Economics.EstimatedCost > budget)
                    result.Reasons.Add("Estimated delivery cost, including contingency, exceeds the fixed budget.");
            }
        }
        if (result.FixedFields.Contains("timeline_months"))
        {
            var hasLimit = DecisionValidationService.TryNumber(original.Get("timeline_months"), out var limit) && limit is > 0 and <= 120;
            if (hasLimit) result.TimelineCeilingMonths = limit;
            if (!hasLimit
                || !DecisionValidationService.TryNumber(proposed.Get("timeline_months"), out var months) || months is <= 0 or > 120
                || !DecisionValidationService.TryNumber(proposed.Get("team_size"), out var team) || team < 1
                || DeliveryCostRules.SplitList(proposed.Get("scope")).Length == 0)
            { result.NeedsInformation = true; result.Reasons.Add("The fixed deadline needs a valid timeline, team size and explicit scope."); }
            else
            {
                if (months > limit || DeliveryScheduleRules.EstimateMonths(proposed) > months)
                    result.Reasons.Add("The proposed schedule does not fit within the fixed deadline under the delivery-effort rules.");
            }
        }
        result.EstimatesWithinLimits = result.Reasons.Count == 0;
        return result;
    }
}
