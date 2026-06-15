using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace DecisionReplay.Infrastructure.Persistence.Repositories;

/// <summary>
/// MongoDB repository for DecisionV2 entities
/// Provides persistence for domain-agnostic decision architecture
/// </summary>
public sealed class DecisionV2Repository : IDecisionV2Repository
{
    private readonly IMongoCollection<DecisionV2> _decisions;

    public DecisionV2Repository(MongoContext context)
    {
        _decisions = context.GetCollection<DecisionV2>(MongoCollections.DecisionsV2);
    }

    public async Task<DecisionV2> CreateAsync(DecisionV2 decision)
    {
        await _decisions.InsertOneAsync(decision);
        return decision;
    }

    public async Task<DecisionV2?> GetByIdAsync(Guid decisionId)
    {
        return await _decisions
            .Find(d => d.Id == decisionId)
            .FirstOrDefaultAsync();
    }

    public async Task<DecisionV2?> GetByIdForUserAsync(Guid decisionId, string userId)
    {
        return await _decisions
            .Find(d => d.Id == decisionId && d.CreatedBy == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<DecisionV2>> GetAllAsync()
    {
        return await _decisions
            .Find(_ => true)
            .SortByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<DecisionV2>> GetByUserAsync(string userId)
    {
        return await _decisions
            .Find(d => d.CreatedBy == userId)
            .SortByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(DecisionV2 decision)
    {
        await _decisions.ReplaceOneAsync(
            d => d.Id == decision.Id,
            decision
        );
    }

    public async Task DeleteAsync(Guid decisionId)
    {
        await _decisions.DeleteOneAsync(d => d.Id == decisionId);
    }

    public async Task<bool> DeleteForUserAsync(Guid decisionId, string userId)
    {
        var result = await _decisions.DeleteOneAsync(
            d => d.Id == decisionId && d.CreatedBy == userId);
        return result.DeletedCount > 0;
    }
}
