using DecisionReplay.Application.Services;

namespace DecisionReplay.Tests;

public sealed class ScopePriorityRulesTests
{
    [Fact]
    public void LaterListedMustHaveIsRetainedInEveryCandidate()
    {
        var data = WorkflowFixture.Project(); data.Fields["required_scope"] = "REPORTING";
        var candidates = ScopePriorityRules.ReducedScopes(data);
        Assert.NotEmpty(candidates);
        Assert.All(candidates, candidate => Assert.Contains("reporting", candidate, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("reporting", candidates);
    }

    [Fact]
    public void AllRequiredScopeCannotBeReduced()
    {
        var data = WorkflowFixture.Project(); data.Fields["required_scope"] = data.Fields["scope"];
        Assert.Empty(ScopePriorityRules.ReducedScopes(data));
    }

    [Fact]
    public void UnknownMustHaveFailsValidationRatherThanBeingSilentlyDropped()
    {
        var data = WorkflowFixture.Project(); data.Fields["required_scope"] = "secure messaging";
        var validation = new DecisionValidationService().Validate(data, new DomainTemplateService().Get(data.Domain));
        Assert.Contains(validation.Errors, message => message.Contains("required_scope"));
    }

    [Fact]
    public void RequiredScopeSurvivesEvenWhenAllOtherLimitsAreFlexible()
    {
        var original = WorkflowFixture.Project(); original.Fields["required_scope"] = "reporting"; original.Fields["fixed_fields"] = "none";
        var proposed = WorkflowFixture.Project(); proposed.Fields["scope"] = "login, payments";
        var template = new DomainTemplateService().Get(original.Domain);
        var assessment = new FeasibilityScoringService().Calculate(proposed, template, new DecisionValidationService().Validate(proposed, template));
        var limits = ImprovementConstraintPolicy.Check(original, proposed, template, assessment);
        Assert.False(limits.EstimatesWithinLimits);
        Assert.Contains(limits.Reasons, reason => reason.Contains("must-have"));
    }

    [Fact]
    public async Task PriorityMetadataCannotInflateTheBaselineScore()
    {
        var data = WorkflowFixture.Project(); var template = new DomainTemplateService().Get(data.Domain);
        var validation = new DecisionValidationService(); var scoring = new FeasibilityScoringService();
        var originalScore = scoring.Calculate(data, template, validation.Validate(data, template));
        data.Fields["required_scope"] = "reporting";
        var constrainedScore = scoring.Calculate(data, template, validation.Validate(data, template));
        Assert.Equal(originalScore.FeasibilityScore, constrainedScore.FeasibilityScore);
        var proposal = await new ImprovementPlanningService(validation, scoring).ProposeAsync(Guid.NewGuid(), 1, data, template, constrainedScore);
        Assert.Equal("proposed", proposal.Status);
        Assert.Contains("reporting", proposal.ProposedFields["scope"]);
    }
}
