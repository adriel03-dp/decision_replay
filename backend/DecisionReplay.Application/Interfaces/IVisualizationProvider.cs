using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

/// <summary>
/// Interface: Visualization Data Provider
/// 
/// SOLID Principles:
/// - Interface Segregation: Focused on visualization data generation
/// - Single Responsibility: Only converts decision data to chart-ready format
/// - Dependency Inversion: UI depends on this abstraction, not concrete implementation
/// 
/// Clean Architecture:
/// - Application layer interface
/// - Produces chart-agnostic data structures
/// - UI layer consumes this data with any charting library
/// 
/// Purpose:
/// Generates data structures suitable for visualizations:
/// - Timeline charts
/// - Capacity vs demand
/// - Risk heatmaps
/// - Feasibility trends
/// 
/// Output is JSON-serializable and framework-agnostic.
/// </summary>
public interface IVisualizationProvider
{
    /// <summary>
    /// Generates timeline data for decision evolution
    /// </summary>
    Task<TimelineData> GenerateTimelineAsync(
        Guid decisionId,
        List<DecisionAnalysis> analyses);

    /// <summary>
    /// Generates risk heatmap data
    /// </summary>
    RiskHeatmapData GenerateRiskHeatmap(DecisionAnalysis analysis);

    /// <summary>
    /// Generates feasibility trend data (useful for replay comparison)
    /// </summary>
    FeasibilityTrendData GenerateFeasibilityTrend(
        List<DecisionAnalysis> analyses,
        List<DateTime> timestamps);

    /// <summary>
    /// Generates recommendation priority chart data
    /// </summary>
    RecommendationPriorityData GenerateRecommendationPriority(DecisionAnalysis analysis);
}

// Chart-agnostic data structures

public class TimelineData
{
    public string Title { get; set; } = "";
    public List<TimelineEvent> Events { get; set; } = new();

    public class TimelineEvent
    {
        public DateTime Timestamp { get; set; }
        public string Label { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}

public class RiskHeatmapData
{
    public List<RiskCell> Risks { get; set; } = new();

    public class RiskCell
    {
        public string Description { get; set; } = "";
        public string Impact { get; set; } = ""; // HIGH, MEDIUM, LOW
        public double Likelihood { get; set; } // 0.0-1.0 (inferred or default)
        public string? Mitigation { get; set; }
        public string Color { get; set; } = ""; // Hex color for visualization
    }
}

public class FeasibilityTrendData
{
    public List<FeasibilityPoint> DataPoints { get; set; } = new();

    public class FeasibilityPoint
    {
        public DateTime Timestamp { get; set; }
        public double FeasibilityScore { get; set; }
        public string Verdict { get; set; } = "";
        public double Confidence { get; set; }
    }
}

public class RecommendationPriorityData
{
    public List<RecommendationItem> Recommendations { get; set; } = new();

    public class RecommendationItem
    {
        public string Description { get; set; } = "";
        public string Priority { get; set; } = ""; // HIGH, MEDIUM, LOW
        public string Category { get; set; } = ""; // Timeline, Resources, Scope, etc.
    }
}
