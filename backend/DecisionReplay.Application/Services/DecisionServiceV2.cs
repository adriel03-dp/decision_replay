using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

/// <summary>
/// REFACTORED Decision Service - Domain-Agnostic
/// 
/// SOLID Principles:
/// - Single Responsibility: Orchestrates decision creation and analysis workflow
/// - Dependency Inversion: Depends on interfaces, not concrete implementations
/// - Open/Closed: Extensible without modification
/// 
/// Clean Architecture:
/// - Application layer service
/// - Orchestrates domain entities and infrastructure services
/// - No framework dependencies
/// 
/// Design:
/// - Coordinates intent parsing, schema generation, and AI analysis
/// - Works with any decision domain without code changes
/// - Supports decision replay functionality
/// </summary>
public class DecisionServiceV2
{
    private readonly IIntentParser _intentParser;
    private readonly IAIReasoningServiceV2 _reasoningService;
    private readonly IReplayEngine _replayEngine;
    private readonly IVisualizationProvider _visualizationProvider;

    public DecisionServiceV2(
        IIntentParser intentParser,
        IAIReasoningServiceV2 reasoningService,
        IReplayEngine replayEngine,
        IVisualizationProvider visualizationProvider)
    {
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
        _reasoningService = reasoningService ?? throw new ArgumentNullException(nameof(reasoningService));
        _replayEngine = replayEngine ?? throw new ArgumentNullException(nameof(replayEngine));
        _visualizationProvider = visualizationProvider ?? throw new ArgumentNullException(nameof(visualizationProvider));
    }

    /// <summary>
    /// Creates a decision from natural language input with immediate analysis
    /// Pipeline: Parse Intent → Generate Schema → Create Decision → Auto-Analyze
    /// </summary>
    public async Task<DecisionV2> CreateDecisionAsync(string naturalLanguageInput, string userId)
    {
        // Step 1: Parse intent and extract context
        var context = await _intentParser.ParseInputAsync(naturalLanguageInput, userId);

        // Step 2: Generate schema based on inferred domain
        var schema = await _intentParser.GenerateSchemaAsync(context);

        // Step 3: Create decision entity
        var decision = new DecisionV2(context, userId);
        decision.AssignSchema(schema);

        // Step 4: Automatically analyze the decision immediately
        var analysis = await _reasoningService.AnalyzeDecisionAsync(
            decision.Context,
            decision.Schema
        );

        decision.StoreAnalysis(analysis);

        return decision;
    }

    /// <summary>
    /// Analyzes a decision using AI reasoning
    /// </summary>
    public async Task<DecisionAnalysis> AnalyzeDecisionAsync(DecisionV2 decision)
    {
        var analysis = await _reasoningService.AnalyzeDecisionAsync(
            decision.Context,
            decision.Schema
        );

        decision.StoreAnalysis(analysis);

        return analysis;
    }

    /// <summary>
    /// Replays a decision with updated input
    /// Pipeline: Parse New Context → Detect Changes → Re-Analyze → Compare
    /// </summary>
    public async Task<DecisionReplayResult> ReplayDecisionAsync(
        DecisionV2 decision,
        string updatedInput,
        string userId)
    {
        // Parse updated input
        var updatedContext = await _intentParser.ParseInputAsync(updatedInput, userId);

        // Get original analysis
        if (decision.Analysis == null)
            throw new InvalidOperationException("Cannot replay decision without original analysis");

        // Perform replay
        var replayResult = await _replayEngine.ReplayDecisionAsync(
            decision.Id,
            decision.Context,
            decision.Analysis,
            updatedContext
        );

        // Update decision with new context and analysis
        decision.UpdateContext(updatedContext);
        if (replayResult.UpdatedAnalysis != null)
        {
            decision.StoreAnalysis(replayResult.UpdatedAnalysis);
        }

        return replayResult;
    }

    /// <summary>
    /// Generates visualization data for a decision
    /// </summary>
    public Task<Dictionary<string, object>> GenerateVisualizationsAsync(DecisionV2 decision)
    {
        var visualizations = new Dictionary<string, object>();

        if (decision.Analysis != null)
        {
            // Risk heatmap
            var riskHeatmap = _visualizationProvider.GenerateRiskHeatmap(decision.Analysis);
            visualizations["riskHeatmap"] = riskHeatmap;

            // Recommendations
            var recommendations = _visualizationProvider.GenerateRecommendationPriority(decision.Analysis);
            visualizations["recommendations"] = recommendations;
        }

        return Task.FromResult(visualizations);
    }

    /// <summary>
    /// Queries AI about a specific decision
    /// </summary>
    public async Task<string> QueryDecisionAsync(DecisionV2 decision, string question)
    {
        return await _reasoningService.QueryDecisionAsync(decision.Context, question);
    }
}
