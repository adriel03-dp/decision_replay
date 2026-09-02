using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.AI;
using DecisionReplay.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace DecisionReplay.Tests;

public sealed class RulesFirstEngineTests
{
    private readonly DomainTemplateService _templates = new();
    private readonly DecisionValidationService _validation = new();
    private readonly FeasibilityScoringService _scoring = new();

    [Theory]
    [InlineData("Build a mobile application within six months with a team of six and a budget of 100,000 USD.")]
    [InlineData("Build a mobile application in 6 months with 6 developers and budget $100,000.")]
    [InlineData("Build a mobile application; timeline_months: six; team_size: six; budget: 100k; currency: USD")]
    public async Task ExplicitUserExampleNeedsNoModel(string input)
    {
        var data = await new RulesLanguageService().ExtractDecisionAsync(input, _templates.GetAll());
        Assert.Equal("project_planning", data.Domain);
        Assert.Equal("100000", data.Get("budget"));
        Assert.Equal("6", data.Get("timeline_months"));
        Assert.Equal("6", data.Get("team_size"));
        Assert.Null(data.Get("scope"));
        Assert.Contains("scope", _validation.Validate(data, _templates.Get(data.Domain)).MissingFields);
    }

    [Fact]
    public async Task RulesOnlyModeResolvesWithoutModelOrCredentials()
    {
        var config = new AiConfiguration();
        config.ValidateSelectedProviders();
        var services = new ServiceCollection().AddLogging().AddDecisionReplayAi(config);
        services.AddSingleton<IAiRunRepository, InMemoryAiRunRepository>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsType<RulesLanguageService>(scope.ServiceProvider.GetRequiredService<IAiLanguageService>());
        var resolver = scope.ServiceProvider.GetRequiredService<IAiProviderResolver>();
        Assert.Equal(new AiTarget("none", "none"), resolver.DefaultTarget("extraction"));
        var disabled = resolver.Resolve("none");
        Assert.Equal("disabled", (await disabled.CheckAvailabilityAsync("none")).Status);
        Assert.Equal("ai_disabled", (await Assert.ThrowsAsync<AiException>(() => disabled.GenerateAsync(new("", "", "none", new())))).Code);
        var workflow = scope.ServiceProvider.GetRequiredService<AiWorkflowService>();
        var failure = await Assert.ThrowsAsync<AiException>(() => workflow.GenerateAsync<DecisionAiAnalysis>("decision-analysis", "v3", new { }, _ => null, target: new("groq", "cloud-model")));
        Assert.Equal("ai_disabled", failure.Code);
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<IAiRunRepository>().GetExecutionsAsync("system"));
    }

    [Fact]
    public async Task CoreCreateAndReplayWorkWithoutAiAndPreserveOriginalVersion()
    {
        var repository = new InMemoryDecisionV2Repository();
        var language = new RulesLanguageService();
        var engine = Engine(language, repository);
        const string input = "Build a mobile application within six months with a team of six and budget 100000 USD; scope: login, payments, reporting";
        var decision = await engine.AnalyzeAndCreateAsync(input, "owner");
        var first = decision.Versions.Single();
        Assert.NotNull(first.Plan);
        Assert.Equal("2.0", first.Feasibility.RulesVersion);
        Assert.NotNull(first.Improvement?.Assessment);
        Assert.True(first.Improvement!.ScoreDelta > 0);
        var replay = await engine.ReplayAsync(decision.Id, input.Replace("100000", "400000"), "owner");
        Assert.Equal(2, replay.Decision.Versions.Count);
        Assert.Equal("100000", replay.Decision.Versions[0].StructuredData.Get("budget"));
        Assert.True(replay.Comparison.ScoreDelta > 0);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.ReplayAsync(decision.Id, input, "someone-else"));
    }

    [Fact]
    public async Task StructuredEntryNeverInvokesExtraction()
    {
        var language = new ExtractionMustNotRun();
        var engine = Engine(language, new());
        var decision = await engine.AnalyzeStructuredAndCreateAsync(Project(), "owner");
        Assert.NotNull(decision.GetCurrentVersion()!.Plan);
        Assert.Equal("100000", decision.GetCurrentVersion()!.StructuredData.Get("budget"));
    }

    [Fact]
    public void CostBreakdownMatchesScoredBudgetAndRecordsAssumptions()
    {
        var data = Project();
        data.Fields["monthly_cost_per_person"] = "2000";
        data.Fields["contingency_percent"] = "10";
        var result = Score(data);
        var cost = result.Economics!;
        Assert.Equal(1.12, cost.ScopeMultiplier, 6);
        Assert.Equal(13440, cost.MonthlyBurn);
        Assert.Equal(88704, cost.EstimatedCost);
        Assert.Equal(0, cost.FundingGap);
        Assert.Equal(7.44, cost.RunwayMonths);
        Assert.Contains("not a calibrated probability", result.ScoreMeaning);
    }

    [Fact]
    public void ForeignCurrencyNeedsExplicitRateAndNeverUsesAnExchangeGuess()
    {
        var data = Project();
        data.Fields["currency"] = "LKR";
        Assert.Null(Score(data).Economics);
        Assert.Equal(35, Score(data).FactorBreakdown.Single(f => f.Factor == "budget_feasibility").Score);
        data.Fields["monthly_cost_per_person"] = "2000";
        Assert.Equal("LKR", Score(data).Economics!.Currency);
    }

    [Fact]
    public void ZeroBudgetIsUnfundedRatherThanMissing()
    {
        var data = Project(); data.Fields["budget"] = "0";
        var result = Score(data);
        Assert.NotNull(result.Economics);
        Assert.Equal(result.Economics!.EstimatedCost, result.Economics.FundingGap);
        Assert.Equal(5, result.FactorBreakdown.Single(f => f.Factor == "budget_feasibility").Score);
    }

    [Theory]
    [InlineData("6 to 12 months")]
    [InlineData("-6")]
    [InlineData("0")]
    [InlineData("121")]
    [InlineData("1e309")]
    public void InvalidTimelineIsExcludedFromScoring(string value)
    {
        var data = Project(); data.Fields["timeline_months"] = value;
        var validation = _validation.Validate(data, _templates.Get(data.Domain));
        Assert.Contains("timeline_months", validation.MissingFields);
        Assert.Contains("timeline_months", validation.InvalidFields);
        Assert.Null(Score(data).Economics);
        Assert.Equal(35, Score(data).FactorBreakdown.Single(f => f.Factor == "timeline_feasibility").Score);
    }

    [Theory]
    [InlineData("not ready", 15)]
    [InlineData("not validated", 15)]
    [InlineData("unvalidated", 15)]
    [InlineData("high", 80)]
    public void ReadinessNegationCannotEarnHighScores(string readiness, double expected)
    {
        var data = Project(); data.Fields["technical_readiness"] = readiness;
        var resource = Score(data).FactorBreakdown.Single(f => f.Factor == "resource_availability");
        Assert.Equal(100 * .7 + expected * .3, resource.Score, 1);
    }

    [Fact]
    public void ModelMissingFieldClaimsDoNotOverrideTemplateValidation()
    {
        var data = Project(); data.MissingFields = new() { "budget", "invented_field" };
        Assert.Empty(_validation.Validate(data, _templates.Get(data.Domain)).MissingFields);
        data.Fields["invented_field"] = "100";
        Assert.NotEmpty(_validation.Validate(data, _templates.Get(data.Domain)).Errors);
    }

    [Fact]
    public async Task ProposedScoreIsReproducibleWithoutChangingOriginalOrInventingEvidence()
    {
        var data = Project(); var template = _templates.Get(data.Domain); var baseline = Score(data);
        var proposal = await new ImprovementPlanningService(_validation, _scoring).ProposeAsync(Guid.NewGuid(), 1, data, template, baseline);
        Assert.Equal("proposed", proposal.Status);
        Assert.Equal("100000", data.Get("budget"));
        Assert.Equal("high", proposal.ProposedFields["technical_readiness"]);
        Assert.Single(proposal.Changes);
        Assert.NotEmpty(proposal.Tradeoffs);
        var rescored = Score(new() { Domain = data.Domain, Goal = data.Goal, Fields = proposal.ProposedFields });
        Assert.Equal(rescored.FeasibilityScore, proposal.Assessment!.FeasibilityScore);
        Assert.Equal(rescored.FeasibilityScore, proposal.Plan!.FeasibilityScore);
        Assert.Equal(Math.Round(rescored.FeasibilityScore - baseline.FeasibilityScore, 1), proposal.ScoreDelta);
    }

    [Fact]
    public async Task IncompleteInputDoesNotReceiveAnInventedUpgrade()
    {
        var data = new StructuredDecisionData { Goal = "Build a mobile application", Fields = new() { ["project_goal"] = "Build a mobile application" } };
        var proposal = await new ImprovementPlanningService(_validation, _scoring).ProposeAsync(Guid.NewGuid(), 1, data, _templates.Get(data.Domain), Score(data));
        Assert.Equal("needs_information", proposal.Status);
        Assert.Null(proposal.Assessment); Assert.Null(proposal.Plan); Assert.Empty(proposal.Changes);
    }

    [Fact]
    public async Task CancellationIsPreservedInRulesOnlyMode()
    {
        using var source = new CancellationTokenSource(); source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new RulesLanguageService().ExtractDecisionAsync("Build an application", _templates.GetAll(), source.Token));
    }

    [Fact]
    public async Task AssessmentAndProposalRoundTripThroughMongoSerialization()
    {
        MongoDbConfiguration.Configure();
        var decision = await Engine(new RulesLanguageService(), new()).AnalyzeStructuredAndCreateAsync(Project(), "owner");
        var restored = BsonSerializer.Deserialize<DecisionReplay.Domain.Entities.DecisionV2>(decision.ToBson());
        var version = restored.GetCurrentVersion()!;
        Assert.Equal("2.0", version.Feasibility.RulesVersion);
        Assert.Equal(decision.GetCurrentVersion()!.Feasibility.Economics!.EstimatedCost, version.Feasibility.Economics!.EstimatedCost);
        Assert.Single(version.Improvement!.Changes);
        Assert.Equal("budget", version.Improvement.Changes[0].Field);
        Assert.Equal(version.Improvement.Assessment!.FeasibilityScore, version.Improvement.Plan!.FeasibilityScore);
    }

    [Fact]
    public void LegacyReplayDisclosesChangedScoringRules()
    {
        var previous = new DecisionVersion { Version = 1, StructuredData = Project(), Feasibility = new() { RulesVersion = "1.0", FeasibilityScore = 80 } };
        var current = new DecisionVersion { Version = 2, StructuredData = Project(), Feasibility = Score(Project()) };
        var comparison = new ReplayComparisonService().Compare(previous, current, _templates.Get("project_planning"));
        Assert.Empty(comparison.ChangedFields);
        Assert.Contains("methodology changes", comparison.MainReason);
        Assert.Contains("1.0 to 2.0", comparison.MainReason);
    }

    private FeasibilityAssessment Score(StructuredDecisionData data) => _scoring.Calculate(data, _templates.Get(data.Domain), _validation.Validate(data, _templates.Get(data.Domain)));
    private DecisionEngineService Engine(IAiLanguageService language, InMemoryDecisionV2Repository repository) =>
        new(new(language, _templates), _validation, _templates, _scoring, new(), new(language), new(new(), language), new(), language, repository);
    private static StructuredDecisionData Project() => new() { Domain = "project_planning", Title = "Mobile application", Goal = "Build a mobile application", Fields = new()
    { ["project_goal"] = "Build a mobile application", ["budget"] = "100000", ["timeline_months"] = "6", ["team_size"] = "6",
        ["scope"] = "login, payments, reporting", ["currency"] = "USD", ["technical_readiness"] = "high" } };
    private sealed class ExtractionMustNotRun : IAiLanguageService
    {
        public bool IsConfigured => false;
        public Task<StructuredDecisionData> ExtractDecisionAsync(string input, IReadOnlyCollection<DecisionDomainTemplate> templates, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Extraction must not run for structured entry.");
        public Task<string> ExplainResultAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, CancellationToken cancellationToken = default) => Task.FromResult("");
        public Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, ActionPlan plan, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlanTaskEnhancement>>(Array.Empty<PlanTaskEnhancement>());
        public Task<string> SummarizeReplayAsync(ReplayComparison comparison, CancellationToken cancellationToken = default) => Task.FromResult("");
    }
}
