using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Persistence.Repositories;

namespace DecisionReplay.Tests;

public sealed class ImprovementConstraintTests
{
    private readonly DomainTemplateService _templates = new();
    private readonly DecisionValidationService _validation = new();
    private readonly FeasibilityScoringService _scoring = new();

    [Fact]
    public async Task DefaultProposalPreservesBudgetAndDeadlineAndFitsTheirEstimates()
    {
        var original = Project();
        var result = await Propose(original);
        Assert.Equal("proposed", result.Status);
        Assert.Equal(new[] { "budget", "timeline_months" }, result.ConstraintCheck!.FixedFields);
        Assert.True(result.ConstraintCheck.EstimatesWithinLimits);
        Assert.Equal(original.Get("budget"), result.ProposedFields["budget"]);
        Assert.Equal(original.Get("timeline_months"), result.ProposedFields["timeline_months"]);
        Assert.True(result.Assessment!.Economics!.EstimatedCost <= 100000);
        Assert.Equal(6, result.Plan!.TimelineMonths);
        Assert.Equal("scope", Assert.Single(result.Changes).Field);
        Assert.Equal("login, payments, reporting", original.Get("scope"));
    }

    [Fact]
    public async Task HigherScoreDoesNotJustifyRemainingOverTheBudget()
    {
        var original = Project(); original.Fields["budget"] = "93000";
        var result = await Propose(original);
        Assert.Equal("constraints_unsatisfied", result.Status);
        Assert.Null(result.Assessment); Assert.Null(result.Plan); Assert.Empty(result.ProposedFields);
        Assert.Contains(result.ConstraintCheck!.Reasons, reason => reason.Contains("exceeds the fixed budget"));
    }

    [Fact]
    public async Task ScopeReductionMayResolveAnInitiallyInsufficientBudget()
    {
        var original = Project(); original.Fields["budget"] = "94000";
        var result = await Propose(original);
        Assert.Equal("proposed", result.Status);
        Assert.True(result.ConstraintCheck!.EstimatesWithinLimits);
        Assert.Equal(93312, result.Assessment!.Economics!.EstimatedCost);
        Assert.Equal("94000", result.ProposedFields["budget"]);
    }

    [Fact]
    public async Task ImpossibleDeadlineCannotBeHiddenByKeepingItsNumericFieldUnchanged()
    {
        var original = Project(); original.Fields["budget"] = "1000000";
        original.Fields["timeline_months"] = "1"; original.Fields["team_size"] = "1";
        original.Fields["scope"] = "login, payments, reports, exports, messaging";
        var result = await Propose(original);
        Assert.Equal("constraints_unsatisfied", result.Status);
        Assert.Null(result.Assessment); Assert.Null(result.Plan);
        Assert.Contains(result.ConstraintCheck!.Reasons, reason => reason.Contains("fixed deadline"));
    }

    [Fact]
    public async Task ScopeCanBeDeferredToFitTheOriginalDeadline()
    {
        var original = Project(); original.Fields["budget"] = "1000000";
        original.Fields["timeline_months"] = "2"; original.Fields["team_size"] = "1";
        original.Fields["scope"] = "login, reports";
        var result = await Propose(original);
        Assert.Equal("proposed", result.Status);
        Assert.Equal(2, result.Plan!.TimelineMonths);
        Assert.True(result.ConstraintCheck!.EstimatesWithinLimits);
        Assert.Equal("login", result.ProposedFields["scope"]);
    }

    [Fact]
    public async Task ExplicitlyFlexibleBudgetCanBeIncreasedWithoutMovingTheDeadline()
    {
        var original = Project(); original.Fields["monthly_cost_per_person"] = "7000";
        original.Fields["fixed_fields"] = "timeline_months, scope";
        var result = await Propose(original);
        Assert.Equal("proposed", result.Status);
        Assert.Equal("budget", Assert.Single(result.Changes).Field);
        Assert.True(double.Parse(result.ProposedFields["budget"], System.Globalization.CultureInfo.InvariantCulture) > 100000);
        Assert.Equal("6", result.ProposedFields["timeline_months"]);
        Assert.Equal(original.Get("scope"), result.ProposedFields["scope"]);
    }

    [Fact]
    public async Task LockingScopePreventsAnAutomaticDeferral()
    {
        var original = Project(); original.Fields["fixed_fields"] = "budget, timeline_months, scope";
        var result = await Propose(original);
        Assert.Equal("no_supported_improvement", result.Status);
        Assert.Empty(result.Changes); Assert.Null(result.Plan);
        Assert.True(result.ConstraintCheck!.EstimatesWithinLimits);
    }

    [Fact]
    public async Task MissingCostsCannotBeCertifiedUnderAFixedBudget()
    {
        var original = Project(); original.Fields["currency"] = "LKR";
        original.Fields.Remove("monthly_cost_per_person");
        var result = await Propose(original);
        Assert.Equal("needs_information", result.Status);
        Assert.True(result.ConstraintCheck!.NeedsInformation);
        Assert.False(result.ConstraintCheck.EstimatesWithinLimits);
        Assert.Null(result.Plan);
    }

    [Theory]
    [InlineData("bduget")]
    [InlineData("none, budget")]
    [InlineData("fixed_fields")]
    [InlineData(", ,")]
    public void InvalidConstraintNamesCannotSilentlyUnlockTheBudget(string value)
    {
        var original = Project(); original.Fields["fixed_fields"] = value;
        Assert.NotEmpty(_validation.Validate(original, _templates.Get(original.Domain)).Errors);
    }

    [Fact]
    public async Task ModelCannotOverrideDefaultLocksOrExplicitRequestMetadata()
    {
        var language = new UntrustedExtraction();
        var engine = Engine(language, new());
        var decision = await engine.AnalyzeAndCreateAsync("Build a mobile application", "owner");
        Assert.Equal("budget, timeline_months", decision.GetCurrentVersion()!.StructuredData.Get("fixed_fields"));
        Assert.Equal("constraints_unsatisfied", decision.GetCurrentVersion()!.Improvement!.Status);
        var forced = await engine.AnalyzeAndCreateAsync("Build a mobile application; fixed_fields: none", "owner", fixedFields: new[] { "budget", "timeline_months" });
        Assert.Equal("budget, timeline_months", forced.GetCurrentVersion()!.StructuredData.Get("fixed_fields"));
        var flexible = await engine.AnalyzeAndCreateAsync("Build a mobile application", "owner", fixedFields: Array.Empty<string>());
        Assert.Equal("none", flexible.GetCurrentVersion()!.StructuredData.Get("fixed_fields"));
        Assert.Equal("budget", Assert.Single(flexible.GetCurrentVersion()!.Improvement!.Changes).Field);
    }

    [Fact]
    public async Task ReplayPreservesPreviouslyChosenLocksWhenTheyAreNotResubmitted()
    {
        var repository = new InMemoryDecisionV2Repository();
        var engine = Engine(new RulesLanguageService(), repository);
        var original = Project(); original.Fields["fixed_fields"] = "budget, timeline_months, scope";
        var decision = await engine.AnalyzeStructuredAndCreateAsync(original, "owner");
        const string input = "Build an app with team 6 for 6 months and budget 100000 USD; scope: login, payments, reporting; monthly_cost_per_person: 2000; technical_readiness: high";
        var replay = await engine.ReplayAsync(decision.Id, input, "owner");
        Assert.Equal("budget, timeline_months, scope", replay.Decision.GetCurrentVersion()!.StructuredData.Get("fixed_fields"));
        Assert.Empty(replay.Decision.GetCurrentVersion()!.Improvement!.Changes);
        Assert.Equal("budget, timeline_months, scope", replay.Decision.Versions[0].StructuredData.Get("fixed_fields"));
    }

    [Fact]
    public void ConstraintMetadataDoesNotChangeTheUnderlyingFeasibilityScore()
    {
        var fixedData = Project(); var flexibleData = Project(); flexibleData.Fields["fixed_fields"] = "none";
        Assert.Equal(Score(fixedData).FeasibilityScore, Score(flexibleData).FeasibilityScore);
    }

    [Theory]
    [InlineData(.1)]
    [InlineData(.5)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(12)]
    public async Task CalendarPhasesAndMilestonesCannotOverrunTheChosenTimeline(double months)
    {
        var original = Project(); original.Fields["timeline_months"] = months.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var plan = await new ActionPlanService(new(), new RulesLanguageService()).GenerateAsync(Guid.NewGuid(), 1, original,
            _templates.Get(original.Domain), Score(original), startDateOverride: start);
        Assert.Equal(months, plan.TimelineMonths);
        Assert.Equal(DeliveryScheduleRules.EndDate(start, months), plan.EndDate);
        Assert.All(plan.Phases, phase => Assert.InRange(phase.EndDate, phase.StartDate, plan.EndDate));
        Assert.All(plan.Milestones, milestone => Assert.InRange(milestone.TargetDate, plan.StartDate, plan.EndDate));
    }

    [Fact]
    public async Task RegenerationKeepsTheOriginalCalendarDeadlineAndReassessesLegacyRules()
    {
        var repository = new InMemoryDecisionV2Repository();
        var original = Project();
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var oldPlan = new ActionPlan { StartDate = start, EndDate = DeliveryScheduleRules.EndDate(start, 6), TimelineMonths = 6 };
        var decision = new DecisionReplay.Domain.Entities.DecisionV2(Guid.NewGuid(), original.Goal, "owner", new()
        { Version = 1, StructuredData = original, Feasibility = new() { RulesVersion = "1.0", FeasibilityScore = 50 }, Plan = oldPlan, CreatedAt = start });
        await repository.CreateAsync(decision);
        var regenerated = await Engine(new RulesLanguageService(), repository).RegeneratePlanAsync(decision.Id, "owner");
        var restored = (await repository.GetByIdForUserAsync(decision.Id, "owner"))!;
        Assert.Equal(oldPlan.StartDate, regenerated.StartDate);
        Assert.Equal(oldPlan.EndDate, regenerated.EndDate);
        Assert.Equal(FeasibilityScoringService.CurrentRulesVersion, restored.GetCurrentVersion()!.Feasibility.RulesVersion);
        Assert.Equal("1.0", restored.Versions[0].Feasibility.RulesVersion);
        Assert.Equal(oldPlan.EndDate, restored.GetCurrentVersion()!.Improvement!.ConstraintCheck!.DeadlineDate);
        Assert.Equal(regenerated.StartDate, restored.GetCurrentVersion()!.Improvement!.Plan!.StartDate);
    }

    [Fact]
    public async Task ReplayDoesNotRestartAFixedDeadlineFromToday()
    {
        var repository = new InMemoryDecisionV2Repository();
        var original = Project();
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousPlan = new ActionPlan { StartDate = start, EndDate = DeliveryScheduleRules.EndDate(start, 6), TimelineMonths = 6 };
        var decision = new DecisionReplay.Domain.Entities.DecisionV2(Guid.NewGuid(), original.Goal, "owner", new()
        { Version = 1, StructuredData = original, Feasibility = Score(original), Plan = previousPlan, CreatedAt = start });
        await repository.CreateAsync(decision);
        var replay = await Engine(new RulesLanguageService(), repository).ReplayAsync(decision.Id,
            "Build an app with team 6 for 6 months and budget 100000 USD; scope: login, payments, reporting; monthly_cost_per_person: 2000; technical_readiness: high", "owner");
        Assert.Equal(start, replay.Decision.GetCurrentVersion()!.Plan!.StartDate);
        Assert.Equal(previousPlan.EndDate, replay.Decision.GetCurrentVersion()!.Plan!.EndDate);
        Assert.Equal(previousPlan.EndDate, replay.Decision.GetCurrentVersion()!.Improvement!.ConstraintCheck!.DeadlineDate);
    }

    [Theory]
    [InlineData(2025, 1, 1, 2, "2025-02-28")]
    [InlineData(2024, 1, 31, 1, "2024-02-28")]
    [InlineData(2025, 1, 31, 1, "2025-02-27")]
    [InlineData(2026, 11, 20, 6, "2027-05-19")]
    public void WholeMonthDeadlinesUseCalendarMonthsAndInclusiveEndDates(int year, int month, int day, double months, string expected)
    {
        var end = DeliveryScheduleRules.EndDate(new DateTime(year, month, day), months);
        Assert.Equal(expected, end.ToString("yyyy-MM-dd"));
    }

    private FeasibilityAssessment Score(StructuredDecisionData data) => _scoring.Calculate(data, _templates.Get(data.Domain), _validation.Validate(data, _templates.Get(data.Domain)));
    private Task<ImprovementProposal> Propose(StructuredDecisionData original) => new ImprovementPlanningService(_validation, _scoring).ProposeAsync(Guid.NewGuid(), 1, original, _templates.Get(original.Domain), Score(original));
    private DecisionEngineService Engine(IAiLanguageService language, InMemoryDecisionV2Repository repository) => new(new(language, _templates), _validation, _templates, _scoring, new(), new(language), new(new(), language), new(), language, repository);
    private static StructuredDecisionData Project() => new() { Domain = "project_planning", Title = "Application", Goal = "Build a mobile application", Fields = new()
    { ["project_goal"] = "Build a mobile application", ["budget"] = "100000", ["currency"] = "USD", ["timeline_months"] = "6", ["team_size"] = "6", ["scope"] = "login, payments, reporting", ["monthly_cost_per_person"] = "2000", ["contingency_percent"] = "20", ["technical_readiness"] = "high" } };
    private sealed class UntrustedExtraction : IAiLanguageService
    {
        public bool IsConfigured => true;
        public Task<StructuredDecisionData> ExtractDecisionAsync(string input, IReadOnlyCollection<DecisionDomainTemplate> templates, CancellationToken cancellationToken = default)
        { var output = Project(); output.Fields["fixed_fields"] = "none"; output.Fields["monthly_cost_per_person"] = "7000"; return Task.FromResult(output); }
        public Task<string> ExplainResultAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, CancellationToken cancellationToken = default) => Task.FromResult("");
        public Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, ActionPlan plan, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlanTaskEnhancement>>(Array.Empty<PlanTaskEnhancement>());
        public Task<string> SummarizeReplayAsync(ReplayComparison comparison, CancellationToken cancellationToken = default) => Task.FromResult("");
    }
}
