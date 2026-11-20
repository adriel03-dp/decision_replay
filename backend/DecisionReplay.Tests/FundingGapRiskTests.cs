using DecisionReplay.Application.Services;
using DecisionReplay.Domain.Enums;

namespace DecisionReplay.Tests;

public sealed class FundingGapRiskTests
{
    [Theory]
    [InlineData("project_planning", "project_goal")]
    [InlineData("product_launch", "product_goal")]
    [InlineData("business_startup", "business_idea")]
    public void ModestFundingGapRemainsVisibleDespiteAGoodWeightedScore(string domain, string goalField)
    {
        var data = WorkflowFixture.Project(); data.Domain = domain; data.Fields[goalField] = data.Goal;
        data.Fields["budget"] = "95000"; data.Fields["scope"] = "onboarding, weekly plans, payments, analytics";
        data.Fields["target_market"] = "Families planning meals";
        foreach (var field in new[] { "market_evidence", "legal_readiness", "operating_experience", "technical_readiness" })
            data.Fields[field] = "high";
        var template = new DomainTemplateService().Get(domain);
        var allowed = template.Fields.Select(field => field.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        data.Fields = data.Fields.Where(pair => allowed.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        var assessment = new FeasibilityScoringService().Calculate(data, template, new DecisionValidationService().Validate(data, template));
        Assert.Equal(5224, assessment.Economics!.FundingGap, 2);
        Assert.True(assessment.FeasibilityScore >= 75);
        var risk = Assert.Single(assessment.Risks, risk => risk.Factor == "budget_feasibility");
        Assert.Equal("delivery_funding_gap", risk.Code);
        Assert.Contains("USD 5224", risk.Message);
        Assert.NotEqual(DecisionRiskLevel.Low, assessment.RiskLevel);
        Assert.Contains(assessment.Recommendations, item => item.Contains("funding gap"));
    }

    [Fact]
    public void FullyFundedEstimateDoesNotCreateAFundingGapRisk()
    {
        var data = WorkflowFixture.Project();
        var template = new DomainTemplateService().Get(data.Domain);
        var assessment = new FeasibilityScoringService().Calculate(data, template, new DecisionValidationService().Validate(data, template));
        Assert.Equal(0, assessment.Economics!.FundingGap);
        Assert.DoesNotContain(assessment.Risks, risk => risk.Code == "delivery_funding_gap");
    }

    [Fact]
    public void SevereBudgetShortfallKeepsItsHighRiskWithoutADuplicate()
    {
        var data = WorkflowFixture.Project(); data.Fields["budget"] = "10000";
        var template = new DomainTemplateService().Get(data.Domain);
        var assessment = new FeasibilityScoringService().Calculate(data, template, new DecisionValidationService().Validate(data, template));
        var risk = Assert.Single(assessment.Risks, risk => risk.Factor == "budget_feasibility");
        Assert.Equal("High", risk.Severity);
        Assert.DoesNotContain(assessment.Risks, item => item.Code == "delivery_funding_gap");
    }
}
