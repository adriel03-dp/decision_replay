using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace DecisionReplay.Infrastructure.Persistence.Repositories;

public sealed class DecisionRepository : IDecisionRepository
{
    private readonly IMongoCollection<Decision> _decisions;
    private readonly IMongoCollection<DecisionEvent> _events;

    public DecisionRepository(MongoContext context)
    {
        _decisions = context.GetCollection<Decision>(MongoCollections.Decisions);
        _events = context.GetCollection<DecisionEvent>(MongoCollections.DecisionEvents);
    }

    public async Task<Decision> CreateAsync(Decision decision)
    {
        await _decisions.InsertOneAsync(decision);
        return decision;
    }

    public async Task AppendEventAsync(DecisionEvent decisionEvent)
    {
        await _events.InsertOneAsync(decisionEvent);
    }

    public async Task<IReadOnlyList<DecisionEvent>> GetEventsAsync(Guid decisionId)
    {
        return await _events
            .Find(e => e.DecisionId == decisionId)
            .SortBy(e => e.Timestamp)
            .ToListAsync();
    }

    public async Task<Decision?> GetByIdAsync(Guid decisionId)
    {
        return await _decisions
            .Find(d => d.Id == decisionId)
            .FirstOrDefaultAsync();
    }
}
