using DecisionReplay.Application.Services;
using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Tests;

public sealed class ReplayComparisonServiceTests
{
    [Fact]
    public void Compare_ReportsBudgetScoreRiskAndPlanChanges()
    {
        var previous = Version(
            1,
            budget: "10000",
            timeline: 6,
            score: 48,
            risk: DecisionRiskLevel.High);
        var current = Version(
            2,
            budget: "25000",
            timeline: 9,
            score: 60,
            risk: DecisionRiskLevel.Medium);
        var template = new DomainTemplateService().Get("business_startup");

        var result = new ReplayComparisonService().Compare(previous, current, template);

        Assert.Contains(result.ChangedFields, change =>
            change.Field == "budget" && change.From == "10000" && change.To == "25000");
        Assert.Equal(12, result.ScoreDelta);
        Assert.Equal("High \u2192 Medium", result.RiskDelta);
        Assert.Contains(result.PlanChanges, change => change.Type == "timeline_change");
        Assert.Contains("budget feasibility", result.MainReason, StringComparison.OrdinalIgnoreCase);
    }

    private static DecisionVersion Version(
        int version,
        string budget,
        double timeline,
        double score,
        DecisionRiskLevel risk) =>
        new()
        {
            Version = version,
            StructuredData = new StructuredDecisionData
            {
                Domain = "business_startup",
                Fields = new Dictionary<string, string>
                {
                    ["budget"] = budget,
                    ["timeline_months"] = timeline.ToString(),
                    ["team_size"] = "2"
                }
            },
            Feasibility = new FeasibilityAssessment
            {
                FeasibilityScore = score,
                RiskLevel = risk,
                FactorBreakdown = new List<FactorBreakdown>
                {
                    new()
                    {
                        Factor = "budget_feasibility",
                        Score = score,
                        Weight = .25,
                        WeightedScore = score * .25
                    }
                }
            },
            Plan = new ActionPlan
            {
                TimelineMonths = timeline,
                Phases = new List<PlanPhase>
                {
                    new()
                    {
                        PhaseName = "Execution",
                        Tasks = new List<PlanTask> { new() { TaskName = "Execute" } }
                    }
                }
            }
        };
}
