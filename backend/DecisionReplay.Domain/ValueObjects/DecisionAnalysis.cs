namespace DecisionReplay.Domain.ValueObjects;

/// <summary>
/// Value Object: Structured analysis result from AI reasoning
/// SOLID: Single Responsibility - Encapsulates AI analysis results
/// Design: Provides structured data for both textual display and visualization
/// </summary>
public class DecisionAnalysis
{
    public Guid AnalysisId { get; private set; }
    public double FeasibilityScore { get; private set; } // 0-100
    public string FeasibilityVerdict { get; private set; } // FEASIBLE, RISKY_BUT_POSSIBLE, NEEDS_ADJUSTMENT, NOT_FEASIBLE
    public string ExecutiveSummary { get; private set; }
    public List<string> Pros { get; private set; }
    public List<string> Cons { get; private set; }
    public List<RiskFactor> Risks { get; private set; }
    public List<string> Assumptions { get; private set; }
    public List<string> Recommendations { get; private set; }
    public double ConfidenceLevel { get; private set; } // 0.0 - 1.0
    public DateTime GeneratedAt { get; private set; }
    public string ModelUsed { get; private set; }

    private DecisionAnalysis()
    {
        Pros = new List<string>();
        Cons = new List<string>();
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
        Risks = risks ?? new List<RiskFactor>();
        Assumptions = assumptions ?? new List<string>();
        Recommendations = recommendations ?? new List<string>();
        ConfidenceLevel = confidenceLevel;
        GeneratedAt = DateTime.UtcNow;
        ModelUsed = modelUsed;
    }
}

/// <summary>
/// Value Object: Represents a risk factor identified during analysis
/// </summary>
public class RiskFactor
{
    public string Description { get; private set; }
    public string Impact { get; private set; } // HIGH, MEDIUM, LOW
    public string? Mitigation { get; private set; }

    private RiskFactor()
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
