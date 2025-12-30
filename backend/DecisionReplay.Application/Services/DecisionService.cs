using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.Enums;

namespace DecisionReplay.Application.Services;

public class DecisionService
{
    private readonly IDecisionRepository _repository;
    private readonly IAIReasoningService _aiService;

    public DecisionService(
        IDecisionRepository repository,
        IAIReasoningService aiService)
    {
        _repository = repository;
        _aiService = aiService;
    }

    public async Task<Decision> CreateDecisionAsync(string type, string createdBy)
    {
        var decision = new Decision(type, createdBy);
        await _repository.CreateAsync(decision);

        var inputEvent = new DecisionEvent(
            decision.Id,
            DecisionEventType.InputCaptured,
            new { type, createdBy });

        await _repository.AppendEventAsync(inputEvent);

        return decision;
    }

    public async Task RunAIReasoningAsync(Guid decisionId)
    {
        var decision = await _repository.GetByIdAsync(decisionId)
            ?? throw new InvalidOperationException("Decision not found");

        var reasoning = await _aiService.GenerateReasoningAsync(decision);

        var aiEvent = new DecisionEvent(
            decision.Id,
            DecisionEventType.AIReasoningGenerated,
            reasoning);

        await _repository.AppendEventAsync(aiEvent);
    }
}
