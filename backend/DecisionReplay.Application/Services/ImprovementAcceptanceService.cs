using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

/// <summary>Adopts only a current, server-generated proposal after explicit user confirmation.</summary>
public sealed class ImprovementAcceptanceService(IDecisionV2Repository decisions, DomainTemplateService templates,
    DecisionValidationService validation, FeasibilityScoringService scoring, PlanPhaseBuilderService phases,
    ImprovementPlanningService improvements, ReplayComparisonService replay, AuditTrailService audit)
{
    public async Task<DecisionReplayResultV3> AcceptAsync(Guid id, string owner, int sourceVersion, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var decision = await decisions.GetByIdForUserAsync(id, owner) ?? throw new KeyNotFoundException("Decision not found.");
        var previous = decision.GetCurrentVersion() ?? throw new ArgumentException("Decision has no current version.");
        if (previous.Version != sourceVersion) throw new DecisionConflictException();
        if (decision.Versions.Count >= 100) throw new ArgumentException("Decision version limit reached.");
        var proposal = previous.Improvement;
        if (proposal?.Status != "proposed" || proposal.Assessment == null || proposal.ConstraintCheck?.EstimatesWithinLimits != true)
            throw new ArgumentException("No checked improvement is available. Regenerate the plan before adopting it.");
        if (previous.Feasibility.RulesVersion != FeasibilityScoringService.CurrentRulesVersion
            || proposal.Assessment.RulesVersion != FeasibilityScoringService.CurrentRulesVersion)
            throw new DecisionConflictException("The scoring rules changed. Regenerate the plan before adopting its proposal.");

        var template = templates.Get(previous.StructuredData.Domain);
        var structured = ReplayContextService.Copy(previous.StructuredData);
        structured.Fields = new(proposal.ProposedFields, StringComparer.OrdinalIgnoreCase);
        foreach (var policy in new[] { "fixed_fields", "required_scope" })
            if (!string.Equals(previous.StructuredData.Get(policy), structured.Get(policy), StringComparison.OrdinalIgnoreCase))
                throw new DecisionConflictException();
        var changedNames = previous.StructuredData.Fields.Keys.Union(structured.Fields.Keys, StringComparer.OrdinalIgnoreCase)
            .Where(name => !string.Equals(previous.StructuredData.Get(name), structured.Get(name), StringComparison.OrdinalIgnoreCase)).ToList();
        if (changedNames.Count == 0 || changedNames.Any(name => name.ToLowerInvariant() is not ("budget" or "timeline_months" or "scope")))
            throw new DecisionConflictException();
        var checkedData = validation.Validate(structured, template);
        if (checkedData.Errors.Count > 0 || checkedData.InvalidFields.Count > 0) throw new DecisionConflictException();
        var baseline = scoring.Calculate(previous.StructuredData, template, validation.Validate(previous.StructuredData, template));
        var assessment = scoring.Calculate(structured, template, checkedData);
        var constraints = ImprovementConstraintPolicy.Check(previous.StructuredData, structured, template, assessment);
        if (!constraints.EstimatesWithinLimits || assessment.FeasibilityScore <= baseline.FeasibilityScore
            || assessment.RiskLevel > baseline.RiskLevel || assessment.FeasibilityScore != proposal.Assessment.FeasibilityScore)
            throw new DecisionConflictException();

        var version = previous.Version + 1;
        var start = constraints.FixedFields.Contains("timeline_months") ? previous.Plan?.StartDate ?? previous.CreatedAt.Date : (DateTime?)null;
        // Adoption remains deterministic even when optional language-model wording is enabled.
        var language = new RulesLanguageService();
        var plan = await new ActionPlanService(phases, language).GenerateAsync(id, version, structured, template, assessment, token, start);
        structured.MissingFields = checkedData.MissingFields.ToList();
        var next = new DecisionVersion
        {
            Version = version, NaturalLanguageInput = $"Adopted the checked improvement from version {sourceVersion} after reviewing its tradeoffs.",
            StructuredData = structured, Validation = checkedData, Feasibility = assessment, Plan = plan,
            Explanation = await new ExplanationService(language).ExplainAsync(structured, assessment, token), CreatedAt = DateTime.UtcNow,
            Improvement = await improvements.ProposeAsync(id, version, structured, template, assessment, token, plan.StartDate)
        };
        next.Context = ReplayContextService.Snapshot(previous);
        next.Context.DecisionAt = next.CreatedAt;
        next.Context.DecisionText = next.NaturalLanguageInput;
        next.Context.Fields = new(structured.Fields, StringComparer.OrdinalIgnoreCase);
        next.Context.ChosenAction = structured.Goal;
        next.Context.Provenance = $"user-accepted improvement from version {sourceVersion}; prior evidence retained without revealing later outcomes";
        decision.AddVersion(next);
        decision.AuditTrail.Add(audit.Create(AuditActionType.ImprovementAccepted, version, owner,
            "The user adopted a checked plan improvement as a new decision version.",
            new Dictionary<string, string> { ["sourceVersion"] = sourceVersion.ToString(), ["changedFields"] = string.Join(",", changedNames), ["rulesVersion"] = assessment.RulesVersion }));
        token.ThrowIfCancellationRequested();
        await decisions.UpdateAsync(decision);
        return new(decision, replay.Compare(previous, next, template));
    }
}
