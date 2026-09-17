using DecisionReplay.Application.Interfaces;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Route("api")]
public sealed class HealthController : ControllerBase
{
    private readonly IMongoClient _mongo;
    private readonly MongoSettings _settings;
    private readonly IAiProviderResolver _providers;

    public HealthController(
        IMongoClient mongo,
        MongoSettings settings,
        IAiProviderResolver providers)
    {
        _mongo = mongo;
        _settings = settings;
        _providers = providers;
    }

    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "DecisionReplay API",
            version = "3.0"
        });

    [HttpGet("health/detailed")]
    public async Task<IActionResult> Detailed(CancellationToken cancellationToken)
    {
        var mongoHealthy = true;
        string? mongoError = null;
        try
        {
            await _mongo.GetDatabase(_settings.DatabaseName)
                .RunCommandAsync<object>(
                    new JsonCommand<object>("{ ping: 1 }"),
                    cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception)
        {
            mongoHealthy = false;
            mongoError = "MongoDB ping failed.";
        }

        var extraction = _providers.DefaultTarget("extraction");
        var analysis = _providers.DefaultTarget("decision-analysis");
        var aiChecks = new List<DecisionReplay.Domain.ValueObjects.AiAvailability>();
        foreach (var target in new[] { extraction, analysis }.Distinct())
            aiChecks.Add(await _providers.Resolve(target.Provider).CheckAvailabilityAsync(target.Model, cancellationToken));
        var healthy = mongoHealthy && aiChecks.All(check => check.Status is "healthy" or "configured");
        var response = new
        {
            status = healthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            service = "DecisionReplay API",
            version = "3.0",
            checks = new
            {
                mongodb = new
                {
                    status = mongoHealthy ? "healthy" : "unhealthy",
                    database = _settings.DatabaseName,
                    error = mongoError
                },
                languageInterface = new
                {
                    status = aiChecks.First(check => check.Provider == extraction.Provider && check.Model == extraction.Model).Status,
                    provider = extraction.Provider,
                    capability = "Structured field extraction",
                    planGradingAuthority = false
                },
                ai = aiChecks,
                decisionEngine = new
                {
                    status = "healthy",
                    grading = "structured plan assessment",
                    rulesVersion = "1.0"
                }
            }
        };
        return healthy ? Ok(response) : StatusCode(503, response);
    }
}
