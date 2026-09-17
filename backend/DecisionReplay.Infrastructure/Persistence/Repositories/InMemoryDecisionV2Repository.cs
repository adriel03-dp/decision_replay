using System.Collections.Concurrent;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Application.Services;

namespace DecisionReplay.Infrastructure.Persistence.Repositories;

public sealed class InMemoryDecisionV2Repository : IDecisionV2Repository
{
    private readonly ConcurrentDictionary<Guid, DecisionV2> _decisions = new();
    private readonly object _gate = new();

    public Task<DecisionV2> CreateAsync(DecisionV2 decision)
    {
        if (!_decisions.TryAdd(decision.Id, ReplayContextService.Copy(decision)))
            throw new InvalidOperationException("A decision with this ID already exists.");
        return Task.FromResult(decision);
    }

    public Task<DecisionV2?> GetByIdAsync(Guid decisionId) =>
        Task.FromResult(_decisions.TryGetValue(decisionId, out var decision) ? ReplayContextService.Copy(decision) : null);

    public Task<DecisionV2?> GetByIdForUserAsync(Guid decisionId, string userId)
    {
        var decision = _decisions.GetValueOrDefault(decisionId);
        return Task.FromResult(decision?.CreatedBy == userId ? ReplayContextService.Copy(decision) : null);
    }

    public Task<IReadOnlyList<DecisionV2>> GetAllAsync() =>
        Task.FromResult<IReadOnlyList<DecisionV2>>(
            _decisions.Values.OrderByDescending(item => item.CreatedAt).Select(ReplayContextService.Copy).ToList());

    public Task<IReadOnlyList<DecisionV2>> GetByUserAsync(string userId) =>
        Task.FromResult<IReadOnlyList<DecisionV2>>(
            _decisions.Values
                .Where(item => item.CreatedBy == userId)
                .OrderByDescending(item => item.CreatedAt).Select(ReplayContextService.Copy)
                .ToList());

    public Task UpdateAsync(DecisionV2 decision)
    {
        lock (_gate)
        {
            if (!_decisions.TryGetValue(decision.Id, out var stored) || stored.CreatedBy != decision.CreatedBy || stored.Revision != decision.Revision)
                throw new DecisionConflictException();
            decision.Revision++;
            _decisions[decision.Id] = ReplayContextService.Copy(decision);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid decisionId)
    {
        _decisions.TryRemove(decisionId, out _);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteForUserAsync(Guid decisionId, string userId)
    {
        var decision = _decisions.GetValueOrDefault(decisionId);
        return Task.FromResult(
            decision?.CreatedBy == userId && _decisions.TryRemove(decisionId, out _));
    }
}
