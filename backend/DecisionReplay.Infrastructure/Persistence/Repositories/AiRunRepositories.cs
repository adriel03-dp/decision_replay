using System.Collections.Concurrent;
using System.Text.Json;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;

namespace DecisionReplay.Infrastructure.Persistence.Repositories;

public sealed class MongoAiRunRepository(MongoContext context) : IAiRunRepository
{
    private readonly IMongoCollection<AiExecution> _executions = context.GetCollection<AiExecution>("ai_executions");
    private readonly IMongoCollection<AiExperiment> _experiments = context.GetCollection<AiExperiment>("ai_experiments");
    public Task SaveExecutionAsync(AiExecution value, CancellationToken token = default) =>
        _executions.ReplaceOneAsync(item => item.Id == value.Id && item.OwnerId == value.OwnerId, value, new ReplaceOptions { IsUpsert = true }, token);
    public async Task<AiExecution?> GetExecutionAsync(Guid id, string ownerId, CancellationToken token = default) =>
        await _executions.Find(item => item.Id == id && item.OwnerId == ownerId).FirstOrDefaultAsync(token);
    public async Task<IReadOnlyList<AiExecution>> GetExecutionsAsync(string ownerId, Guid? decisionId = null, CancellationToken token = default) =>
        await _executions.Find(item => item.OwnerId == ownerId && (decisionId == null || item.DecisionId == decisionId))
            .SortByDescending(item => item.StartedAt).Limit(100).ToListAsync(token);
    public Task SaveExperimentAsync(AiExperiment value, CancellationToken token = default) =>
        _experiments.ReplaceOneAsync(item => item.Id == value.Id && item.OwnerId == value.OwnerId, value, new ReplaceOptions { IsUpsert = true }, token);
    public async Task<AiExperiment?> GetExperimentAsync(Guid id, string ownerId, CancellationToken token = default) =>
        await _experiments.Find(item => item.Id == id && item.OwnerId == ownerId).FirstOrDefaultAsync(token);
    public async Task<IReadOnlyList<AiExperiment>> GetExperimentsAsync(string ownerId, CancellationToken token = default) =>
        await _experiments.Find(item => item.OwnerId == ownerId).SortByDescending(item => item.StartedAt).Limit(100).ToListAsync(token);
}

public sealed class InMemoryAiRunRepository : IAiRunRepository
{
    private readonly ConcurrentDictionary<Guid, AiExecution> _executions = new();
    private readonly ConcurrentDictionary<Guid, AiExperiment> _experiments = new();
    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, AiWorkflowService.Json), AiWorkflowService.Json)!;
    public Task SaveExecutionAsync(AiExecution value, CancellationToken token = default)
    { token.ThrowIfCancellationRequested(); _executions[value.Id] = Copy(value); return Task.CompletedTask; }
    public Task<AiExecution?> GetExecutionAsync(Guid id, string ownerId, CancellationToken token = default) =>
        Task.FromResult(_executions.TryGetValue(id, out var value) && value.OwnerId == ownerId ? Copy(value) : null);
    public Task<IReadOnlyList<AiExecution>> GetExecutionsAsync(string ownerId, Guid? decisionId = null, CancellationToken token = default) =>
        Task.FromResult<IReadOnlyList<AiExecution>>(_executions.Values.Where(item => item.OwnerId == ownerId && (decisionId == null || item.DecisionId == decisionId))
            .OrderByDescending(item => item.StartedAt).Take(100).Select(Copy).ToList());
    public Task SaveExperimentAsync(AiExperiment value, CancellationToken token = default)
    { token.ThrowIfCancellationRequested(); _experiments[value.Id] = Copy(value); return Task.CompletedTask; }
    public Task<AiExperiment?> GetExperimentAsync(Guid id, string ownerId, CancellationToken token = default) =>
        Task.FromResult(_experiments.TryGetValue(id, out var value) && value.OwnerId == ownerId ? Copy(value) : null);
    public Task<IReadOnlyList<AiExperiment>> GetExperimentsAsync(string ownerId, CancellationToken token = default) =>
        Task.FromResult<IReadOnlyList<AiExperiment>>(_experiments.Values.Where(item => item.OwnerId == ownerId).OrderByDescending(item => item.StartedAt).Take(100).Select(Copy).ToList());
}
