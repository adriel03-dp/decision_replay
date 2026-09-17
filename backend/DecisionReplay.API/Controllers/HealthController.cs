using DecisionReplay.Application.Interfaces;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using DecisionReplay.Infrastructure.Services;
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
    private readonly OllamaLanguageService _ollama;

    public HealthController(
        IMongoClient mongo,
        MongoSettings settings,
        IAiLanguageService languageService,
        OllamaLanguageService ollama)
    {
        _mongo = mongo;
        _settings = settings;
        _languageService = languageService;
        _ollama = ollama;
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

        var ollama = await _ollama.CheckAvailabilityAsync(cancellationToken);
        var healthy = mongoHealthy && ollama.Status == "healthy";
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
                    status = _languageService.IsConfigured ? "configured" : "fallback",
                    provider = "Groq",
                    capability = "Structured field extraction",
                    planGradingAuthority = false
                },
                ollama = new
                {
                    status = ollama.Status,
                    model = ollama.Model,
                    error = ollama.Error,
                    capability = "Explanations, plan wording, replay summaries",
                    planGradingAuthority = false
                },
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
