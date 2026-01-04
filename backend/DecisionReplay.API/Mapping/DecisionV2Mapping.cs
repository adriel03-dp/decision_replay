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
            analysis.Pros,
            analysis.Cons,
            analysis.Risks.Select(r => new RiskResponse(r.Description, r.Impact, r.Mitigation)).ToList(),
            analysis.Assumptions,
            analysis.Recommendations,
            analysis.ConfidenceLevel,
            analysis.GeneratedAt,
            analysis.ModelUsed
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
}
