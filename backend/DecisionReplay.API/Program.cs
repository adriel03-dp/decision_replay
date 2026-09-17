using System.Text;
using DecisionReplay.API.Middleware;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using DecisionReplay.Infrastructure.Persistence.Repositories;
using DecisionReplay.Infrastructure.Services;
using DecisionReplay.Infrastructure.AI;
using DecisionReplay.Domain.ValueObjects;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath)) Env.NoClobber().Load(envPath);
else Env.NoClobber().Load();

var checkOllama = args.Contains("--check-ollama");
var evaluate = args.Contains("--evaluate");
string Option(string name, string fallback) => args.FirstOrDefault(arg => arg.StartsWith($"--{name}=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? fallback;
var builder = WebApplication.CreateBuilder(args.Where(arg => arg != "--check-ollama" && arg != "--evaluate"
    && !arg.StartsWith("--evaluation-") && !arg.StartsWith("--prompt-version=")).ToArray());
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
var aiConfiguration = AiConfiguration.FromEnvironment();
if (checkOllama) aiConfiguration.ValidateTarget(new("ollama", aiConfiguration.OllamaModel));
else if (evaluate) aiConfiguration.ValidateTarget(new(Option("evaluation-provider", aiConfiguration.Target("decision-analysis").Provider),
    Option("evaluation-model", aiConfiguration.Target("decision-analysis").Model)));
else aiConfiguration.ValidateSelectedProviders();
builder.Services.AddDecisionReplayAi(aiConfiguration);
builder.Services.AddSingleton<DomainTemplateService>();
builder.Services.AddScoped<DecisionValidationService>();
builder.Services.AddScoped<FeasibilityScoringService>();

// Run the same Ollama client in the API's runtime, without database or auth dependencies.
if (checkOllama || evaluate)
{
    builder.Services.AddSingleton<IAiRunRepository, InMemoryAiRunRepository>();
    builder.Services.AddSingleton<IDecisionV2Repository, InMemoryDecisionV2Repository>();
    await using var checkApp = builder.Build();
    using var scope = checkApp.Services.CreateScope();
    if (evaluate)
    {
        var target = new AiTarget(Option("evaluation-provider", aiConfiguration.Target("decision-analysis").Provider),
            Option("evaluation-model", aiConfiguration.Target("decision-analysis").Model));
        var results = await scope.ServiceProvider.GetRequiredService<EvaluationService>().RunAsync("cli", "v1", Option("prompt-version", "v1"), new[] { target });
        var records = await scope.ServiceProvider.GetRequiredService<IAiRunRepository>().GetExecutionsAsync("cli");
        var output = Path.GetFullPath(Option("evaluation-output", Path.Combine(builder.Environment.ContentRootPath,
            "..", "..", "artifacts", "evaluations", $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json")));
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        await File.WriteAllTextAsync(output, System.Text.Json.JsonSerializer.Serialize(new { experiments = results, executions = records },
            new System.Text.Json.JsonSerializerOptions(AiWorkflowService.Json) { WriteIndented = true }));
        Console.WriteLine($"Evaluation results: {output}");
        Environment.ExitCode = results.Any(result => result.Status != "completed") ? 1 : 0;
        return;
    }
    var providers = scope.ServiceProvider.GetRequiredService<IAiProviderResolver>();
    var availability = await providers.Resolve("ollama").CheckAvailabilityAsync(aiConfiguration.OllamaModel);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(availability));
    if (availability.Status != "healthy") { Environment.ExitCode = 1; return; }
    try
    {
        var answer = await scope.ServiceProvider.GetRequiredService<AiWorkflowService>().GenerateAsync<System.Text.Json.JsonElement>("replay-summary", "v1",
            new { scoreDelta = 0, mainReason = "Smoke check: no inputs changed." },
            result => result.ValueKind == System.Text.Json.JsonValueKind.Object && result.TryGetProperty("text", out var text)
                && text.ValueKind == System.Text.Json.JsonValueKind.String && AiOutputValidation.Text(text.GetString()) ? null : "Missing summary text.",
            new("cli", Guid.NewGuid().ToString()), new("ollama", aiConfiguration.OllamaModel));
        Console.WriteLine($"Ollama inference succeeded. ExecutionId={answer.ExecutionId}");
    }
    catch (AiException ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
    return;
}

MongoDbConfiguration.Configure();
var mongoSettings = new MongoSettings
{
    ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? throw new InvalidOperationException("MONGODB_CONNECTION_STRING is not configured."),
    DatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME") ?? "DecisionReplay"
};
var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoSettings.ConnectionString);
mongoClientSettings.MaxConnectionPoolSize = 200;
mongoClientSettings.MinConnectionPoolSize = 5;
mongoClientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
mongoClientSettings.SocketTimeout = TimeSpan.FromSeconds(15);

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoClientSettings));
builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoContext>();
var useInMemoryDecisions =
    builder.Environment.IsDevelopment()
    && string.Equals(
        Environment.GetEnvironmentVariable("DECISION_REPLAY_USE_IN_MEMORY"),
        "true",
        StringComparison.OrdinalIgnoreCase);
if (useInMemoryDecisions)
{
    builder.Services.AddSingleton<IDecisionV2Repository, InMemoryDecisionV2Repository>();
    builder.Services.AddSingleton<IAiRunRepository, InMemoryAiRunRepository>();
}
else
{
    builder.Services.AddScoped<IDecisionV2Repository, DecisionV2Repository>();
    builder.Services.AddScoped<IAiRunRepository, MongoAiRunRepository>();
}
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<DecisionParserService>();
builder.Services.AddScoped<ReplayComparisonService>();
builder.Services.AddScoped<PlanReplayComparisonService>();
builder.Services.AddScoped<PlanPhaseBuilderService>();
builder.Services.AddScoped<ActionPlanService>();
builder.Services.AddScoped<ExplanationService>();
builder.Services.AddScoped<AuditTrailService>();
builder.Services.AddScoped<DecisionEngineService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<PlanExportService>();

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET is not configured.");
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy
        .WithOrigins(
            "http://localhost:3000",
            "http://localhost:3001",
            "https://localhost:3000",
            "https://localhost:3001")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

var app = builder.Build();
app.UseResponseCompression();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.RequestServices.GetRequiredService<AiExecutionScope>().Context = new(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous", context.TraceIdentifier);
    await next();
});
app.UseMiddleware<AiLanguageRateLimitMiddleware>();
app.MapControllers();
app.Run();

public partial class Program;
