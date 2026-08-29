using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace DecisionReplay.Api.Tests;

public sealed class WorkflowApiFactory : WebApplicationFactory<Program>
{
    private const string TestKey = "decision-replay-api-test-only-signing-secret-not-a-production-key";
    private readonly Dictionary<string, string?> _previous = new();
    public WorkflowApiFactory()
    {
        foreach (var pair in new Dictionary<string, string>
        {
            ["AI_PROVIDER"] = "none", ["AI_INTERACTIVE_WORDING"] = "false", ["AI_EXTRACTION_PROVIDER"] = "",
            ["AI_FALLBACK_PROVIDER"] = "", ["AI_FALLBACK_MODEL"] = "", ["DECISION_REPLAY_USE_IN_MEMORY"] = "true",
            ["JWT_SECRET"] = TestKey, ["JWT_ISSUER"] = "DecisionReplayTests",
            ["MONGODB_CONNECTION_STRING"] = "mongodb://127.0.0.1:27017", ["MONGODB_DATABASE_NAME"] = "DecisionReplayApiTests"
        })
        { _previous[pair.Key] = Environment.GetEnvironmentVariable(pair.Key); Environment.SetEnvironmentVariable(pair.Key, pair.Value); }
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDecisionV2Repository>(); services.RemoveAll<IAiRunRepository>();
            services.AddSingleton<IDecisionV2Repository, InMemoryDecisionV2Repository>();
            services.AddSingleton<IAiRunRepository, InMemoryAiRunRepository>();
        });
    }
    public HttpClient AuthenticatedClient(string? owner = null)
    {
        var client = CreateClient();
        var token = new JwtSecurityToken(issuer: "DecisionReplayTests", audience: "DecisionReplayTests",
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, owner ?? Guid.NewGuid().ToString()) }, expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(TestKey)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var pair in _previous) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
    }
}
