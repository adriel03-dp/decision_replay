using DecisionReplay.Domain.Entities;

namespace DecisionReplay.Application.Interfaces;

public interface IAIReasoningService
{
    Task<object> GenerateReasoningAsync(Decision decision);
}
