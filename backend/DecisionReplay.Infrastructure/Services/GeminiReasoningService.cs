using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;

namespace DecisionReplay.Infrastructure.Services;

public class GeminiReasoningService : IAIReasoningService
{
    public Task<object> GenerateReasoningAsync(Decision decision)
    {
        // TODO: Implement Gemini API integration
        var stubReasoning = new
        {
            DecisionId = decision.Id,
            Analysis = "AI reasoning placeholder - Gemini integration pending",
            Recommendation = "Pending AI analysis",
            Confidence = 0.0
        };

        return Task.FromResult<object>(stubReasoning);
    }
}
