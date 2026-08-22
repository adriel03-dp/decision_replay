using System.Globalization;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

/// <summary>Evaluates a small, bounded set of explicit what-if changes using the original scoring rules.</summary>
public sealed class ImprovementPlanningService(DecisionValidationService validation, FeasibilityScoringService scoring)
{
    public async Task<ImprovementProposal> ProposeAsync(Guid decisionId, int version, StructuredDecisionData original,
        DecisionDomainTemplate template, FeasibilityAssessment baseline, CancellationToken cancellationToken = default,
        DateTime? planStartDate = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var originalValidation = validation.Validate(original, template);
        var result = new ImprovementProposal { MissingFields = originalValidation.MissingFields.ToList() };
        if (originalValidation.Errors.Count > 0) throw new ArgumentException(string.Join(" ", originalValidation.Errors));
        if (template.Domain is not ("project_planning" or "product_launch" or "business_startup")) return result;
        result.ConstraintCheck = ImprovementConstraintPolicy.Check(original, original, template, baseline);
        AnchorDeadline(result.ConstraintCheck);
        var fixedFields = result.ConstraintCheck.FixedFields;
        var candidates = new List<(Dictionary<string, string> Changes, string Tradeoff)>();
        if (!fixedFields.Contains("budget") && baseline.Economics is { } economics && baseline.FactorBreakdown.Any(f => f.Factor == "budget_feasibility" && f.Score < 100))
            candidates.Add((new() { ["budget"] = Math.Ceiling(economics.EstimatedCost * 1.25).ToString(CultureInfo.InvariantCulture) },
                "Requires additional funding. The proposed budget targets 1.25 times the estimated cost; it does not establish that funding is available."));
        if (!fixedFields.Contains("scope"))
            foreach (var reducedScope in ScopePriorityRules.ReducedScopes(original))
                candidates.Add((new() { ["scope"] = reducedScope },
                    "Defers optional deliverables while preserving every selected must-have. Confirm that the retained scope is useful and dependencies still hold."));
        if (!fixedFields.Contains("timeline_months") && DecisionValidationService.TryNumber(original.Get("timeline_months"), out var months) && months is > 0 and < 120)
            candidates.Add((new() { ["timeline_months"] = Math.Min(120, Math.Ceiling(months * 1.5)).ToString(CultureInfo.InvariantCulture) },
                "Moves the delivery deadline and increases funded time. Confirm that the deadline and resulting cost are acceptable."));

        // Single changes keep the reason for any improvement inspectable. Never invent evidence/readiness.
        StructuredDecisionData? best = null;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var proposed = new StructuredDecisionData { Domain = original.Domain, Title = original.Title, Goal = original.Goal,
                Fields = new(original.Fields, StringComparer.OrdinalIgnoreCase), Assumptions = original.Assumptions.ToList() };
            foreach (var pair in candidate.Changes) proposed.Fields[pair.Key] = pair.Value;
            var checkedData = validation.Validate(proposed, template);
            if (checkedData.Errors.Count > 0 || checkedData.InvalidFields.Count > 0) continue;
            var assessment = scoring.Calculate(proposed, template, checkedData);
            var constraints = ImprovementConstraintPolicy.Check(original, proposed, template, assessment);
            AnchorDeadline(constraints);
            if (!constraints.EstimatesWithinLimits) continue;
            var referenceScore = result.Assessment?.FeasibilityScore ?? baseline.FeasibilityScore;
            if (assessment.FeasibilityScore <= referenceScore || assessment.RiskLevel > baseline.RiskLevel) continue;
            best = proposed;
            result.Status = "proposed";
            result.Assessment = assessment;
            result.ConstraintCheck = constraints;
            result.ProposedFields = new(proposed.Fields, StringComparer.OrdinalIgnoreCase);
            result.MissingFields = checkedData.MissingFields;
            result.ScoreDelta = Math.Round(assessment.FeasibilityScore - baseline.FeasibilityScore, 1);
            result.Changes = candidate.Changes.Select(pair => new ProposedFieldChange(pair.Key, original.Get(pair.Key) ?? "", pair.Value)).ToList();
            result.Tradeoffs = new() { candidate.Tradeoff };
        }
        if (best != null)
        {
            // Proposal generation is always local, even when optional AI wording is enabled elsewhere.
            var plans = new ActionPlanService(new PlanPhaseBuilderService(), new RulesLanguageService());
            result.Plan = await plans.GenerateAsync(decisionId, version, best, template, result.Assessment!, cancellationToken, planStartDate);
        }
        else if (result.MissingFields.Count > 0 || result.ConstraintCheck.NeedsInformation) result.Status = "needs_information";
        else if (!result.ConstraintCheck.EstimatesWithinLimits) result.Status = "constraints_unsatisfied";
        return result;

        void AnchorDeadline(ImprovementConstraintCheck check)
        {
            if (check.TimelineCeilingMonths is { } months)
                check.DeadlineDate = DeliveryScheduleRules.EndDate(planStartDate?.Date ?? DateTime.UtcNow.Date, months);
        }
    }
}
