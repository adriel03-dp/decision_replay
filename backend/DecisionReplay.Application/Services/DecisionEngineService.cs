using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class DecisionEngineService
{
    private readonly DecisionParserService _parser;
    private readonly DecisionValidationService _validation;
    private readonly DomainTemplateService _templates;
    private readonly FeasibilityScoringService _scoring;
    private readonly ReplayComparisonService _replay;
    private readonly ExplanationService _explanation;
    private readonly ActionPlanService _plans;
    private readonly AuditTrailService _audit;
    private readonly IAiLanguageService _languageService;
    private readonly IDecisionV2Repository _repository;

    public DecisionEngineService(
        DecisionParserService parser,
        DecisionValidationService validation,
        DomainTemplateService templates,
        FeasibilityScoringService scoring,
        ReplayComparisonService replay,
        ExplanationService explanation,
        ActionPlanService plans,
        AuditTrailService audit,
        IAiLanguageService languageService,
        IDecisionV2Repository repository)
    {
        _parser = parser;
        _validation = validation;
        _templates = templates;
        _scoring = scoring;
        _replay = replay;
        _explanation = explanation;
        _plans = plans;
        _audit = audit;
        _languageService = languageService;
        _repository = repository;
    }

    public async Task<DecisionV2> AnalyzeAndCreateAsync(
        string naturalLanguageInput,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var decisionId = Guid.NewGuid();
        var version = await BuildVersionAsync(
            decisionId,
            1,
            naturalLanguageInput,
            cancellationToken);
        var decision = new DecisionV2(
            decisionId,
            naturalLanguageInput,
            userId,
            version);

        decision.AuditTrail.AddRange(new[]
        {
            _audit.Create(
                AuditActionType.DecisionCreated,
                1,
                userId,
                "Decision record created from natural-language input."),
            _audit.Create(
                AuditActionType.InputParsed,
                1,
                "system",
                $"Input extracted into the {version.StructuredData.Domain} template.",
                new Dictionary<string, string>
                {
                    ["fieldCount"] = version.StructuredData.Fields.Count.ToString(),
                    ["missingFieldCount"] = version.Validation.MissingFields.Count.ToString()
                }),
            _audit.Create(
                AuditActionType.FeasibilityCalculated,
                1,
                "system",
                "Deterministic feasibility rules were applied.",
                new Dictionary<string, string>
                {
                    ["score"] = version.Feasibility.FeasibilityScore.ToString("0.0"),
                    ["riskLevel"] = version.Feasibility.RiskLevel.ToString(),
                    ["rulesVersion"] = version.Feasibility.RulesVersion
                }),
            _audit.Create(
                AuditActionType.PlanGenerated,
                1,
                "system",
                "A constraint-bound action plan was generated.",
                new Dictionary<string, string>
                {
                    ["planId"] = version.Plan?.PlanId.ToString() ?? string.Empty,
                    ["phaseCount"] = version.Plan?.Phases.Count.ToString() ?? "0"
                })
        });

        await _repository.CreateAsync(decision);
        return decision;
    }

    public async Task<DecisionReplayResultV3> ReplayAsync(
        Guid decisionId,
        string updatedInput,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var decision = await _repository.GetByIdForUserAsync(decisionId, userId)
            ?? throw new KeyNotFoundException("Decision not found.");
        var previous = decision.GetCurrentVersion()
            ?? throw new InvalidOperationException("Decision has no version to replay.");
        var next = await BuildVersionAsync(
            decision.Id,
            previous.Version + 1,
            updatedInput,
            cancellationToken);
        decision.AddVersion(next);

        var template = _templates.Get(next.StructuredData.Domain);
        var comparison = _replay.Compare(previous, next, template);
        comparison.LanguageSummary = await _languageService.SummarizeReplayAsync(
            comparison,
            cancellationToken);

        decision.AuditTrail.Add(_audit.Create(
            AuditActionType.ReplayCreated,
            next.Version,
            userId,
            "A new decision version was calculated and compared.",
            new Dictionary<string, string>
            {
                ["previousVersion"] = previous.Version.ToString(),
                ["newVersion"] = next.Version.ToString(),
                ["scoreDelta"] = comparison.ScoreDelta.ToString("0.0"),
                ["riskDelta"] = comparison.RiskDelta
            }));
        await _repository.UpdateAsync(decision);

        return new DecisionReplayResultV3(decision, comparison);
    }

    public async Task<ActionPlan> RegeneratePlanAsync(
        Guid decisionId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var decision = await _repository.GetByIdForUserAsync(decisionId, userId)
            ?? throw new KeyNotFoundException("Decision not found.");
        var version = decision.GetCurrentVersion()
            ?? throw new InvalidOperationException("Decision has no current version.");
        var template = _templates.Get(version.StructuredData.Domain);
        version.Plan = await _plans.GenerateAsync(
            decision.Id,
            version.Version,
            version.StructuredData,
            template,
            version.Feasibility,
            cancellationToken);

        decision.AuditTrail.Add(_audit.Create(
            AuditActionType.PlanGenerated,
            version.Version,
            userId,
            "The current version action plan was regenerated.",
            new Dictionary<string, string> { ["planId"] = version.Plan.PlanId.ToString() }));
        await _repository.UpdateAsync(decision);
        return version.Plan;
    }

    public async Task<ActionPlan> GetPlanAsync(
        Guid decisionId,
        Guid planId,
        string userId)
    {
        var decision = await _repository.GetByIdForUserAsync(decisionId, userId)
            ?? throw new KeyNotFoundException("Decision not found.");
        return decision.Versions
            .Select(version => version.Plan)
            .FirstOrDefault(plan => plan?.PlanId == planId)
            ?? throw new KeyNotFoundException("Plan not found.");
    }

    private async Task<DecisionVersion> BuildVersionAsync(
        Guid decisionId,
        int versionNumber,
        string naturalLanguageInput,
        CancellationToken cancellationToken)
    {
        var structured = await _parser.ParseAsync(naturalLanguageInput, cancellationToken);
        var template = _templates.Get(structured.Domain);
        structured.Domain = template.Domain;

        var validation = _validation.Validate(structured, template);
        if (validation.Errors.Count > 0)
            throw new ArgumentException(string.Join(" ", validation.Errors));

        structured.MissingFields = validation.MissingFields;
        var feasibility = _scoring.Calculate(structured, template, validation);
        var plan = await _plans.GenerateAsync(
            decisionId,
            versionNumber,
            structured,
            template,
            feasibility,
            cancellationToken);
        var explanation = await _explanation.ExplainAsync(
            structured,
            feasibility,
            cancellationToken);

        return new DecisionVersion
        {
            Version = versionNumber,
            NaturalLanguageInput = naturalLanguageInput.Trim(),
            StructuredData = structured,
            Validation = validation,
            Feasibility = feasibility,
            Plan = plan,
            Explanation = explanation,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public sealed record DecisionReplayResultV3(
    DecisionV2 Decision,
    ReplayComparison Comparison);
