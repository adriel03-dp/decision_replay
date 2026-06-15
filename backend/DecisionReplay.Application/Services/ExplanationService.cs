using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class ExplanationService
{
    private readonly IAiLanguageService _languageService;

    public ExplanationService(IAiLanguageService languageService)
    {
        _languageService = languageService;
    }

    public async Task<string> ExplainAsync(
        StructuredDecisionData decision,
        FeasibilityAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        var explanation = await _languageService.ExplainResultAsync(
            decision,
            assessment,
            cancellationToken);
        return string.IsNullOrWhiteSpace(explanation)
            ? BuildDeterministicExplanation(assessment)
            : explanation.Trim();
    }

    private static string BuildDeterministicExplanation(FeasibilityAssessment assessment)
    {
        var weakest = assessment.FactorBreakdown.OrderBy(factor => factor.Score).Take(2).ToList();
        return $"The backend calculated a feasibility score of {assessment.FeasibilityScore:0.#}/100 " +
               $"with {assessment.RiskLevel} risk. The most constrained factors are " +
               $"{string.Join(" and ", weakest.Select(factor => factor.Factor.Replace('_', ' ')))}. " +
               "The score is derived from domain rules and the supplied structured values.";
    }
}
