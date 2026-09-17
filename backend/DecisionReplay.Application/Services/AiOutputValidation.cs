using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class AiOutputValidation
{
    public static bool Text(string? value, int max = 4000) => !string.IsNullOrWhiteSpace(value) && value.Length <= max;
    public static bool Strings(IReadOnlyCollection<string>? values, int maxItems = 50) =>
        values != null && values.Count <= maxItems && values.All(item => Text(item));

    public static string? Analysis(DecisionAiAnalysis output, DecisionContextSnapshot context, bool compact = false)
    {
        if (!Text(output.Analysis, 8000) || !double.IsFinite(output.Confidence) || output.Confidence < 0 || output.Confidence > 1)
            return "Analysis text or confidence is invalid.";
        if (output.Assumptions == null || output.Risks == null || output.Alternatives == null
            || output.Assumptions.Count > 50 || output.Risks.Count > 50 || output.Alternatives.Count is < 1 or > 20
            || !Strings(output.MissingInformation)) return "Analysis collections are invalid.";
        var evidenceIds = context.Evidence.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        bool References(List<string>? ids, bool hypothesis) => ids != null && ids.Count <= 50
            && ids.Distinct(StringComparer.Ordinal).Count() == ids.Count && ids.All(evidenceIds.Contains)
            && (hypothesis || ids.Count > 0);
        if (output.Assumptions.Concat(output.Risks).Any(claim => claim == null || !Text(claim.Text)
            || !References(claim.EvidenceIds, claim.IsHypothesis))) return "Use only evidence[].id values in evidenceIds; never cite assumptions[].id or constraints[].id. Unsupported assumptions must use evidenceIds=[] and isHypothesis=true.";
        if (output.Alternatives.Any(item => item == null || !Text(item.Name, 500) || !Text(item.Tradeoff)
            || !References(item.EvidenceIds, item.IsHypothesis))) return "Alternative citations or content are invalid.";
        if (compact && (output.Assumptions.Count > 3 || output.Risks.Count > 3 || output.MissingInformation.Count > 4 || output.Alternatives.Count > 2
            || output.Analysis.Length > 1200 || output.Assumptions.Concat(output.Risks).Any(claim => claim.Text.Length > 500)
            || output.MissingInformation.Any(text => text.Length > 500) || output.Alternatives.Any(item => item.Name.Length > 200 || item.Tradeoff.Length > 500)))
            return "Compact analysis exceeds the v3 output contract limits.";
        return null;
    }

    public static string? Reflection(OutcomeReflection output) =>
        Text(output.DecisionProcessAssessment, 8000) && Text(output.OutcomeAssessment, 8000)
        && Text(output.LuckAndUncertainty, 8000) && Strings(output.Lessons) && output.Lessons.Count > 0
            ? null : "Outcome reflection is incomplete.";
}
