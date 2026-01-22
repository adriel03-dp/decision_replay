using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DecisionReplay.Domain.ValueObjects;

/// <summary>
/// Value Object: Structured analysis result from AI reasoning
/// SOLID: Single Responsibility - Encapsulates AI analysis results
/// Design: Provides structured data for both textual display and visualization
/// </summary>
[BsonIgnoreExtraElements]
public class DecisionAnalysis
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid AnalysisId { get; set; }
    public double FeasibilityScore { get; set; } // 0-100
    public string FeasibilityVerdict { get; set; } // FEASIBLE, RISKY_BUT_POSSIBLE, NEEDS_ADJUSTMENT, NOT_FEASIBLE
    public string ExecutiveSummary { get; set; }
    public CurrentPlanAnalysis? CurrentPlanAnalysis { get; set; }
    public List<string> Pros { get; set; }
    public List<string> Cons { get; set; }
    public OptimizedSolution? OptimizedSolution { get; set; }
    public List<string> OptimizedPros { get; set; }
    public List<string> OptimizedCons { get; set; }
    public List<RiskFactor> Risks { get; set; }
    public List<string> Assumptions { get; set; }
    public List<string> Recommendations { get; set; }
    public double ConfidenceLevel { get; set; } // 0.0 - 1.0
    public DateTime GeneratedAt { get; set; }
    public string ModelUsed { get; set; }
    public ComparisonLayer? Comparison { get; set; }

    public DecisionAnalysis()
    {
        Pros = new List<string>();
        Cons = new List<string>();
        OptimizedPros = new List<string>();
        OptimizedCons = new List<string>();
        Risks = new List<RiskFactor>();
        Assumptions = new List<string>();
        Recommendations = new List<string>();
        FeasibilityVerdict = string.Empty;
        ExecutiveSummary = string.Empty;
        ModelUsed = string.Empty;
    }

    public DecisionAnalysis(
        double feasibilityScore,
        string feasibilityVerdict,
        string executiveSummary,
        List<string> pros,
        List<string> cons,
        List<RiskFactor> risks,
        List<string> assumptions,
        List<string> recommendations,
        double confidenceLevel,
        string modelUsed)
    {
        if (feasibilityScore < 0 || feasibilityScore > 100)
            throw new ArgumentException("Feasibility score must be between 0 and 100");
        if (confidenceLevel < 0 || confidenceLevel > 1)
            throw new ArgumentException("Confidence level must be between 0 and 1");

        AnalysisId = Guid.NewGuid();
        FeasibilityScore = feasibilityScore;
        FeasibilityVerdict = feasibilityVerdict;
        ExecutiveSummary = executiveSummary;
        Pros = pros ?? new List<string>();
        Cons = cons ?? new List<string>();
        OptimizedPros = new List<string>();
        OptimizedCons = new List<string>();
        Risks = risks ?? new List<RiskFactor>();
        Assumptions = assumptions ?? new List<string>();
        Recommendations = recommendations ?? new List<string>();
        ConfidenceLevel = confidenceLevel;
        GeneratedAt = DateTime.UtcNow;
        ModelUsed = modelUsed;
    }
}

/// <summary>
/// Value Object: Current plan assessment details
/// </summary>
public class CurrentPlanAnalysis
{
    public string TimelineAssessment { get; set; }
    public string ScopeAssessment { get; set; }
    public string BudgetAssessment { get; set; }
    public string ResourceAssessment { get; set; }

    public CurrentPlanAnalysis()
    {
        TimelineAssessment = string.Empty;
        ScopeAssessment = string.Empty;
        BudgetAssessment = string.Empty;
        ResourceAssessment = string.Empty;
    }

    public CurrentPlanAnalysis(string timelineAssessment, string scopeAssessment, string budgetAssessment, string resourceAssessment)
    {
        TimelineAssessment = timelineAssessment;
        ScopeAssessment = scopeAssessment;
        BudgetAssessment = budgetAssessment;
        ResourceAssessment = resourceAssessment;
    }
}

/// <summary>
/// Value Object: Optimized solution recommendations
/// </summary>
public class OptimizedSolution
{
    public string ImprovedTimeline { get; set; }
    public string ClarifiedScope { get; set; }
    public string BudgetOptimization { get; set; }
    public string ResourceStrategy { get; set; }
    public double SuccessProbability { get; set; }

    public OptimizedSolution()
    {
        ImprovedTimeline = string.Empty;
        ClarifiedScope = string.Empty;
        BudgetOptimization = string.Empty;
        ResourceStrategy = string.Empty;
    }

    public OptimizedSolution(string improvedTimeline, string clarifiedScope, string budgetOptimization, string resourceStrategy, double successProbability)
    {
        ImprovedTimeline = improvedTimeline;
        ClarifiedScope = clarifiedScope;
        BudgetOptimization = budgetOptimization;
        ResourceStrategy = resourceStrategy;
        SuccessProbability = successProbability;
    }
}

/// <summary>
/// Value Object: Represents a risk factor identified during analysis
/// </summary>
public class RiskFactor
{
    public string Description { get; set; }
    public string Impact { get; set; } // HIGH, MEDIUM, LOW
    public string? Mitigation { get; set; }

    public RiskFactor()
    {
        Description = string.Empty;
        Impact = string.Empty;
    }

    public RiskFactor(string description, string impact, string? mitigation = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty");
        if (string.IsNullOrWhiteSpace(impact))
            throw new ArgumentException("Impact cannot be empty");

        Description = description;
        Impact = impact.ToUpper();
        Mitigation = mitigation;
    }
}

/// <summary>
/// Value Object: Comparison between user plan and optimized plan
/// </summary>
public class ComparisonLayer
{
    public List<string> MainDifferences { get; set; } = new();
    public List<string> Tradeoffs { get; set; } = new();
    public string WhyOptimizedIsBetter { get; set; } = string.Empty;

    public ComparisonLayer() { }

    public ComparisonLayer(List<string> mainDifferences, List<string> tradeoffs, string whyOptimizedIsBetter)
    {
        MainDifferences = mainDifferences ?? new List<string>();
        Tradeoffs = tradeoffs ?? new List<string>();
        WhyOptimizedIsBetter = whyOptimizedIsBetter ?? string.Empty;
    }
}
