using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Rule-based feasibility engine. Produces a deterministic 0-100 score.
///
/// Score = BudgetFit×0.30 + TimelineFit×0.30 + TeamCapacity×0.20 + Simplicity×0.20
/// </summary>
public class FeasibilityEngine : IFeasibilityEngine
{
    // ── Complexity keyword weights ────────────────────────────────────────

    private static readonly Dictionary<string, double> HighComplexity = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ai"] = 18, ["ml"] = 18, ["machine learning"] = 18, ["deep learning"] = 18,
        ["payments"] = 15, ["payment gateway"] = 15, ["stripe"] = 12,
        ["real-time"] = 14, ["realtime"] = 14, ["websockets"] = 12,
        ["blockchain"] = 18, ["smart contracts"] = 18,
        ["video streaming"] = 16, ["live video"] = 16,
        ["microservices"] = 14, ["distributed"] = 14,
        ["multi-tenant"] = 13, ["multi tenant"] = 13,
        ["3d"] = 16, ["ar"] = 16, ["vr"] = 16,
        ["compliance"] = 12, ["gdpr"] = 10, ["hipaa"] = 12,
    };

    private static readonly Dictionary<string, double> MediumComplexity = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auth"] = 8, ["authentication"] = 8, ["oauth"] = 9, ["sso"] = 9,
        ["dashboard"] = 7, ["analytics"] = 9,
        ["notifications"] = 6, ["email"] = 5, ["sms"] = 6,
        ["file upload"] = 7, ["storage"] = 6,
        ["search"] = 8, ["filter"] = 4,
        ["api"] = 6, ["rest api"] = 6, ["graphql"] = 9,
        ["mobile"] = 10, ["ios"] = 10, ["android"] = 10,
        ["geolocation"] = 8, ["maps"] = 8,
        ["reporting"] = 7, ["export"] = 5, ["pdf"] = 6,
        ["social"] = 8, ["comments"] = 5, ["ratings"] = 4,
        ["e-commerce"] = 10, ["cart"] = 8, ["checkout"] = 9,
    };

    // ── Cost per month by team size bracket ───────────────────────────────
    private static decimal CostPerMonth(int teamSize) => teamSize switch
    {
        1     => 8_000m,
        <= 3  => 18_000m,
        <= 6  => 40_000m,
        <= 10 => 80_000m,
        _     => 140_000m,
    };

    // ── IFeasibilityEngine ────────────────────────────────────────────────

    public FeasibilityResult Evaluate(ProjectInput input)
    {
        var complexity  = CalculateComplexityScore(input.Features, input.ProjectType);
        var estCost     = EstimateCost(input, complexity);
        var estMonths   = EstimateMonths(input, complexity);
        var reqTeam     = RecommendedTeamSize(complexity, input.TimelineMonths);

        // Sub-scores (each 0-100)
        var budgetFit    = ScoreBudget(input.BudgetUsd, estCost);
        var timelineFit  = ScoreTimeline(input.TimelineMonths, estMonths);
        var teamCapacity = ScoreTeam(input.TeamSize, reqTeam);
        var simplicity   = 100 - complexity;   // complexity is already 0-100

        var overall = budgetFit * 0.30
                    + timelineFit * 0.30
                    + teamCapacity * 0.20
                    + simplicity * 0.20;

        var issues      = BuildIssues(input, budgetFit, timelineFit, teamCapacity, complexity, estCost, estMonths, reqTeam);
        var suggestions = BuildSuggestions(input, budgetFit, timelineFit, teamCapacity, estCost, estMonths, reqTeam);
        var explain     = BuildExplainability(input, budgetFit, timelineFit, teamCapacity, simplicity, complexity, estCost, estMonths, reqTeam);

        var verdict = overall >= 75 ? "Feasible"
                    : overall >= 55 ? "Risky"
                    : "NeedsAdjustment";

        return new FeasibilityResult
        {
            Score               = Math.Round(overall, 1),
            Verdict             = verdict,
            BudgetFitScore      = Math.Round(budgetFit, 1),
            TimelineFitScore    = Math.Round(timelineFit, 1),
            TeamCapacityScore   = Math.Round(teamCapacity, 1),
            ComplexityScore     = Math.Round(simplicity, 1),
            EstimatedCostUsd    = estCost,
            EstimatedMonths     = Math.Round(estMonths, 1),
            RequiredTeamSize    = reqTeam,
            Issues              = issues,
            SuggestedAdjustments= suggestions,
            Explainability      = explain,
        };
    }

    public double CalculateComplexityScore(IEnumerable<string> features, string projectType)
    {
        double total = 0;
        foreach (var feature in features)
        {
            if (string.IsNullOrWhiteSpace(feature)) continue;
            var lower = feature.ToLowerInvariant();

            foreach (var (kw, w) in HighComplexity)
                if (lower.Contains(kw)) { total += w; break; }

            foreach (var (kw, w) in MediumComplexity)
                if (lower.Contains(kw)) { total += w; break; }

            // Default feature cost
            total += 3;
        }

        // Cap at 100
        return Math.Min(total, 100);
    }

    public decimal EstimateCost(ProjectInput input, double complexityScore)
    {
        var multiplier = 1 + complexityScore / 100.0;
        return CostPerMonth(input.TeamSize) * (decimal)(input.TimelineMonths * multiplier);
    }

    public double EstimateMonths(ProjectInput input, double complexityScore)
    {
        // Base months: complexity / 10 (scale: 0-100 complexity → 0-10 base months)
        var baseMonths = 1 + complexityScore / 10.0;
        // Adjust for team throughput (more devs shorten, solo extends)
        var throughput = Math.Log(input.TeamSize + 1, 2);  // log2(teamSize+1)
        return Math.Max(0.5, baseMonths / throughput);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static int RecommendedTeamSize(double complexity, double timelineMonths)
    {
        if (complexity < 20) return 1;
        if (complexity < 40) return Math.Max(1, (int)Math.Ceiling(3 / timelineMonths));
        if (complexity < 60) return Math.Max(2, (int)Math.Ceiling(6 / timelineMonths));
        if (complexity < 80) return Math.Max(3, (int)Math.Ceiling(12 / timelineMonths));
        return Math.Max(5, (int)Math.Ceiling(18 / timelineMonths));
    }

    private static double ScoreBudget(decimal budget, decimal estCost)
    {
        if (estCost <= 0) return 100;
        var ratio = (double)(budget / estCost);
        return ratio switch
        {
            >= 1.5 => 100,
            >= 1.2 => 90,
            >= 1.0 => 75,
            >= 0.8 => 55,
            >= 0.6 => 35,
            >= 0.4 => 20,
            _      => 5,
        };
    }

    private static double ScoreTimeline(double available, double required)
    {
        if (required <= 0) return 100;
        var ratio = available / required;
        return ratio switch
        {
            >= 1.5 => 100,
            >= 1.2 => 90,
            >= 1.0 => 75,
            >= 0.8 => 50,
            >= 0.6 => 25,
            _      => 5,
        };
    }

    private static double ScoreTeam(int available, int required)
    {
        if (required <= 0) return 100;
        var ratio = (double)available / required;
        return ratio switch
        {
            >= 1.5 => 100,
            >= 1.0 => 85,
            >= 0.8 => 65,
            >= 0.6 => 40,
            >= 0.4 => 20,
            _      => 5,
        };
    }

    private static List<FeasibilityIssue> BuildIssues(
        ProjectInput input, double budgetFit, double timelineFit, double teamCapacity,
        double complexity, decimal estCost, double estMonths, int reqTeam)
    {
        var issues = new List<FeasibilityIssue>();

        if (budgetFit < 55)
            issues.Add(new FeasibilityIssue
            {
                Dimension = "Budget",
                Message   = $"Budget ${input.BudgetUsd:N0} is below the estimated ${estCost:N0} needed.",
                Severity  = budgetFit < 25 ? "Critical" : "High",
            });

        if (timelineFit < 55)
            issues.Add(new FeasibilityIssue
            {
                Dimension = "Timeline",
                Message   = $"{input.TimelineMonths} months is shorter than the estimated {estMonths:F1} months.",
                Severity  = timelineFit < 25 ? "Critical" : "High",
            });

        if (teamCapacity < 55)
            issues.Add(new FeasibilityIssue
            {
                Dimension = "Team",
                Message   = $"{input.TeamSize} developer(s) may be insufficient; recommended {reqTeam} for this scope.",
                Severity  = teamCapacity < 25 ? "High" : "Medium",
            });

        if (complexity > 70)
            issues.Add(new FeasibilityIssue
            {
                Dimension = "Complexity",
                Message   = $"High feature complexity ({complexity:F0}/100). Consider phasing delivery.",
                Severity  = complexity > 85 ? "High" : "Medium",
            });

        return issues;
    }

    private static List<SuggestedAdjustment> BuildSuggestions(
        ProjectInput input, double budgetFit, double timelineFit, double teamCapacity,
        decimal estCost, double estMonths, int reqTeam)
    {
        var suggestions = new List<SuggestedAdjustment>();

        if (budgetFit < 75)
            suggestions.Add(new SuggestedAdjustment
            {
                Parameter          = "Budget",
                Description        = $"Increase budget to at least ${estCost * 1.1m:N0} (10% buffer above estimate).",
                QuantitativeImpact = $"+{(estCost * 1.1m - input.BudgetUsd):N0} USD",
            });

        if (timelineFit < 75)
            suggestions.Add(new SuggestedAdjustment
            {
                Parameter          = "Timeline",
                Description        = $"Extend timeline to {Math.Ceiling(estMonths * 1.1):F0} months (10% buffer).",
                QuantitativeImpact = $"+{Math.Ceiling(estMonths * 1.1) - input.TimelineMonths:F1} months",
            });

        if (teamCapacity < 75 && reqTeam > input.TeamSize)
            suggestions.Add(new SuggestedAdjustment
            {
                Parameter          = "Team Size",
                Description        = $"Add {reqTeam - input.TeamSize} developer(s) to meet the recommended {reqTeam}.",
                QuantitativeImpact = $"+{reqTeam - input.TeamSize} developer(s)",
            });

        return suggestions;
    }

    private static List<string> BuildExplainability(
        ProjectInput input, double budgetFit, double timelineFit, double teamCapacity,
        double simplicity, double complexity, decimal estCost, double estMonths, int reqTeam)
    {
        return new List<string>
        {
            $"Budget fit ({budgetFit:F0}/100): Your budget of ${input.BudgetUsd:N0} vs estimated ${estCost:N0}.",
            $"Timeline fit ({timelineFit:F0}/100): {input.TimelineMonths} months available vs {estMonths:F1} months estimated.",
            $"Team capacity ({teamCapacity:F0}/100): {input.TeamSize} developer(s) vs recommended {reqTeam}.",
            $"Simplicity ({simplicity:F0}/100): Feature complexity scored at {complexity:F0}/100.",
            $"Features analysed: {string.Join(", ", input.Features.Take(8))}.",
        };
    }
}
