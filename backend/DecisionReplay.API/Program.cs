using System.Text;
using DecisionReplay.API.Middleware;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Infrastructure.Persistence.Mongo;
using DecisionReplay.Infrastructure.Persistence.Repositories;
using DecisionReplay.Infrastructure.Services;
using DotNetEnv;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath)) Env.Load(envPath);
else Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient();

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
    builder.Services.AddSingleton<IDecisionV2Repository, InMemoryDecisionV2Repository>();
else
    builder.Services.AddScoped<IDecisionV2Repository, DecisionV2Repository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IAiLanguageService, GroqLanguageService>();
builder.Services.AddSingleton<DomainTemplateService>();
builder.Services.AddScoped<DecisionParserService>();
builder.Services.AddScoped<DecisionValidationService>();
builder.Services.AddScoped<FeasibilityScoringService>();
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
app.UseMiddleware<AiLanguageRateLimitMiddleware>();
app.MapControllers();
app.Run();

public partial class Program;
