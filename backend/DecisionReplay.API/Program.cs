using DotNetEnv;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using DecisionReplay.Infrastructure.Persistence.Repositories;
using DecisionReplay.Infrastructure.Services;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MongoDB.Driver;

// Load .env from parent directory (backend folder)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    Env.Load(); // Try current directory
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Performance: Add response caching and compression
builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Performance: Add memory caching for frequently accessed data
builder.Services.AddMemoryCache();

// Global Exception Handler (Production-ready)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// MongoDB Configuration
MongoDbConfiguration.Configure(); // Initialize BSON serializers for entities with private setters

var mongoSettings = new MongoSettings
{
    ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Mongo connection string missing"),
    DatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
        ?? "DecisionReplay"
};

// Performance: Configure MongoDB client with optimized settings
var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoSettings.ConnectionString);
mongoClientSettings.MaxConnectionPoolSize = 200;
mongoClientSettings.MinConnectionPoolSize = 5;
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
mongoClientSettings.SocketTimeout = TimeSpan.FromSeconds(10);
mongoClientSettings.WaitQueueTimeout = TimeSpan.FromSeconds(3);

builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoClientSettings));
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IDecisionRepository, DecisionRepository>();
builder.Services.AddScoped<IDecisionV2Repository, DecisionV2Repository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// V1 Services (Legacy - keeping for backward compatibility)
// Register V2 services only (V1 disabled to avoid static resource conflicts)
builder.Services.AddScoped<IAuthService, AuthService>();
// V2 Services only (V1 removed to eliminate conflicts)

// V2 Services (Refactored - Domain-Agnostic Architecture)
// SOLID: Dependency Inversion - Register interfaces with implementations

// Configure HttpClient with optimized timeout for Gemini API calls
builder.Services.AddHttpClient<GeminiIntentParser>();

builder.Services.AddHttpClient<GeminiReasoningServiceV2>();

builder.Services.AddScoped<IIntentParser, GeminiIntentParser>();
builder.Services.AddScoped<IAIReasoningServiceV2, GeminiReasoningServiceV2>();
builder.Services.AddScoped<IReplayEngine, DecisionReplayEngine>();
builder.Services.AddScoped<IVisualizationProvider, VisualizationProvider>();
builder.Services.AddScoped<IInputValidationService, InputValidationService>();
builder.Services.AddScoped<DecisionServiceV2>();

// JWT Authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET not configured");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "DecisionReplay";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtIssuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret))
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:3001",
            "https://localhost:3000",
            "https://localhost:3001"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Performance: Enable response compression first
app.UseResponseCompression();

// Global Exception Handler
app.UseExceptionHandler(options => { }); // Enables IExceptionHandler

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ----------------------
// CORS + HTTPS ORDER FIX
// ----------------------

// 1. For development: skip HTTPS redirection to allow HTTP frontend
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 2. Apply CORS
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Performance: Add response caching
app.UseResponseCaching();

// Rate Limiting Middleware (must be after authentication)
app.UseMiddleware<GeminiRateLimitMiddleware>();

// Configure routing (V1 disabled - frontend uses V2 only)
app.MapControllers();

// FORCE CLEAR corrupted collections on startup due to GUID serialization issues
// Clean up old broken MongoDB documents on startup
try
{
    using var scope = app.Services.CreateScope();
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoContext>();

    // FORCE drop decision_events collection due to GUID serialization corruption
    await mongoContext.Database.DropCollectionAsync("decision_events");
    Console.WriteLine("[STARTUP] Force dropped decision_events collection due to GUID serialization issues");

    // Check DecisionsV2 collection
    var decisionsV2Collection = mongoContext.GetCollection<DecisionReplay.Domain.Entities.DecisionV2>("decisions_v2");
    try
    {
        var testDecision = await decisionsV2Collection.Find(MongoDB.Driver.Builders<DecisionReplay.Domain.Entities.DecisionV2>.Filter.Empty).FirstOrDefaultAsync();
        Console.WriteLine("[STARTUP] MongoDB DecisionsV2 collection is healthy");
    }
    catch
    {
        await mongoContext.Database.DropCollectionAsync("decisions_v2");
        Console.WriteLine("[STARTUP] Dropped corrupted decisions_v2 collection - starting fresh");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP] MongoDB cleanup warning: {ex.Message}");
}

app.Run();
