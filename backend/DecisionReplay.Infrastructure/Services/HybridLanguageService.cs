using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>Groq owns intake extraction; Ollama owns optional result wording.</summary>
public sealed class HybridLanguageService(GroqLanguageService groq, OllamaLanguageService ollama) : IAiLanguageService
{
    public bool IsConfigured => groq.IsConfigured;

    public Task<StructuredDecisionData> ExtractDecisionAsync(string input,
        IReadOnlyCollection<DecisionDomainTemplate> templates, CancellationToken cancellationToken = default) =>
        groq.ExtractDecisionAsync(input, templates, cancellationToken);

    public Task<string> ExplainResultAsync(StructuredDecisionData decision,
        FeasibilityAssessment assessment, CancellationToken cancellationToken = default) =>
        ollama.ExplainResultAsync(decision, assessment, cancellationToken);

    public Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(StructuredDecisionData decision,
        FeasibilityAssessment assessment, ActionPlan plan, CancellationToken cancellationToken = default) =>
        ollama.EnhancePlanTasksAsync(decision, assessment, plan, cancellationToken);

    public Task<string> SummarizeReplayAsync(ReplayComparison comparison,
        CancellationToken cancellationToken = default) => ollama.SummarizeReplayAsync(comparison, cancellationToken);
}
