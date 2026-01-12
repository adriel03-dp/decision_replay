using DecisionReplay.Domain.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionReplay.Application.Interfaces;

/// <summary>
/// REFACTORED AI Reasoning Service Interface
/// 
/// SOLID Principles:
/// - Interface Segregation: Focused contract for reasoning operations
/// - Dependency Inversion: High-level modules depend on this abstraction
/// - Open/Closed: New reasoning strategies can be added without changing interface
/// 
/// Clean Architecture:
/// - Application layer interface
/// - Infrastructure provides implementation (Gemini, OpenAI, etc.)
/// - Domain-agnostic: works with any decision type via DecisionContext
/// </summary>
public interface IAIReasoningServiceV2
{
    /// <summary>
    /// Generates comprehensive analysis for a decision based on its context
    /// Returns structured DecisionAnalysis with feasibility, risks, pros/cons, etc.
    /// </summary>
    Task<DecisionAnalysis> AnalyzeDecisionAsync(DecisionContext context, DecisionSchema? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-analyzes a decision with updated context (for replay capability)
    /// </summary>
    Task<DecisionAnalysis> ReAnalyzeDecisionAsync(
        DecisionContext originalContext,
        DecisionContext updatedContext,
        DecisionSchema? schema = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Answers specific questions about a decision
    /// </summary>
    Task<string> QueryDecisionAsync(DecisionContext context, string question, CancellationToken cancellationToken = default);
}
