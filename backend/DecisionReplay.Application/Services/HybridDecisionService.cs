using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DecisionReplay.Application.Services;

/// <summary>
/// Orchestrates deterministic scoring and optional qualitative AI analysis.
/// Numerical feasibility remains owned by the deterministic engine.
/// </summary>
public class HybridDecisionService
{
    private readonly IFeasibilityEngine _feasibility;
    private readonly ITimelineGenerator _timeline;
    private readonly ISimulationEngine _simulation;
    private readonly IAiDecisionAnalyzer _aiAnalyzer;
    private readonly ILogger<HybridDecisionService> _logger;

    public HybridDecisionService(
        IFeasibilityEngine feasibility,
        ITimelineGenerator timeline,
        ISimulationEngine simulation,
        IAiDecisionAnalyzer aiAnalyzer,
        ILogger<HybridDecisionService> logger)
    {
        _feasibility = feasibility;
        _timeline = timeline;
        _simulation = simulation;
        _aiAnalyzer = aiAnalyzer;
        _logger = logger;
    }

    public async Task<HybridAnalysisResult> EvaluateAsync(
        ProjectInput input,
        bool requestAiEnhancement,
        CancellationToken cancellationToken = default)
    {
        var feasibility = _feasibility.Evaluate(input);
        var plan = _timeline.Generate(input, feasibility);

        DecisionAnalysis? aiEnhancement = null;
        if (requestAiEnhancement && _aiAnalyzer.IsConfigured)
        {
            aiEnhancement = await _aiAnalyzer.AnalyzeAsync(
                input,
                feasibility,
                plan,
                cancellationToken);
        }

        _logger.LogInformation(
            "[HYBRID] Evaluated {Type} - score={Score}, AI={AiAvailable}",
            input.ProjectType,
            feasibility.Score,
            aiEnhancement != null);

        return new HybridAnalysisResult
        {
            Input = input,
            Feasibility = feasibility,
            Plan = plan,
            AiEnhancement = aiEnhancement,
            AiAvailable = aiEnhancement != null,
            GeneratedAt = DateTime.UtcNow,
        };
    }

    public SimulationResult Simulate(
        ProjectInput baseInput,
        SimulationAdjustments adjustments) =>
        _simulation.Simulate(baseInput, adjustments);
}

public class HybridAnalysisResult
{
    public ProjectInput Input { get; init; } = null!;
    public FeasibilityResult Feasibility { get; init; } = null!;
    public ProjectPlan Plan { get; init; } = null!;
    public DecisionAnalysis? AiEnhancement { get; init; }
    public bool AiAvailable { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
