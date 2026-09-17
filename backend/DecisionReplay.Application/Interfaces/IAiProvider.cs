using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

public interface IAiProvider
{
    string Name { get; }
    Task<AiAvailability> CheckAvailabilityAsync(string model, CancellationToken cancellationToken = default);
    Task<AiGenerationResponse> GenerateAsync(AiGenerationRequest request, CancellationToken cancellationToken = default);
}

public interface IAiProviderResolver
{
    IAiProvider Resolve(string provider);
    AiTarget DefaultTarget(string operation);
    AiTarget? FallbackTarget { get; }
    int MaxRetries { get; }
    TimeSpan RequestTimeout { get; }
    bool DeterministicFallback { get; }
    AiParameters Parameters { get; }
}

public interface IPromptCatalog
{
    PromptTemplate Resolve(string operation, string version);
    IReadOnlyList<PromptTemplate> List();
}

public interface IGoldenDatasetCatalog
{
    GoldenDataset Resolve(string version);
}

public interface IAiRunRepository
{
    Task SaveExecutionAsync(AiExecution execution, CancellationToken cancellationToken = default);
    Task<AiExecution?> GetExecutionAsync(Guid id, string ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiExecution>> GetExecutionsAsync(string ownerId, Guid? decisionId = null,
        CancellationToken cancellationToken = default);
    Task SaveExperimentAsync(AiExperiment experiment, CancellationToken cancellationToken = default);
    Task<AiExperiment?> GetExperimentAsync(Guid id, string ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiExperiment>> GetExperimentsAsync(string ownerId, CancellationToken cancellationToken = default);
}
