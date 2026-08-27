using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.AI;

public sealed class DisabledAiProvider : IAiProvider
{
    public string Name => "none";
    public Task<AiAvailability> CheckAvailabilityAsync(string model, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(new AiAvailability("disabled", Name, "none")); }
    public Task<AiGenerationResponse> GenerateAsync(AiGenerationRequest request, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); throw new AiException("ai_disabled", "Optional AI is disabled. Configure AI_PROVIDER to enable advisory analysis."); }
}
