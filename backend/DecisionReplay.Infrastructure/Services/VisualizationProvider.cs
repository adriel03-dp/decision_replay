using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Visualization Data Provider Implementation
/// 
/// SOLID Principles:
/// - Single Responsibility: Transforms decision data into chart-ready formats
/// - Open/Closed: Extensible with new visualization types
/// - Dependency Inversion: Implements IVisualizationProvider
/// 
/// Clean Architecture:
/// - Infrastructure layer implementation
/// - No UI dependencies - produces pure data structures
/// - Framework-agnostic output (works with any charting library)
/// 
/// Purpose:
/// Converts domain objects into visualization-friendly data structures.
/// The UI can consume these with Chart.js, D3.js, Recharts, or any other library.
/// </summary>
public class VisualizationProvider : IVisualizationProvider
{
    public Task<TimelineData> GenerateTimelineAsync(
        Guid decisionId,
        List<DecisionAnalysis> analyses)
    {
        var timeline = new TimelineData
        {
            Title = $"Decision {decisionId} Evolution"
        };

        foreach (var analysis in analyses.OrderBy(a => a.GeneratedAt))
        {
            timeline.Events.Add(new TimelineData.TimelineEvent
            {
                Timestamp = analysis.GeneratedAt,
                Label = $"Analysis: {analysis.FeasibilityVerdict}",
                Description = analysis.ExecutiveSummary,
                Category = "Analysis",
                Metadata = new Dictionary<string, object>
                {
                    ["feasibilityScore"] = analysis.FeasibilityScore,
                    ["confidence"] = analysis.ConfidenceLevel,
                    ["riskCount"] = analysis.Risks.Count
                }
            });
        }

        return Task.FromResult(timeline);
    }

    public RiskHeatmapData GenerateRiskHeatmap(DecisionAnalysis analysis)
    {
        var heatmap = new RiskHeatmapData();

        foreach (var risk in analysis.Risks)
        {
            // Infer likelihood based on impact (simple heuristic)
            var likelihood = risk.Impact.ToUpper() switch
            {
                "HIGH" => 0.7,
                "MEDIUM" => 0.5,
                "LOW" => 0.3,
                _ => 0.5
            };

            var color = GetRiskColor(risk.Impact, likelihood);

            heatmap.Risks.Add(new RiskHeatmapData.RiskCell
            {
                Description = risk.Description,
                Impact = risk.Impact,
                Likelihood = likelihood,
                Mitigation = risk.Mitigation,
                Color = color
            });
        }

        return heatmap;
    }

    public FeasibilityTrendData GenerateFeasibilityTrend(
        List<DecisionAnalysis> analyses,
        List<DateTime> timestamps)
    {
        var trend = new FeasibilityTrendData();

        var analysesList = analyses.OrderBy(a => a.GeneratedAt).ToList();

        for (int i = 0; i < analysesList.Count; i++)
        {
            var analysis = analysesList[i];
            trend.DataPoints.Add(new FeasibilityTrendData.FeasibilityPoint
            {
                Timestamp = i < timestamps.Count ? timestamps[i] : analysis.GeneratedAt,
                FeasibilityScore = analysis.FeasibilityScore,
                Verdict = analysis.FeasibilityVerdict,
                Confidence = analysis.ConfidenceLevel
            });
        }

        return trend;
    }

    public RecommendationPriorityData GenerateRecommendationPriority(DecisionAnalysis analysis)
    {
        var data = new RecommendationPriorityData();

        // Infer priority and category from recommendation text (simple heuristics)
        foreach (var recommendation in analysis.Recommendations)
        {
            var priority = InferPriority(recommendation);
            var category = InferCategory(recommendation);

            data.Recommendations.Add(new RecommendationPriorityData.RecommendationItem
            {
                Description = recommendation,
                Priority = priority,
                Category = category
            });
        }

        // Sort by priority
        data.Recommendations = data.Recommendations
            .OrderByDescending(r => r.Priority == "HIGH" ? 3 : r.Priority == "MEDIUM" ? 2 : 1)
            .ToList();

        return data;
    }

    private string GetRiskColor(string impact, double likelihood)
    {
        // Risk matrix colors (standard traffic light system)
        var impactLevel = impact.ToUpper() switch
        {
            "HIGH" => 3,
            "MEDIUM" => 2,
            "LOW" => 1,
            _ => 2
        };

        var likelihoodLevel = likelihood >= 0.7 ? 3 : likelihood >= 0.4 ? 2 : 1;
        var severity = impactLevel * likelihoodLevel;

        return severity switch
        {
            >= 6 => "#FF4444", // High risk - red
            >= 4 => "#FFAA00", // Medium risk - orange
            _ => "#FFDD44"     // Low risk - yellow
        };
    }

    private string InferPriority(string recommendation)
    {
        var highPriorityKeywords = new[] { "critical", "must", "immediately", "urgent", "essential" };
        var lowPriorityKeywords = new[] { "consider", "optionally", "if possible", "nice to have" };

        var lowerRec = recommendation.ToLower();

        if (highPriorityKeywords.Any(k => lowerRec.Contains(k)))
            return "HIGH";

        if (lowPriorityKeywords.Any(k => lowerRec.Contains(k)))
            return "LOW";

        return "MEDIUM";
    }

    private string InferCategory(string recommendation)
    {
        var lowerRec = recommendation.ToLower();

        if (lowerRec.Contains("timeline") || lowerRec.Contains("deadline") || lowerRec.Contains("schedule"))
            return "Timeline";

        if (lowerRec.Contains("team") || lowerRec.Contains("developer") || lowerRec.Contains("hire") || lowerRec.Contains("resource"))
            return "Resources";

        if (lowerRec.Contains("scope") || lowerRec.Contains("feature") || lowerRec.Contains("requirement"))
            return "Scope";

        if (lowerRec.Contains("budget") || lowerRec.Contains("cost") || lowerRec.Contains("funding"))
            return "Budget";

        if (lowerRec.Contains("technical") || lowerRec.Contains("architecture") || lowerRec.Contains("technology"))
            return "Technical";

        return "General";
    }
}
