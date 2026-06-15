using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

public interface IAiLanguageService
{
    bool IsConfigured { get; }

    Task<StructuredDecisionData> ExtractDecisionAsync(
        string naturalLanguageInput,
        IReadOnlyCollection<DecisionDomainTemplate> templates,
        CancellationToken cancellationToken = default);

    Task<string> ExplainResultAsync(
        StructuredDecisionData decision,
        FeasibilityAssessment assessment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(
        StructuredDecisionData decision,
        FeasibilityAssessment assessment,
        ActionPlan plan,
        CancellationToken cancellationToken = default);

    Task<string> SummarizeReplayAsync(
        ReplayComparison comparison,
        CancellationToken cancellationToken = default);
}
