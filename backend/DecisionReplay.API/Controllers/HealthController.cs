using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using System.Net.Http;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoSettings _mongoSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IMongoClient mongoClient,
        MongoSettings mongoSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<HealthController> logger)
    {
        _mongoClient = mongoClient;
        _mongoSettings = mongoSettings;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check - returns 200 if API is running
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "DecisionReplay API",
            version = "2.0"
        });
    }

    /// <summary>
    /// Comprehensive health check - validates all dependencies
    /// Checks: MongoDB connection, Gemini API availability, memory usage
    /// </summary>
    [HttpGet("health/detailed")]
    public async Task<IActionResult> DetailedHealth()
    {
        var healthChecks = new Dictionary<string, object>();
        var overallHealthy = true;

        // 1. MongoDB Health Check
        var mongoHealth = await CheckMongoDbHealthAsync();
        healthChecks["mongodb"] = mongoHealth;
        if (mongoHealth.status != "healthy") overallHealthy = false;

        // 2. Gemini API Keys Configuration Check
        var geminiHealth = CheckGeminiApiKeysHealth();
        healthChecks["gemini_api"] = geminiHealth;
        if (geminiHealth.status != "healthy") overallHealthy = false;

        // 3. Memory Usage Check
        var memoryHealth = CheckMemoryHealth();
        healthChecks["memory"] = memoryHealth;
        if (memoryHealth.status == "critical") overallHealthy = false;

        // 4. Environment Variables Check
        var envHealth = CheckEnvironmentVariables();
        healthChecks["environment"] = envHealth;
        if (envHealth.status != "healthy") overallHealthy = false;

        var response = new
        {
            status = overallHealthy ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            service = "DecisionReplay API",
            version = "2.0",
            checks = healthChecks
        };

        return overallHealthy ? Ok(response) : StatusCode(503, response);
    }

    private async Task<dynamic> CheckMongoDbHealthAsync()
    {
        try
        {
            var database = _mongoClient.GetDatabase(_mongoSettings.DatabaseName);
            await (await database.ListCollectionNamesAsync()).ToListAsync();
            
            return new
            {
                status = "healthy",
                message = "MongoDB connection successful",
                database = _mongoSettings.DatabaseName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return new
            {
                status = "unhealthy",
                message = "MongoDB connection failed",
                error = ex.Message
            };
        }
    }

    private dynamic CheckGeminiApiKeysHealth()
    {
        var key1 = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var key2 = Environment.GetEnvironmentVariable("GEMINI_API_KEY_2");
        var key3 = Environment.GetEnvironmentVariable("GEMINI_API_KEY_3");

        var keyCount = 0;
        if (!string.IsNullOrEmpty(key1)) keyCount++;
        if (!string.IsNullOrEmpty(key2)) keyCount++;
        if (!string.IsNullOrEmpty(key3)) keyCount++;

        if (keyCount == 0)
        {
            return new
            {
                status = "unhealthy",
                message = "No Gemini API keys configured",
                configured_keys = 0
            };
        }

        return new
        {
            status = "healthy",
            message = "Gemini API keys configured",
            configured_keys = keyCount
        };
    }

    private dynamic CheckMemoryHealth()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var memoryMB = process.WorkingSet64 / 1024 / 1024;
        
        string status;
        if (memoryMB < 500) status = "healthy";
        else if (memoryMB < 1000) status = "warning";
        else status = "critical";

        return new
        {
            status,
            message = $"Memory usage: {memoryMB} MB",
            memory_mb = memoryMB,
            threshold_warning = 500,
            threshold_critical = 1000
        };
    }

    private dynamic CheckEnvironmentVariables()
    {
        var requiredVars = new[] { "MONGODB_CONNECTION_STRING", "JWT_SECRET", "GEMINI_API_KEY" };
        var missingVars = requiredVars.Where(v => string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v))).ToList();

        if (missingVars.Any())
        {
            return new
            {
                status = "unhealthy",
                message = "Required environment variables missing",
                missing = missingVars
            };
        }

        return new
        {
            status = "healthy",
            message = "All required environment variables configured"
        };
    }
}

