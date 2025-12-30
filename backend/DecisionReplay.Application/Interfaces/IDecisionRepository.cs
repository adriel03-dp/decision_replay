using DecisionReplay.Domain.Entities;

namespace DecisionReplay.Application.Interfaces;

public interface IDecisionRepository
{
    Task<Decision> CreateAsync(Decision decision);
    Task AppendEventAsync(DecisionEvent decisionEvent);
    Task<IReadOnlyList<DecisionEvent>> GetEventsAsync(Guid decisionId);
    Task<Decision?> GetByIdAsync(Guid decisionId);
}
