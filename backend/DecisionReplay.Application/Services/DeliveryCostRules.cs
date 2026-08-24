using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class DeliveryCostRules
{
    public static EconomicEstimate? Estimate(StructuredDecisionData decision, double defaultMonthlyRate)
    {
        if (!DecisionValidationService.TryNumber(decision.Get("budget"), out var budget) || budget < 0 || !Positive("timeline_months", out var months) || !Positive("team_size", out var team)) return null;
        var currency = decision.Get("currency")?.Trim().ToUpperInvariant() ?? "USD";
        var hasRate = Positive("monthly_cost_per_person", out var rate);
        // Never apply a USD benchmark to a different currency or invent an exchange rate.
        if (currency != "USD" && !hasRate) return null;
        if (!hasRate) rate = defaultMonthlyRate;
        var hasContingency = DecisionValidationService.TryNumber(decision.Get("contingency_percent"), out var contingency) && contingency is >= 0 and <= 100;
        if (!hasContingency) contingency = 20;
        var scope = SplitList(decision.Get("scope")).Length;
        var multiplier = 1 + Math.Min(Math.Max(1, scope), 12) * .04;
        var burn = rate * team * multiplier;
        var cost = burn * months * (1 + contingency / 100);
        if (!double.IsFinite(cost) || !double.IsFinite(burn)) return null;
        var assumptions = new List<string> { "Delivery cost = monthly cost per person × team size × scope multiplier × months × (1 + contingency / 100). Revenue, profit, tax and ROI are not estimated without business data.",
            "Scope multiplier is a heuristic: 1 + 0.04 × item count (one to twelve items); it does not estimate feature effort." };
        if (decision.Get("currency") == null) assumptions.Add("Currency was not supplied; USD is assumed. Confirm before relying on cost estimates.");
        if (!hasRate) assumptions.Add($"Monthly cost per person defaults to {rate:0.##} USD as a planning assumption, not a verified local salary or market quote.");
        if (!hasContingency) assumptions.Add("A 20% contingency is assumed.");
        if (scope == 0) assumptions.Add("Scope was not supplied; one scope item is assumed for cost estimation only.");
        return new() { Currency = currency, AvailableBudget = budget, MonthlyCostPerPerson = rate, ContingencyPercent = contingency,
            ScopeMultiplier = multiplier, MonthlyBurn = Math.Round(burn, 2), EstimatedCost = Math.Round(cost, 2),
            FundingGap = Math.Round(Math.Max(0, cost - budget), 2), RunwayMonths = Math.Round(budget / burn, 2), Assumptions = assumptions };

        bool Positive(string field, out double value) => DecisionValidationService.TryNumber(decision.Get(field), out value) && value > 0;
    }

    public static string[] SplitList(string? value) => string.IsNullOrWhiteSpace(value) || value.Trim().Equals("none", StringComparison.OrdinalIgnoreCase)
        ? Array.Empty<string>() : value.Split(new[] { ',', ';', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
