using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.API.DTOs;

public static class DecisionRequestMapper
{
    public static StructuredDecisionData Structured(AnalyzeStructuredDecisionRequest request)
    {
        if (request.Fields.Count > 100 || request.Fields.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > 100 || pair.Value == null || pair.Value.Length > 20000))
            throw new ArgumentException("Supply at most 100 bounded, non-null fields.");
        return new()
        {
            Domain = request.Domain.Trim(), Title = request.Title.Trim(), Goal = request.Goal.Trim(),
            Fields = new(request.Fields, StringComparer.OrdinalIgnoreCase)
        };
    }
}
