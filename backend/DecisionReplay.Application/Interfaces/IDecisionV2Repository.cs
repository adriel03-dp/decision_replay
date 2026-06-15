using DecisionReplay.Domain.Entities;

namespace DecisionReplay.Application.Interfaces;

/// <summary>
/// Repository interface for DecisionV2 entities (Domain-Agnostic architecture)
/// </summary>
public interface IDecisionV2Repository
{
    Task<DecisionV2> CreateAsync(DecisionV2 decision);
    Task<DecisionV2?> GetByIdAsync(Guid decisionId);
    Task<DecisionV2?> GetByIdForUserAsync(Guid decisionId, string userId);
    Task<IReadOnlyList<DecisionV2>> GetAllAsync();
    Task<IReadOnlyList<DecisionV2>> GetByUserAsync(string userId);
    Task UpdateAsync(DecisionV2 decision);
    Task DeleteAsync(Guid decisionId);
    Task<bool> DeleteForUserAsync(Guid decisionId, string userId);
}
