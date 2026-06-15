using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class FeasibilityScoringService
{
    public FeasibilityAssessment Calculate(
        StructuredDecisionData decision,
        DecisionDomainTemplate template,
        DecisionValidationResult validation)
    {
        var factors = template.ScoringFactors
            .Select(factor => EvaluateFactor(decision, factor))
            .ToList();

        var missingPenalty = Math.Min(20, validation.MissingFields.Count * 4);
        var rawScore = factors.Sum(factor => factor.WeightedScore);
        var finalScore = Math.Clamp(Math.Round(rawScore - missingPenalty, 1), 0, 100);
        var risks = BuildRisks(template, factors, validation);
        var riskLevel = CalculateRiskLevel(finalScore, risks);
        var recommendations = BuildRecommendations(factors, validation);

        return new FeasibilityAssessment
        {
            FeasibilityScore = finalScore,
            RiskLevel = riskLevel,
            FactorBreakdown = factors,
            Risks = risks,
            Recommendations = recommendations,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static FactorBreakdown EvaluateFactor(
        StructuredDecisionData decision,
        ScoringFactorDefinition factor)
    {
        var presentFields = factor.SourceFields
            .Where(field => decision.Get(field) != null)
            .ToList();
        var missingFields = factor.SourceFields
            .Except(presentFields, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var score = factor.Evaluator switch
        {
            "budget_feasibility" => BudgetScore(decision, 3_500),
            "project_budget" => BudgetScore(decision, 7_000),
            "product_budget" => BudgetScore(decision, 8_000),
            "education_budget" => EducationBudgetScore(decision),
            "timeline_feasibility" => TimelineScore(decision),
            "career_timeline" => CareerTimelineScore(decision),
            "market_evidence" => LevelScore(decision.Get("market_evidence"), 35),
            "resource_availability" => ResourceScore(decision),
            "operational_complexity" => ComplexityScore(decision),
            "legal_readiness" => LevelScore(decision.Get("legal_readiness"), 35),
            "skill_readiness" => LevelScore(decision.Get("skill_readiness"), 35),
            "financial_resilience" => FinancialResilienceScore(decision),
            "market_demand" => LevelScore(decision.Get("market_demand"), 45),
            "support_level" => LevelScore(decision.Get("support_level"), 50),
            "constraint_load" => ConstraintScore(decision),
            "time_capacity" => TimeCapacityScore(decision),
            "career_alignment" => LevelScore(decision.Get("career_alignment"), 40),
            "entry_readiness" => LevelScore(decision.Get("entry_readiness"), 45),
            "program_value" => LevelScore(decision.Get("program_value"), 45),
            "scope_clarity" => ScopeClarityScore(decision),
            "technical_readiness" => LevelScore(decision.Get("technical_readiness"), 45),
            "dependency_risk" => DependencyScore(decision),
            _ => 50
        };

        score = Math.Clamp(Math.Round(score, 1), 0, 100);
        var confidence = missingFields.Count == 0
            ? FactorConfidence.High
            : presentFields.Count > 0
                ? FactorConfidence.Medium
                : FactorConfidence.Low;
        var assumption = missingFields.Count == 0
            ? null
            : $"Conservative defaults were used for: {string.Join(", ", missingFields)}.";

        return new FactorBreakdown
        {
            Factor = factor.Name,
            Score = score,
            Weight = factor.Weight,
            WeightedScore = Math.Round(score * factor.Weight, 2),
            Reason = BuildReason(factor.Name, score),
            Assumption = assumption,
            Confidence = confidence
        };
    }

    private static double BudgetScore(StructuredDecisionData decision, double monthlyCostPerPerson)
    {
        if (!Number(decision, "budget", out var budget)) return 35;
        var timeline = NumberOr(decision, "timeline_months", 6);
        var teamSize = Math.Max(1, NumberOr(decision, "team_size", 1));
        var scopeCount = Math.Max(1, ListCount(decision.Get("scope")));
        var complexityMultiplier = 1 + Math.Min(scopeCount, 12) * .04;
        var required = monthlyCostPerPerson * teamSize * Math.Max(1, timeline) * complexityMultiplier;
        return FitRatio(budget / required);
    }

    private static double EducationBudgetScore(StructuredDecisionData decision)
    {
        if (!Number(decision, "budget", out var budget)) return 35;
        var months = NumberOr(decision, "timeline_months", 12);
        var conservativeCost = Math.Max(1_000, months * 300);
        return FitRatio(budget / conservativeCost);
    }

    private static double TimelineScore(StructuredDecisionData decision)
    {
        if (!Number(decision, "timeline_months", out var timeline)) return 35;
        var team = Math.Max(1, NumberOr(decision, "team_size", 1));
        var scope = Math.Max(1, ListCount(decision.Get("scope")));
        var dependencies = ListCount(decision.Get("dependencies"));
        var required = Math.Max(1, (scope * 1.25 + dependencies * .8) / Math.Sqrt(team));
        return FitRatio(timeline / required);
    }

    private static double CareerTimelineScore(StructuredDecisionData decision)
    {
        if (!Number(decision, "timeline_months", out var timeline)) return 35;
        var readiness = LevelScore(decision.Get("skill_readiness"), 35);
        var required = readiness >= 80 ? 3 : readiness >= 60 ? 6 : 12;
        return FitRatio(timeline / required);
    }

    private static double ResourceScore(StructuredDecisionData decision)
    {
        var team = NumberOr(decision, "team_size", 1);
        var scope = Math.Max(1, ListCount(decision.Get("scope")));
        var recommended = Math.Max(1, Math.Ceiling(scope / 2d));
        var baseScore = FitRatio(team / recommended);
        var readiness = LevelScore(
            decision.Get("technical_readiness") ?? decision.Get("operating_experience"),
            50);
        return baseScore * .7 + readiness * .3;
    }

    private static double ComplexityScore(StructuredDecisionData decision)
    {
        var scope = ListCount(decision.Get("scope"));
        var dependencies = ListCount(decision.Get("dependencies"));
        var complexity = Math.Min(90, scope * 7 + dependencies * 10);
        return 100 - complexity;
    }

    private static double FinancialResilienceScore(StructuredDecisionData decision)
    {
        if (!Number(decision, "savings_months", out var months)) return 35;
        return months switch
        {
            >= 12 => 100,
            >= 9 => 90,
            >= 6 => 75,
            >= 3 => 55,
            >= 1 => 35,
            _ => 10
        };
    }

    private static double ConstraintScore(StructuredDecisionData decision)
    {
        var count = ListCount(decision.Get("constraints"));
        return Math.Max(20, 100 - count * 15);
    }

    private static double TimeCapacityScore(StructuredDecisionData decision)
    {
        if (!Number(decision, "weekly_hours", out var hours)) return 35;
        return hours switch
        {
            >= 20 => 100,
            >= 15 => 85,
            >= 10 => 70,
            >= 6 => 50,
            >= 3 => 30,
            _ => 10
        };
    }

    private static double ScopeClarityScore(StructuredDecisionData decision)
    {
        var value = decision.Get("scope");
        if (string.IsNullOrWhiteSpace(value)) return 30;
        var count = ListCount(value);
        if (count is >= 1 and <= 8) return 90;
        if (count <= 12) return 75;
        return 55;
    }

    private static double DependencyScore(StructuredDecisionData decision)
    {
        var count = ListCount(decision.Get("dependencies"));
        return count switch
        {
            0 => 90,
            1 => 80,
            2 => 65,
            3 => 50,
            _ => 30
        };
    }

    private static double LevelScore(string? value, double fallback)
    {
        if (DecisionValidationService.TryNumber(value, out var numeric))
            return numeric <= 1 ? numeric * 100 : Math.Clamp(numeric, 0, 100);
        if (string.IsNullOrWhiteSpace(value)) return fallback;

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Contains("very high") || normalized.Contains("ready") || normalized.Contains("strong"))
            return 90;
        if (normalized.Contains("high") || normalized.Contains("good"))
            return 80;
        if (normalized.Contains("medium") || normalized.Contains("partial") || normalized.Contains("moderate"))
            return 60;
        if (normalized.Contains("low") || normalized.Contains("weak"))
            return 30;
        if (normalized.Contains("none") || normalized.Contains("unknown"))
            return 15;
        return fallback;
    }

    private static double FitRatio(double ratio) =>
        ratio switch
        {
            >= 1.25 => 100,
            >= 1 => 80 + (ratio - 1) * 80,
            >= .75 => 60 + (ratio - .75) * 80,
            >= .5 => 40 + (ratio - .5) * 80,
            >= .25 => 20 + (ratio - .25) * 80,
            _ => Math.Max(5, ratio * 80)
        };

    private static List<DecisionRisk> BuildRisks(
        DecisionDomainTemplate template,
        IReadOnlyCollection<FactorBreakdown> factors,
        DecisionValidationResult validation)
    {
        var risks = new List<DecisionRisk>();
        foreach (var rule in template.RiskRules)
        {
            var factor = factors.FirstOrDefault(item =>
                string.Equals(item.Factor, rule.Factor, StringComparison.OrdinalIgnoreCase));
            if (factor == null || factor.Score >= rule.TriggerBelow) continue;

            risks.Add(new DecisionRisk
            {
                Code = $"{rule.Factor}_below_threshold",
                Factor = rule.Factor,
                Severity = rule.Severity,
                Message = rule.Message,
                Mitigation = MitigationFor(rule.Factor)
            });
        }

        foreach (var field in validation.MissingFields)
        {
            risks.Add(new DecisionRisk
            {
                Code = $"missing_{field}",
                Factor = "input_completeness",
                Severity = "Medium",
                Message = $"Required information '{field}' is missing.",
                Mitigation = $"Provide a verified value for '{field}' before committing to the decision."
            });
        }

        return risks;
    }

    private static DecisionRiskLevel CalculateRiskLevel(
        double score,
        IReadOnlyCollection<DecisionRisk> risks)
    {
        if (score < 40 || risks.Count(risk => risk.Severity == "High") >= 3)
            return DecisionRiskLevel.Critical;
        if (score < 55 || risks.Any(risk => risk.Severity == "High"))
            return DecisionRiskLevel.High;
        if (score < 75 || risks.Count > 0)
            return DecisionRiskLevel.Medium;
        return DecisionRiskLevel.Low;
    }

    private static List<string> BuildRecommendations(
        IReadOnlyCollection<FactorBreakdown> factors,
        DecisionValidationResult validation)
    {
        var recommendations = factors
            .Where(factor => factor.Score < 70)
            .OrderBy(factor => factor.Score)
            .Select(factor => RecommendationFor(factor.Factor))
            .Distinct()
            .ToList();

        recommendations.AddRange(validation.MissingFields.Select(field =>
            $"Confirm '{field}' with evidence and rerun the decision before final approval."));
        return recommendations.Distinct().ToList();
    }

    private static string BuildReason(string factor, double score)
    {
        var condition = score >= 75 ? "supports the decision"
            : score >= 55 ? "is workable but constrained"
            : "is a material constraint";
        return $"{factor.Replace('_', ' ')} {condition} under the supplied structured values.";
    }

    private static string RecommendationFor(string factor) =>
        factor switch
        {
            "budget_feasibility" => "Increase available budget, reduce scope, or extend the funding period.",
            "timeline_feasibility" => "Reduce scope, add capacity, or extend the selected timeline.",
            "market_risk" => "Collect direct market evidence before making irreversible commitments.",
            "resource_availability" => "Assign additional capacity or narrow the delivery scope.",
            "operational_complexity" => "Phase the work and remove nonessential operational dependencies.",
            "legal_risk" => "Complete legal and regulatory checks before launch.",
            "skill_readiness" => "Close the highest-priority skill gaps with measurable evidence.",
            "financial_resilience" => "Increase financial runway or use a staged transition.",
            "dependency_risk" => "Assign owners, due dates, and contingency paths to critical dependencies.",
            "scope_clarity" => "Define explicit deliverables and acceptance criteria.",
            "technical_readiness" => "Resolve readiness gaps before committing to launch.",
            _ => $"Improve {factor.Replace('_', ' ')} before committing."
        };

    private static string MitigationFor(string factor) =>
        RecommendationFor(factor);

    private static bool Number(
        StructuredDecisionData decision,
        string field,
        out double value) =>
        DecisionValidationService.TryNumber(decision.Get(field), out value);

    private static double NumberOr(
        StructuredDecisionData decision,
        string field,
        double fallback) =>
        Number(decision, field, out var value) ? value : fallback;

    private static int ListCount(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? 0
            : value.Split(new[] { ',', ';', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries).Length;
}
