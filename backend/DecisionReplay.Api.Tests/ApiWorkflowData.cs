using DecisionReplay.API.DTOs;
using System.Text.Json;

namespace DecisionReplay.Api.Tests;

internal static class ApiWorkflowData
{
    public const string Narrative = "Build a mobile app in six months with team six and budget 100000 USD; scope: login, payments, reporting; monthly_cost_per_person: 2000; technical_readiness: partial";
    public static AnalyzeStructuredDecisionRequest Request() => new()
    {
        Domain = "project_planning", Title = "API workflow example", Goal = "Deliver a mobile application", OriginalInput = Narrative,
        Fields = new() { ["project_goal"] = "Deliver a mobile application", ["budget"] = "100000", ["currency"] = "USD",
            ["team_size"] = "6", ["timeline_months"] = "6", ["scope"] = "login, payments, reporting", ["required_scope"] = "reporting",
            ["monthly_cost_per_person"] = "2000", ["technical_readiness"] = "partial" }
    };
    public static async Task<T> Success<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"HTTP {(int)response.StatusCode}: {body[..Math.Min(2000, body.Length)]}");
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
    public static async Task<DecisionEngineResponse> Create(HttpClient client) =>
        await Success<DecisionEngineResponse>(await client.PostAsJsonAsync("/api/v2/decisions/analyze-structured", Request()));
}
