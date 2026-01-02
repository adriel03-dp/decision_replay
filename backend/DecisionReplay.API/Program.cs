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

// Global Exception Handler (Production-ready)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add HttpClient for external API calls
builder.Services.AddHttpClient();

// MongoDB Configuration
var mongoSettings = new MongoSettings
{
    ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Mongo connection string missing"),
    DatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
        ?? "DecisionReplay"
};

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IDecisionRepository, DecisionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// V1 Services (Legacy - keeping for backward compatibility)
builder.Services.AddScoped<IAIReasoningService, GeminiReasoningService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<DecisionService>();

// V2 Services (Refactored - Domain-Agnostic Architecture)
// SOLID: Dependency Inversion - Register interfaces with implementations
builder.Services.AddScoped<IIntentParser, GeminiIntentParser>();
builder.Services.AddScoped<IAIReasoningServiceV2, GeminiReasoningServiceV2>();
builder.Services.AddScoped<IReplayEngine, DecisionReplayEngine>();
builder.Services.AddScoped<IVisualizationProvider, VisualizationProvider>();
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

// Global Exception Handler
app.UseExceptionHandler(options => { }); // Enables IExceptionHandler

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Rate Limiting Middleware (must be after authentication)
app.UseMiddleware<GeminiRateLimitMiddleware>();

app.MapControllers();

app.Run();
