using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Tests;

public sealed class FeasibilityScoringServiceTests
{
    private readonly DomainTemplateService _templates = new();
    private readonly DecisionValidationService _validation = new();
    private readonly FeasibilityScoringService _scoring = new();

    [Fact]
    public void BusinessStartup_HigherBudgetImprovesDeterministicScore()
    {
        var template = _templates.Get("business_startup");
        var constrained = Startup("10000");
        var funded = Startup("250000");

        var constrainedResult = Score(constrained, template);
        var fundedResult = Score(funded, template);

        Assert.True(fundedResult.FeasibilityScore > constrainedResult.FeasibilityScore);
        Assert.True(
            fundedResult.FactorBreakdown.Single(f => f.Factor == "budget_feasibility").Score >
            constrainedResult.FactorBreakdown.Single(f => f.Factor == "budget_feasibility").Score);
    }

    [Fact]
    public void MissingRequiredFields_CreatePenaltyRiskAndLowConfidence()
    {
        var template = _templates.Get("project_planning");
        var decision = new StructuredDecisionData
        {
            Domain = "project_planning",
            Goal = "Deliver a new internal platform",
            Fields = new Dictionary<string, string>
            {
                ["project_goal"] = "Deliver a new internal platform",
                ["timeline_months"] = "6"
            }
        };

        var validation = _validation.Validate(decision, template);
        var result = _scoring.Calculate(decision, template, validation);

        Assert.Contains("budget", validation.MissingFields);
        Assert.Contains("team_size", validation.MissingFields);
        Assert.Contains(result.Risks, risk => risk.Code == "missing_budget");
        Assert.Contains(result.FactorBreakdown, factor => factor.Confidence.ToString() == "Low");
        Assert.True(result.FeasibilityScore < 75);
    }

    [Fact]
    public void InvalidExtractedRequiredValues_BecomeImprovementGapsNotErrors()
    {
        var template = _templates.Get("project_planning");
        var decision = new StructuredDecisionData
        {
            Domain = "project_planning",
            Goal = "Improve a student registration platform",
            Fields = new Dictionary<string, string>
            {
                ["project_goal"] = "Improve a student registration platform",
                ["budget"] = "$5,000",
                ["timeline_months"] = "first semester",
                ["team_size"] = "4",
                ["scope"] = "club registration, ticket generation, QR attendance"
            }
        };

        var validation = _validation.Validate(decision, template);

        Assert.Empty(validation.Errors);
        Assert.Contains("timeline_months", validation.MissingFields);
        Assert.Contains(validation.Warnings, warning =>
            warning.Contains("timeline_months", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FactorWeights_SumToOneForEveryTemplate()
    {
        foreach (var template in _templates.GetAll())
            Assert.Equal(1, template.ScoringFactors.Sum(factor => factor.Weight), precision: 6);
    }

    private FeasibilityAssessment Score(
        StructuredDecisionData decision,
        DecisionDomainTemplate template)
    {
        var validation = _validation.Validate(decision, template);
        return _scoring.Calculate(decision, template, validation);
    }

    private static StructuredDecisionData Startup(string budget) =>
        new()
        {
            Domain = "business_startup",
            Title = "Validated services startup",
            Goal = "Launch a specialist services business",
            Fields = new Dictionary<string, string>
            {
                ["business_idea"] = "Specialist services",
                ["budget"] = budget,
                ["timeline_months"] = "6",
                ["team_size"] = "2",
                ["target_market"] = "Small businesses",
                ["market_evidence"] = "high",
                ["legal_readiness"] = "high",
                ["operating_experience"] = "high",
                ["scope"] = "landing page, pilot service, billing"
            }
        };
}
