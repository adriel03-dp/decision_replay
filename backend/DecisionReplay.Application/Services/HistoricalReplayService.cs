using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class HistoricalReplayService(IDecisionV2Repository decisions, AiWorkflowService ai,
    DomainTemplateService templates, DecisionValidationService validation, FeasibilityScoringService scoring,
    AiExecutionScope executions)
{
    private async Task<DecisionV2> Owned(Guid id, string owner) =>
        await decisions.GetByIdForUserAsync(id, owner) ?? throw new KeyNotFoundException("Decision not found.");

    public async Task<DecisionTimeline> GetTimelineAsync(Guid id, string owner)
    {
        var decision = await Owned(id, owner);
        return new(decision.Versions.OrderBy(version => version.Version).Select(version =>
            new HistoricalVersion(version.Version, ReplayContextService.Snapshot(version), version.AiAnalyses)).ToList(),
            decision.RecordedOutcomes, decision.ReplayScenarios);
    }

    public async Task<int> RecordContextAsync(Guid id, string owner, DecisionContextSnapshot input)
    {
        ReplayContextService.Validate(input);
        var decision = await Owned(id, owner);
        if (decision.Versions.Count >= 100) throw new ArgumentException("Decision version limit reached.");
        var previous = decision.GetCurrentVersion()!;
        var next = ReplayContextService.Copy(previous);
        next.Version++;
        next.CreatedAt = DateTime.UtcNow;
        next.Context = ReplayContextService.Copy(input);
        next.Context.Provenance = "user-attested; not independently verified";
        next.NaturalLanguageInput = input.DecisionText;
        next.StructuredData.Fields = new(input.Fields);
        next.StructuredData.Goal = input.ExpectedOutcome;
        next.StructuredData.Assumptions = input.Assumptions.Select(item => item.Text).ToList();
        next.StructuredData.MissingFields = new();
        var template = templates.Get(next.StructuredData.Domain);
        ValidateFields(next.StructuredData.Fields, template);
        next.Validation = validation.Validate(next.StructuredData, template);
        next.Feasibility = scoring.Calculate(next.StructuredData, template, next.Validation);
        // A changed context must not inherit an explanation, analysis or plan for different inputs.
        next.Explanation = "A new historical context was recorded. Review its deterministic assessment and run a T0 analysis.";
        next.Plan = null;
        next.AiAnalyses = new();
        decision.AddVersion(next);
        decision.AuditTrail.Add(new AuditTrailService().Create(AuditActionType.HistoricalContextRecorded, next.Version, owner,
            "A new user-attested T0 snapshot was recorded; previous snapshots were preserved."));
        await decisions.UpdateAsync(decision);
        return next.Version;
    }

    public async Task<RecordedOutcome> RecordOutcomeAsync(Guid id, string owner, RecordedOutcome input)
    {
        var decision = await Owned(id, owner);
        var version = decision.Versions.FirstOrDefault(version => version.Version == input.DecisionVersion)
            ?? throw new ArgumentException("Unknown decision version.");
        if (input.ObservedAt.Kind != DateTimeKind.Utc || input.ObservedAt < ReplayContextService.Snapshot(version).DecisionAt
            || input.ObservedAt > DateTime.UtcNow || !AiOutputValidation.Text(input.Description, 8000)
            || input.OutcomeQuality is not ("positive" or "negative" or "mixed" or "unknown"))
            throw new ArgumentException("Outcome needs a UTC T1 timestamp at or after T0, valid description, and positive/negative/mixed/unknown quality.");
        if (decision.RecordedOutcomes.Count >= 100) throw new ArgumentException("Outcome record limit reached.");
        var outcome = new RecordedOutcome { DecisionVersion = input.DecisionVersion, ObservedAt = input.ObservedAt,
            Description = input.Description, OutcomeQuality = input.OutcomeQuality };
        decision.RecordedOutcomes.Add(outcome);
        decision.AuditTrail.Add(new AuditTrailService().Create(AuditActionType.OutcomeRecorded, outcome.DecisionVersion, owner,
            "A separate T1 outcome was appended without changing T0.", new Dictionary<string, string> { ["outcomeId"] = outcome.Id.ToString() }));
        await decisions.UpdateAsync(decision);
        return outcome;
    }

    public async Task<ReplayScenario> CreateScenarioAsync(Guid id, string owner, ScenarioChanges changes)
    {
        if (!AiOutputValidation.Text(changes.Name, 200) || changes.Fields == null || changes.Assumptions == null || changes.Constraints == null
            || changes.Fields.Count + changes.Assumptions.Count + changes.Constraints.Count == 0
            || changes.Fields.Count + changes.Assumptions.Count + changes.Constraints.Count > 100)
            throw new ArgumentException("Supply a scenario name and between 1 and 100 explicit changes.");
        var decision = await Owned(id, owner);
        if (decision.ReplayScenarios.Count >= 50) throw new ArgumentException("Scenario limit reached.");
        var original = decision.Versions.FirstOrDefault(version => version.Version == changes.BaseVersion)
            ?? throw new ArgumentException("Unknown base decision version.");
        var snapshot = ReplayContextService.Snapshot(original);
        var template = templates.Get(original.StructuredData.Domain);
        ValidateFields(changes.Fields, template);
        foreach (var (name, value) in changes.Fields) snapshot.Fields[name] = value;
        ApplyStatements(snapshot.Assumptions, changes.Assumptions);
        ApplyStatements(snapshot.Constraints, changes.Constraints);
        snapshot.Provenance += "; explicitly simulated what-if values";
        ReplayContextService.Validate(snapshot);
        var structured = ReplayContextService.Copy(original.StructuredData);
        structured.Fields = new(snapshot.Fields);
        structured.Assumptions = snapshot.Assumptions.Select(item => item.Text).ToList();
        structured.MissingFields = new();
        var checkedData = validation.Validate(structured, template);
        var assessment = scoring.Calculate(structured, template, checkedData);
        var scenario = new ReplayScenario
        {
            Name = changes.Name, BaseVersion = changes.BaseVersion, Context = snapshot,
            FieldChanges = new(changes.Fields), AssumptionChanges = new(changes.Assumptions), ConstraintChanges = new(changes.Constraints),
            Assessment = assessment,
            Comparison = new ReplayComparisonService().Compare(original,
                new DecisionVersion { Version = original.Version, StructuredData = structured, Feasibility = assessment }, template)
        };
        scenario.Comparison.MainReason += " Narrative-only assumptions and constraints do not alter numeric scores unless their corresponding structured fields change.";
        decision.ReplayScenarios.Add(scenario);
        decision.AuditTrail.Add(new AuditTrailService().Create(AuditActionType.ScenarioCreated, scenario.BaseVersion, owner,
            "An immutable what-if scenario was created.", new Dictionary<string, string> { ["scenarioId"] = scenario.Id.ToString() }));
        await decisions.UpdateAsync(decision);
        return scenario;
    }

    public async Task<ReplayAnalysisRecord> AnalyseAsync(Guid id, string owner, int versionNumber, Guid? scenarioId,
        string promptVersion = "v3", CancellationToken cancellationToken = default)
    {
        var decision = await Owned(id, owner);
        var version = decision.Versions.FirstOrDefault(version => version.Version == versionNumber)
            ?? throw new ArgumentException("Unknown decision version.");
        var scenario = scenarioId == null ? null : decision.ReplayScenarios.FirstOrDefault(item => item.Id == scenarioId)
            ?? throw new KeyNotFoundException("Scenario not found.");
        if (scenario != null && scenario.BaseVersion != versionNumber) throw new ArgumentException("Scenario does not belong to the requested base version.");
        var records = scenario?.Analyses ?? version.AiAnalyses;
        if (records.Count >= 20) throw new ArgumentException("Analysis history limit reached.");
        var snapshot = scenario?.Context ?? ReplayContextService.Snapshot(version);
        var result = await ai.GenerateAsync<DecisionAiAnalysis>("decision-analysis", promptVersion,
            ReplayContextService.T0Input(snapshot), output => AiOutputValidation.Analysis(output, snapshot, promptVersion == "v3"),
            executions.Context with { OwnerId = owner, DecisionId = id, ContextVersion = versionNumber, ReplayId = scenarioId },
            outputSchema: promptVersion == "v3" ? AiOutputSchema.Analysis(snapshot) : null,
            cancellationToken: cancellationToken);
        var record = new ReplayAnalysisRecord { ExecutionId = result.ExecutionId, Analysis = result.Value };
        records.Add(record);
        decision.AuditTrail.Add(new AuditTrailService().Create(AuditActionType.HistoricalAnalysisCompleted, versionNumber, owner,
            "T0-only advisory analysis completed.", new Dictionary<string, string> { ["executionId"] = result.ExecutionId.ToString() }));
        await decisions.UpdateAsync(decision);
        return record;
    }

    public async Task<ValidatedAiResult<OutcomeReflection>> ReflectAsync(Guid id, string owner, Guid outcomeId,
        Guid analysisId, Guid? scenarioId, CancellationToken cancellationToken = default)
    {
        var decision = await Owned(id, owner);
        var outcome = decision.RecordedOutcomes.FirstOrDefault(item => item.Id == outcomeId)
            ?? throw new KeyNotFoundException("Outcome not found.");
        var version = decision.Versions.First(item => item.Version == outcome.DecisionVersion);
        if (scenarioId != null) throw new ArgumentException("Observed outcomes are compared only against the original T0 analysis, not a hypothetical scenario.");
        var initial = version.AiAnalyses.FirstOrDefault(item => item.Id == analysisId)
            ?? throw new ArgumentException("Run an original T0 analysis for this outcome's decision version before revealing T1.");
        return await ai.GenerateAsync<OutcomeReflection>("outcome-reflection", "v1",
            new { t0 = ReplayContextService.T0Input(ReplayContextService.Snapshot(version)), initialAnalysis = initial.Analysis, revealedOutcome = outcome },
            AiOutputValidation.Reflection, executions.Context with { OwnerId = owner, DecisionId = id, ContextVersion = version.Version },
            cancellationToken: cancellationToken);
    }

    private static void ValidateFields(Dictionary<string, string> fields, DecisionDomainTemplate template)
    {
        if (fields.Any(pair => !template.Fields.Any(field => field.Name == pair.Key) || !AiOutputValidation.Text(pair.Value, 20000)))
            throw new ArgumentException("Context contains unsupported or empty structured fields.");
    }
    private static void ApplyStatements(List<ContextStatement> statements, Dictionary<string, string> changes)
    {
        foreach (var (id, text) in changes)
        {
            var statement = statements.FirstOrDefault(item => item.Id == id) ?? throw new ArgumentException("Unknown statement ID in scenario changes.");
            if (!AiOutputValidation.Text(text)) throw new ArgumentException("Scenario statement is empty or too long.");
            statement.Text = text;
        }
    }
}

public sealed record ScenarioChanges(string Name, int BaseVersion, Dictionary<string, string> Fields,
    Dictionary<string, string> Assumptions, Dictionary<string, string> Constraints);
public sealed record HistoricalVersion(int Version, DecisionContextSnapshot Context, IReadOnlyList<ReplayAnalysisRecord> Analyses);
public sealed record DecisionTimeline(IReadOnlyList<HistoricalVersion> Versions, IReadOnlyList<RecordedOutcome> Outcomes,
    IReadOnlyList<ReplayScenario> Scenarios);
