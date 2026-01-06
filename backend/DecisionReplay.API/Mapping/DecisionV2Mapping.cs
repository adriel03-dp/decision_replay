using DecisionReplay.API.DTOs;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.API.Mapping;

/// <summary>
/// Mapping extensions for V2 entities
/// SOLID: Single Responsibility - Only handles DTO mapping
/// </summary>
public static class DecisionV2Mapping
{
    public static DecisionV2Response ToResponse(this DecisionV2 decision)
    {
        return new DecisionV2Response(
            decision.Id,
            decision.Context?.NaturalLanguageInput ?? string.Empty,
            decision.Context?.InferredAttributes ?? new Dictionary<string, object>(),
            decision.Schema?.DomainType,
            decision.Status.ToString(),
            decision.Outcome.ToString(),
            decision.CreatedAt,
            decision.LastModifiedAt,
            decision.CreatedBy
        );
    }

    public static AnalysisResponse ToResponse(this DecisionAnalysis analysis)
    {
        return new AnalysisResponse(
            analysis.AnalysisId,
            analysis.FeasibilityScore,
            analysis.FeasibilityVerdict,
            analysis.ExecutiveSummary,
            analysis.CurrentPlanAnalysis != null ? new CurrentPlanAnalysisResponse(
                analysis.CurrentPlanAnalysis.TimelineAssessment,
                analysis.CurrentPlanAnalysis.ScopeAssessment,
                analysis.CurrentPlanAnalysis.BudgetAssessment,
                analysis.CurrentPlanAnalysis.ResourceAssessment
            ) : null,
            analysis.Pros,
            analysis.Cons,
            analysis.OptimizedSolution != null ? new OptimizedSolutionResponse(
                analysis.OptimizedSolution.ImprovedTimeline,
                analysis.OptimizedSolution.ClarifiedScope,
                analysis.OptimizedSolution.BudgetOptimization,
                analysis.OptimizedSolution.ResourceStrategy,
                analysis.OptimizedSolution.SuccessProbability
            ) : null,
            analysis.OptimizedPros,
            analysis.OptimizedCons,
            analysis.Risks.Select(r => new RiskResponse(r.Description, r.Impact, r.Mitigation)).ToList(),
            analysis.Assumptions,
            analysis.Recommendations,
            analysis.ConfidenceLevel,
            analysis.GeneratedAt,
            analysis.ModelUsed,
            analysis.GenerateChartData()
        );
    }

    public static SchemaResponse ToResponse(this DecisionSchema schema)
    {
        return new SchemaResponse(
            schema.Id,
            schema.DomainType,
            schema.Fields,
            schema.GeneratedAt
        );
    }

    public static ReplayResponse ToResponse(this DecisionReplayResult replay)
    {
        return new ReplayResponse(
            replay.ReplayId,
            replay.ReplayedAt,
            replay.Changes.Select(c => new ChangeResponse(
                c.Field,
                c.OldValue?.ToString(),
                c.NewValue?.ToString(),
                c.IsSignificant,
                c.ChangeType
            )).ToList(),
            replay.HasSignificantChanges,
            replay.OriginalAnalysis?.ToResponse(),
            replay.UpdatedAnalysis?.ToResponse(),
            replay.FeasibilityDelta,
            replay.ImpactSummary,
            replay.VisualizationData
        );
    }

    /// <summary>
    /// Generate chart data from DecisionAnalysis
    /// </summary>
    public static ChartDataResponse? GenerateChartData(this DecisionAnalysis analysis)
    {
        // Generate timeline data showing feasibility progression
        var timelineData = new List<FeasibilityTimelineData>
        {
            new("Initial", analysis.FeasibilityScore, CalculateTimelinePressure(analysis), CalculateResourceAdequacy(analysis), CalculateScopeComplexity(analysis)),
            new("Mid-Point", Math.Min(100, analysis.FeasibilityScore + 15), Math.Max(0, CalculateTimelinePressure(analysis) - 10), CalculateResourceAdequacy(analysis) + 5, CalculateScopeComplexity(analysis)),
            new("Final", analysis.OptimizedSolution?.SuccessProbability ?? analysis.FeasibilityScore, Math.Max(0, CalculateTimelinePressure(analysis) - 20), CalculateResourceAdequacy(analysis) + 10, Math.Max(0, CalculateScopeComplexity(analysis) - 15))
        };

        // Generate performance/resource allocation data
        var performanceData = GenerateResourceAllocationData(analysis);

        // Generate risk factor heatmap data
        var riskHeatmapData = analysis.Risks.Select(risk => new PlanningFactorData(
            risk.Description,
            risk.Impact == "HIGH" ? 85 : risk.Impact == "MEDIUM" ? 60 : 35,
            risk.Impact == "HIGH" ? "critical" : risk.Impact == "MEDIUM" ? "warning" : "good",
            "stable",
            risk.Mitigation
        )).ToList();

        // Add general planning factors
        if (analysis.CurrentPlanAnalysis != null)
        {
            riskHeatmapData.AddRange(new[]
            {
                new PlanningFactorData("Timeline Feasibility", CalculateTimelinePressure(analysis), GetStatusFromScore(100 - CalculateTimelinePressure(analysis)), "stable", analysis.CurrentPlanAnalysis.TimelineAssessment),
                new PlanningFactorData("Resource Availability", CalculateResourceAdequacy(analysis), GetStatusFromScore(CalculateResourceAdequacy(analysis)), "up", analysis.CurrentPlanAnalysis.ResourceAssessment),
                new PlanningFactorData("Scope Complexity", CalculateScopeComplexity(analysis), GetStatusFromScore(100 - CalculateScopeComplexity(analysis)), "down", analysis.CurrentPlanAnalysis.ScopeAssessment)
            });
        }

        return new ChartDataResponse(timelineData, performanceData, riskHeatmapData);
    }

    private static List<ResourceAllocationData> GenerateResourceAllocationData(DecisionAnalysis analysis)
    {
        var resourceData = new List<ResourceAllocationData>();

        // Extract resource information from analysis text
        if (analysis.CurrentPlanAnalysis != null)
        {
            // Timeline resource
            var timelineAllocated = ExtractNumericFromText(analysis.CurrentPlanAnalysis.TimelineAssessment, 100);
            var timelineRequired = analysis.OptimizedSolution != null ? 
                ExtractNumericFromText(analysis.OptimizedSolution.ImprovedTimeline, timelineAllocated + 20) : 
                timelineAllocated + 10;
            resourceData.Add(new ResourceAllocationData("Timeline (weeks)", timelineAllocated, timelineRequired, timelineRequired - timelineAllocated));

            // Budget resource  
            var budgetAllocated = ExtractNumericFromText(analysis.CurrentPlanAnalysis.BudgetAssessment, 80);
            var budgetRequired = analysis.OptimizedSolution != null ?
                ExtractNumericFromText(analysis.OptimizedSolution.BudgetOptimization, budgetAllocated + 15) :
                budgetAllocated + 10;
            resourceData.Add(new ResourceAllocationData("Budget (k$)", budgetAllocated, budgetRequired, budgetRequired - budgetAllocated));
        }

        // Add default resources if none extracted
        if (resourceData.Count == 0)
        {
            var baseScore = analysis.FeasibilityScore;
            resourceData.AddRange(new[]
            {
                new ResourceAllocationData("Team Size", Math.Round(baseScore * 0.8), Math.Round(baseScore * 0.9), Math.Round(baseScore * 0.1)),
                new ResourceAllocationData("Budget (k$)", Math.Round(baseScore * 1.2), Math.Round(baseScore * 1.35), Math.Round(baseScore * 0.15)),
                new ResourceAllocationData("Timeline (weeks)", Math.Round(baseScore * 0.6), Math.Round(baseScore * 0.7), Math.Round(baseScore * 0.1))
            });
        }

        return resourceData;
    }

    private static double CalculateTimelinePressure(DecisionAnalysis analysis)
    {
        // Higher pressure = more challenging timeline
        var basePressure = 100 - analysis.FeasibilityScore;
        if (analysis.CurrentPlanAnalysis?.TimelineAssessment?.ToLower().Contains("aggressive") == true ||
            analysis.CurrentPlanAnalysis?.TimelineAssessment?.ToLower().Contains("tight") == true)
            basePressure += 20;
        return Math.Min(100, Math.Max(0, basePressure));
    }

    private static double CalculateResourceAdequacy(DecisionAnalysis analysis)
    {
        // Higher adequacy = better resource availability
        var adequacy = analysis.FeasibilityScore;
        if (analysis.CurrentPlanAnalysis?.ResourceAssessment?.ToLower().Contains("sufficient") == true)
            adequacy += 10;
        else if (analysis.CurrentPlanAnalysis?.ResourceAssessment?.ToLower().Contains("lacking") == true)
            adequacy -= 20;
        return Math.Min(100, Math.Max(0, adequacy));
    }

    private static double CalculateScopeComplexity(DecisionAnalysis analysis)
    {
        // Higher complexity = more challenging scope
        var complexity = 100 - analysis.FeasibilityScore;
        if (analysis.CurrentPlanAnalysis?.ScopeAssessment?.ToLower().Contains("complex") == true ||
            analysis.CurrentPlanAnalysis?.ScopeAssessment?.ToLower().Contains("ambitious") == true)
            complexity += 25;
        return Math.Min(100, Math.Max(0, complexity));
    }

    private static string GetStatusFromScore(double score)
    {
        return score >= 70 ? "good" : score >= 40 ? "warning" : "critical";
    }

    private static double ExtractNumericFromText(string text, double fallback)
    {
        if (string.IsNullOrEmpty(text)) return fallback;
        
        var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d+")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => double.Parse(m.Value))
            .ToList();
        
        return numbers.Any() ? numbers.First() : fallback;
    }
}
