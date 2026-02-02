using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Validates AI-generated responses for logical consistency and completeness
/// Detects hallucinations, contradictions, and missing critical information
/// </summary>
public class GeminiResponseValidator
{
    private readonly ILogger<GeminiResponseValidator> _logger;

    public GeminiResponseValidator(ILogger<GeminiResponseValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates a DecisionAnalysis for logical consistency
    /// </summary>
    public ValidationResult ValidateAnalysis(DecisionAnalysis analysis, string originalContext)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // 1. Feasibility Score Validation
        if (analysis.FeasibilityScore < 0 || analysis.FeasibilityScore > 100)
        {
            errors.Add($"Feasibility score {analysis.FeasibilityScore} out of valid range (0-100)");
        }

        // 2. Verdict-Score Consistency
        var scoreVerdictMismatch = CheckScoreVerdictConsistency(analysis.FeasibilityScore, analysis.FeasibilityVerdict);
        if (scoreVerdictMismatch != null)
        {
            warnings.Add(scoreVerdictMismatch);
        }

        // 3. Pros/Cons Quality Check
        if (analysis.Pros.Count == 0 && analysis.Cons.Count == 0)
        {
            warnings.Add("No pros or cons provided - analysis may be incomplete");
        }

        // 4. Contradictory Pros/Cons Detection
        var contradictions = DetectContradictions(analysis.Pros, analysis.Cons);
        if (contradictions.Any())
        {
            warnings.AddRange(contradictions.Select(c => $"Potential contradiction: {c}"));
        }

        // 5. Risk Assessment Validation
        if (analysis.FeasibilityScore < 70 && analysis.Risks.Count == 0)
        {
            warnings.Add("Low feasibility score but no risks identified - analysis may be incomplete");
        }

        foreach (var risk in analysis.Risks)
        {
            if (string.IsNullOrWhiteSpace(risk.Mitigation))
            {
                warnings.Add($"Risk '{risk.Description}' has no mitigation strategy");
            }
        }

        // 6. Executive Summary Quality
        if (string.IsNullOrWhiteSpace(analysis.ExecutiveSummary))
        {
            errors.Add("Executive summary is missing");
        }
        else if (analysis.ExecutiveSummary.Length < 50)
        {
            warnings.Add("Executive summary is very brief - may lack detail");
        }

        // 7. Recommendations Validation
        if (analysis.FeasibilityScore < 70 && analysis.Recommendations.Count == 0)
        {
            warnings.Add("Low feasibility but no recommendations provided");
        }

        // 8. Confidence Level Validation
        if (analysis.ConfidenceLevel < 0 || analysis.ConfidenceLevel > 1)
        {
            errors.Add($"Confidence level {analysis.ConfidenceLevel} out of valid range (0-1)");
        }

        // 9. Optimized Solution Validation
        if (analysis.OptimizedSolution != null)
        {
            if (analysis.OptimizedSolution.SuccessProbability <= analysis.FeasibilityScore)
            {
                warnings.Add($"Optimized solution success probability ({analysis.OptimizedSolution.SuccessProbability}) not higher than current plan ({analysis.FeasibilityScore})");
            }
        }

        // 10. Context Relevance Check (basic keyword matching)
        var contextRelevance = CheckContextRelevance(originalContext, analysis);
        if (!contextRelevance)
        {
            warnings.Add("Analysis may not be relevant to the original decision context");
        }

        _logger.LogDebug("[VALIDATION] Analysis validation completed: {ErrorCount} errors, {WarningCount} warnings", 
            errors.Count, warnings.Count);

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings,
            Score = CalculateQualityScore(errors.Count, warnings.Count, analysis)
        };
    }

    private string? CheckScoreVerdictConsistency(double score, string verdict)
    {
        return (score, verdict.ToUpper()) switch
        {
            ( >= 70, "FEASIBLE") => null,
            ( >= 40 and < 70, "RISKY_BUT_POSSIBLE") => null,
            ( >= 20 and < 40, "NEEDS_ADJUSTMENT") => null,
            ( < 20, "NOT_FEASIBLE") => null,
            _ => $"Feasibility verdict '{verdict}' inconsistent with score {score}"
        };
    }

    private List<string> DetectContradictions(List<string> pros, List<string> cons)
    {
        var contradictions = new List<string>();
        var prosLower = pros.Select(p => p.ToLower()).ToList();
        var consLower = cons.Select(c => c.ToLower()).ToList();

        // Simple keyword-based contradiction detection
        var oppositeKeywords = new Dictionary<string, string[]>
        {
            ["fast"] = new[] { "slow", "delayed", "lengthy" },
            ["cheap"] = new[] { "expensive", "costly", "high cost" },
            ["simple"] = new[] { "complex", "complicated", "difficult" },
            ["reliable"] = new[] { "unreliable", "risky", "unstable" },
            ["scalable"] = new[] { "limited", "constrained", "inflexible" }
        };

        foreach (var (keyword, opposites) in oppositeKeywords)
        {
            var proHasKeyword = prosLower.Any(p => p.Contains(keyword));
            var conHasOpposite = consLower.Any(c => opposites.Any(o => c.Contains(o)));

            if (proHasKeyword && conHasOpposite)
            {
                contradictions.Add($"Pro mentions '{keyword}' but con mentions opposite concept");
            }
        }

        return contradictions;
    }

    private bool CheckContextRelevance(string context, DecisionAnalysis analysis)
    {
        // Extract key nouns from context (simple approach)
        var contextLower = context.ToLower();
        var summaryLower = analysis.ExecutiveSummary.ToLower();

        // Check if executive summary mentions similar concepts
        var commonWords = new[] { "software", "app", "development", "project", "budget", "timeline", 
            "team", "resources", "construction", "building", "finance", "investment", "marketing", 
            "product", "launch", "strategy", "plan", "decision" };

        var contextHasWords = commonWords.Where(w => contextLower.Contains(w)).ToList();
        if (contextHasWords.Count == 0) return true; // Can't determine, assume valid

        var summaryHasWords = contextHasWords.Where(w => summaryLower.Contains(w)).ToList();
        
        // At least 30% of key context words should appear in summary
        return summaryHasWords.Count >= (contextHasWords.Count * 0.3);
    }

    private double CalculateQualityScore(int errorCount, int warningCount, DecisionAnalysis analysis)
    {
        double score = 100.0;
        
        // Deduct points for issues
        score -= errorCount * 20;  // Critical errors
        score -= warningCount * 5;  // Warnings
        
        // Add points for completeness
        if (analysis.Pros.Count > 0) score += 5;
        if (analysis.Cons.Count > 0) score += 5;
        if (analysis.Risks.Count > 0) score += 5;
        if (analysis.Recommendations.Count > 0) score += 5;
        if (analysis.OptimizedSolution != null) score += 10;
        
        return Math.Max(0, Math.Min(100, score));
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public double Score { get; set; } // 0-100 quality score
}
