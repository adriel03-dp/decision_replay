using System.Text.Json;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Persistence.Repositories;

namespace DecisionReplay.Tests;

public sealed class HistoricalReplayTests
{
    [Fact]
    public void EvidenceKnownAfterT0CannotEnterReplayContext()
    {
        var context = AiTestHarness.Context();
        context.Evidence[0].KnownAt = context.DecisionAt.AddDays(1);
        Assert.Throws<ArgumentException>(() => ReplayContextService.T0Input(context));
    }

    [Fact]
    public async Task InitialReplayCannotSeeStoredOutcomesAndReflectionRevealsThemExplicitly()
    {
        var harness = new AiTestHarness(send: (request, _) => Task.FromResult(new AiGenerationResponse(
            request.SystemPrompt.Contains("T1 outcome reflection")
                ? """{"decisionProcessAssessment":"Reasonable using limited T0 evidence","outcomeAssessment":"The observed result was negative","luckAndUncertainty":"Causality is uncertain","lessons":["Validate demand"]}"""
                : AiTestHarness.AnalysisJson())));
        var (repository, decision) = await StoredDecision();
        var service = Service(repository, harness);
        var outcome = await service.RecordOutcomeAsync(decision.Id, "test", new()
        {
            DecisionVersion = 1, ObservedAt = new(2025, 2, 1, 10, 0, 0, DateTimeKind.Utc),
            Description = "OUTCOME_SENTINEL_731 loss occurred", OutcomeQuality = "negative"
        });
        var analysed = await service.AnalyseAsync(decision.Id, "test", 1, null);
        Assert.DoesNotContain("OUTCOME_SENTINEL_731", harness.Provider.Requests[0].UserPrompt);
        Assert.DoesNotContain("recordedOutcomes", harness.Provider.Requests[0].UserPrompt);
        var reflection = await service.ReflectAsync(decision.Id, "test", outcome.Id, analysed.Id, null);
        Assert.Contains("OUTCOME_SENTINEL_731", harness.Provider.Requests[1].UserPrompt);
        Assert.NotEmpty(reflection.Value.DecisionProcessAssessment);
        var saved = (await repository.GetByIdAsync(decision.Id))!;
        Assert.Equal(analysed.Analysis.Analysis, saved.Versions[0].AiAnalyses[0].Analysis.Analysis);
        Assert.Equal(decision.Versions[0].Feasibility.FeasibilityScore, saved.Versions[0].Feasibility.FeasibilityScore);
    }

    [Fact]
    public async Task OutcomeReflectionRequiresACompletedOriginalT0Analysis()
    {
        var (repository, decision) = await StoredDecision();
        var service = Service(repository, new());
        var outcome = await service.RecordOutcomeAsync(decision.Id, "test", new()
        { DecisionVersion = 1, ObservedAt = new(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), Description = "Observed result" });
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReflectAsync(decision.Id, "test", outcome.Id, Guid.NewGuid(), null));
    }

    [Fact]
    public async Task ScenarioChangesPreserveOriginalVersionAndContext()
    {
        var (repository, decision) = await StoredDecision();
        var harness = new AiTestHarness();
        var service = Service(repository, harness);
        var scenario = await service.CreateScenarioAsync(decision.Id, "test", new("Reduced budget", 1,
            new() { ["budget"] = "60000" }, new() { ["a1"] = "Demand may be medium" }, new()));
        var original = (await repository.GetByIdAsync(decision.Id))!;
        Assert.Equal(1, original.CurrentVersion);
        Assert.Equal("100000", original.Versions[0].Context!.Fields["budget"]);
        Assert.Equal("Demand will be high", original.Versions[0].Context!.Assumptions[0].Text);
        Assert.Equal("60000", scenario.Context.Fields["budget"]);
        Assert.Equal("Demand may be medium", scenario.Context.Assumptions[0].Text);
        Assert.Contains(scenario.Comparison.ChangedFields, change => change.Field == "budget");
        await service.AnalyseAsync(decision.Id, "test", 1, scenario.Id);
        Assert.Contains("60000", harness.Provider.Requests.Single().UserPrompt);
        Assert.Equal(scenario.Id, (await harness.Runs.GetExecutionsAsync("test")).Single().ReplayId);
    }

    [Fact]
    public async Task RecordingContextCreatesANewVersionAndInvalidatesOldDerivedResults()
    {
        var (repository, decision) = await StoredDecision();
        var context = AiTestHarness.Context(); context.Fields["budget"] = "50000";
        var service = Service(repository, new());
        Assert.Equal(2, await service.RecordContextAsync(decision.Id, "test", context));
        var saved = (await repository.GetByIdAsync(decision.Id))!;
        Assert.Equal("100000", saved.Versions[0].Context!.Fields["budget"]);
        Assert.Equal("50000", saved.Versions[1].Context!.Fields["budget"]);
        Assert.Null(saved.Versions[1].Plan);
        Assert.Empty(saved.Versions[1].AiAnalyses);
    }

    [Fact]
    public async Task OwnersAreIsolatedAndStaleWritesCannotOverwriteHistory()
    {
        var (repository, decision) = await StoredDecision();
        Assert.Null(await repository.GetByIdForUserAsync(decision.Id, "other-user"));
        var first = (await repository.GetByIdAsync(decision.Id))!;
        var stale = (await repository.GetByIdAsync(decision.Id))!;
        first.OriginalInput = "changed locally";
        Assert.NotEqual(first.OriginalInput, (await repository.GetByIdAsync(decision.Id))!.OriginalInput);
        await repository.UpdateAsync(first);
        await Assert.ThrowsAsync<DecisionConflictException>(() => repository.UpdateAsync(stale));
    }

    [Fact]
    public async Task RegeneratingPlanPreservesHistoricalPlanAndCreatesAVersion()
    {
        var (repository, decision) = await StoredDecision();
        var original = (await repository.GetByIdAsync(decision.Id))!;
        original.Versions[0].Plan = new() { PlanId = Guid.NewGuid(), DecisionId = decision.Id, Version = 1 };
        var oldPlanId = original.Versions[0].Plan!.PlanId;
        await repository.UpdateAsync(original);
        var language = new NoAiLanguage();
        var templates = new DomainTemplateService();
        var engine = new DecisionEngineService(new(language, templates), new(), templates, new(), new(), new(language),
            new(new(), language), new(), language, repository);
        var newPlan = await engine.RegeneratePlanAsync(decision.Id, "test");
        Assert.Equal(2, newPlan.Version);
        Assert.NotEqual(oldPlanId, newPlan.PlanId);
        Assert.Equal(oldPlanId, (await engine.GetPlanAsync(decision.Id, oldPlanId, "test")).PlanId);
    }

    internal static HistoricalReplayService Service(IDecisionV2Repository repository, AiTestHarness harness) =>
        new(repository, harness.Workflow, new(), new(), new(), harness.Scope);

    internal static async Task<(InMemoryDecisionV2Repository Repository, DecisionV2 Decision)> StoredDecision()
    {
        var context = AiTestHarness.Context();
        var structured = new StructuredDecisionData { Domain = "project_planning", Goal = context.ExpectedOutcome, Fields = new(context.Fields) };
        var template = new DomainTemplateService().Get(structured.Domain);
        var validation = new DecisionValidationService().Validate(structured, template);
        var decision = new DecisionV2(Guid.NewGuid(), context.DecisionText, "test", new()
        { Version = 1, NaturalLanguageInput = context.DecisionText, StructuredData = structured, Context = context,
            Feasibility = new FeasibilityScoringService().Calculate(structured, template, validation), Validation = validation });
        var repository = new InMemoryDecisionV2Repository();
        await repository.CreateAsync(decision);
        return (repository, decision);
    }

    private sealed class NoAiLanguage : IAiLanguageService
    {
        public bool IsConfigured => false;
        public Task<StructuredDecisionData> ExtractDecisionAsync(string input, IReadOnlyCollection<DecisionDomainTemplate> templates, CancellationToken token = default) => Task.FromResult(new StructuredDecisionData());
        public Task<string> ExplainResultAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, CancellationToken token = default) => Task.FromResult("");
        public Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, ActionPlan plan, CancellationToken token = default) => Task.FromResult<IReadOnlyList<PlanTaskEnhancement>>(Array.Empty<PlanTaskEnhancement>());
        public Task<string> SummarizeReplayAsync(ReplayComparison comparison, CancellationToken token = default) => Task.FromResult("");
    }
}
