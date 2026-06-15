using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Tests;

public sealed class ActionPlanServiceTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(12)]
    public async Task GeneratedPlan_StaysWithinSelectedTimeline(double months)
    {
        var templates = new DomainTemplateService();
        var planService = new ActionPlanService(
            new PlanPhaseBuilderService(),
            new NoOpLanguageService());
        var decision = new StructuredDecisionData
        {
            Domain = "project_planning",
            Goal = "Deliver a reporting platform",
            Fields = new Dictionary<string, string>
            {
                ["project_goal"] = "Deliver a reporting platform",
                ["budget"] = "120000",
                ["timeline_months"] = months.ToString(),
                ["team_size"] = "3",
                ["scope"] = "authentication, reporting, exports"
            }
        };
        var assessment = new FeasibilityAssessment
        {
            FeasibilityScore = 72,
            RiskLevel = DecisionReplay.Domain.Enums.DecisionRiskLevel.Medium
        };

        var plan = await planService.GenerateAsync(
            Guid.NewGuid(),
            1,
            decision,
            templates.Get("project_planning"),
            assessment);

        Assert.NotEmpty(plan.Phases);
        Assert.All(plan.Phases, phase => Assert.NotEmpty(phase.Tasks));
        Assert.True(plan.Phases.Max(phase => phase.EndWeek) <= Math.Ceiling(months * 4.345) + 1);
        Assert.True(plan.EndDate >= plan.StartDate);
    }

    private sealed class NoOpLanguageService : IAiLanguageService
    {
        public bool IsConfigured => false;

        public Task<StructuredDecisionData> ExtractDecisionAsync(
            string naturalLanguageInput,
            IReadOnlyCollection<DecisionDomainTemplate> templates,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> ExplainResultAsync(
            StructuredDecisionData decision,
            FeasibilityAssessment assessment,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);

        public Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(
            StructuredDecisionData decision,
            FeasibilityAssessment assessment,
            ActionPlan plan,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PlanTaskEnhancement>>(Array.Empty<PlanTaskEnhancement>());

        public Task<string> SummarizeReplayAsync(
            ReplayComparison comparison,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Empty);
    }
}
