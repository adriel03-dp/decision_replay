using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace DecisionReplay.Infrastructure.AI;

public static class AiServiceRegistration
{
    public static IServiceCollection AddDecisionReplayAi(this IServiceCollection services, AiConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddHttpClient();
        services.AddSingleton<IPromptCatalog, EmbeddedPromptCatalog>();
        services.AddSingleton<IGoldenDatasetCatalog, EmbeddedGoldenDatasetCatalog>();
        services.AddScoped<IAiProvider, OllamaProvider>();
        services.AddScoped<IAiProvider, GroqProvider>();
        services.AddScoped<IAiProviderResolver, AiProviderResolver>();
        services.AddScoped<AiExecutionScope>();
        services.AddScoped<AiWorkflowService>();
        services.AddScoped<IAiLanguageService, AiLanguageService>();
        services.AddScoped<EvaluationService>();
        services.AddScoped<HistoricalReplayService>();
        return services;
    }
}
