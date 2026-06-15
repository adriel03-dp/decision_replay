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
    private readonly IAiLanguageService _languageService;

    public HealthController(
        IMongoClient mongo,
        MongoSettings settings,
        IAiLanguageService languageService)
    {
        _mongo = mongo;
        _settings = settings;
        _languageService = languageService;
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
        catch (Exception ex)
        {
            mongoHealthy = false;
            mongoError = ex.Message;
        }

        var response = new
        {
            status = mongoHealthy ? "healthy" : "degraded",
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
                    status = _languageService.IsConfigured ? "configured" : "fallback",
                    provider = "Groq",
                    scoringAuthority = false
                },
                decisionEngine = new
                {
                    status = "healthy",
                    scoring = "deterministic",
                    rulesVersion = "1.0"
                }
            }
        };
        return mongoHealthy ? Ok(response) : StatusCode(503, response);
    }
}
