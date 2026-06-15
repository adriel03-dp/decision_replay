using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DecisionReplay.Infrastructure.Services;

public sealed class GroqDecisionAnalyzer : IAiDecisionAnalyzer
{
    private const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqDecisionAnalyzer> _logger;
    private readonly string? _apiKey;
    private readonly string _model;

    public GroqDecisionAnalyzer(
        IHttpClientFactory httpClientFactory,
        ILogger<GroqDecisionAnalyzer> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(GroqDecisionAnalyzer));
        _httpClient.Timeout = TimeSpan.FromSeconds(45);
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        _model = Environment.GetEnvironmentVariable("GROQ_MODEL")
            ?? "llama-3.3-70b-versatile";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<DecisionAnalysis?> AnalyzeAsync(
        ProjectInput input,
        FeasibilityResult feasibility,
        ProjectPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("[GROQ] GROQ_API_KEY is not configured");
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new
        {
            model = _model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                        You are a senior project delivery and risk analyst.
                        Return only valid JSON matching the requested schema.
                        Do not replace the supplied deterministic feasibility score.
                        Be specific to the supplied scope, budget, timeline, and team.
                        Never claim a project is guaranteed or 100% feasible.
                        """
                },
                new
                {
                    role = "user",
                    content = BuildPrompt(input, feasibility, plan)
                }
            }
        });

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[GROQ] Request failed with status {Status}: {Body}",
                    (int)response.StatusCode,
                    Truncate(responseBody, 500));
                return null;
            }

            using var envelope = JsonDocument.Parse(responseBody);
            var content = envelope.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("[GROQ] Response did not contain analysis content");
                return null;
            }

            var result = JsonSerializer.Deserialize<GroqAnalysis>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result == null ? null : ToDecisionAnalysis(result, feasibility);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[GROQ] Analysis timed out");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GROQ] Analysis failed");
            return null;
        }
    }

    private static string BuildPrompt(
        ProjectInput input,
        FeasibilityResult feasibility,
        ProjectPlan plan)
    {
        var project = JsonSerializer.Serialize(new
        {
            input.ProjectType,
            input.Features,
            input.BudgetUsd,
            input.TimelineMonths,
            input.TeamSize,
            input.RawInput,
            DeterministicAssessment = new
            {
                feasibility.Score,
                feasibility.Verdict,
                feasibility.BudgetFitScore,
                feasibility.TimelineFitScore,
                feasibility.TeamCapacityScore,
                feasibility.ComplexityScore,
                feasibility.EstimatedCostUsd,
                feasibility.EstimatedMonths,
                feasibility.RequiredTeamSize,
                Issues = feasibility.Issues
            },
            ExistingRoadmap = plan.Phases.Select(p => new
            {
                p.Name,
                p.DurationMonths,
                p.Tasks
            })
        });

        return $$"""
            Analyze this project:
            {{project}}

            Confidence means confidence in the quality of this assessment based on
            scope clarity and completeness, not probability of project success.

            Return this exact JSON shape:
            {
              "executiveSummary": "string",
              "confidenceLevel": 0.0,
              "pros": ["string"],
              "cons": ["string"],
              "assumptions": ["string"],
              "risks": [
                {
                  "description": "string",
                  "impact": "HIGH|MEDIUM|LOW",
                  "mitigation": "string"
                }
              ],
              "recommendations": ["string"],
              "optimizedPlan": {
                "improvedTimeline": "string",
                "clarifiedScope": "string",
                "budgetOptimization": "string",
                "resourceStrategy": "string",
                "targetFeasibility": 0.0
              },
              "roadmap": [
                {
                  "phase": "string",
                  "objective": "string",
                  "deliverables": ["string"],
                  "exitCriteria": ["string"]
                }
              ]
            }

            Rules:
            - confidenceLevel must be between 0 and 1.
            - targetFeasibility must be between the current score and 95.
            - Identify missing or ambiguous scope as assumptions or risks.
            - Recommendations must state concrete scope, budget, schedule, or staffing changes.
            - Roadmap phases must be actionable and ordered.
            """;
    }

    private DecisionAnalysis ToDecisionAnalysis(
        GroqAnalysis result,
        FeasibilityResult feasibility)
    {
        var target = feasibility.Score >= 95
            ? feasibility.Score
            : Math.Clamp(
                result.OptimizedPlan?.TargetFeasibility ?? feasibility.Score,
                feasibility.Score,
                95);

        var roadmap = (result.Roadmap ?? new List<GroqRoadmapPhase>())
            .Where(r => !string.IsNullOrWhiteSpace(r.Phase))
            .Select(r =>
                $"{r.Phase}: {r.Objective}. Deliverables: {string.Join(", ", r.Deliverables)}. " +
                $"Exit criteria: {string.Join(", ", r.ExitCriteria)}")
            .ToList();

        var analysis = new DecisionAnalysis(
            feasibility.Score,
            ToAiVerdict(feasibility.Score),
            result.ExecutiveSummary,
            result.Pros ?? new List<string>(),
            result.Cons ?? new List<string>(),
            (result.Risks ?? new List<GroqRisk>())
                .Where(r => !string.IsNullOrWhiteSpace(r.Description))
                .Select(r => new RiskFactor(
                    r.Description,
                    NormalizeImpact(r.Impact),
                    r.Mitigation))
                .ToList(),
            result.Assumptions ?? new List<string>(),
            (result.Recommendations ?? new List<string>()).Concat(roadmap).ToList(),
            Math.Clamp(result.ConfidenceLevel, 0, 1),
            _model);

        if (result.OptimizedPlan != null)
        {
            analysis.OptimizedSolution = new OptimizedSolution(
                result.OptimizedPlan.ImprovedTimeline,
                result.OptimizedPlan.ClarifiedScope,
                result.OptimizedPlan.BudgetOptimization,
                result.OptimizedPlan.ResourceStrategy,
                target);
        }

        return analysis;
    }

    private static string ToAiVerdict(double score) => score switch
    {
        >= 75 => "FEASIBLE",
        >= 55 => "RISKY_BUT_POSSIBLE",
        _ => "NEEDS_ADJUSTMENT"
    };

    private static string NormalizeImpact(string impact) =>
        impact.ToUpperInvariant() is "HIGH" or "MEDIUM" or "LOW"
            ? impact.ToUpperInvariant()
            : "MEDIUM";

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed class GroqAnalysis
    {
        public string ExecutiveSummary { get; init; } = "";
        public double ConfidenceLevel { get; init; }
        public List<string> Pros { get; init; } = new();
        public List<string> Cons { get; init; } = new();
        public List<string> Assumptions { get; init; } = new();
        public List<GroqRisk> Risks { get; init; } = new();
        public List<string> Recommendations { get; init; } = new();
        public GroqOptimizedPlan? OptimizedPlan { get; init; }
        public List<GroqRoadmapPhase> Roadmap { get; init; } = new();
    }

    private sealed class GroqRisk
    {
        public string Description { get; init; } = "";
        public string Impact { get; init; } = "MEDIUM";
        public string? Mitigation { get; init; }
    }

    private sealed class GroqOptimizedPlan
    {
        public string ImprovedTimeline { get; init; } = "";
        public string ClarifiedScope { get; init; } = "";
        public string BudgetOptimization { get; init; } = "";
        public string ResourceStrategy { get; init; } = "";
        public double TargetFeasibility { get; init; }
    }

    private sealed class GroqRoadmapPhase
    {
        public string Phase { get; init; } = "";
        public string Objective { get; init; } = "";
        public List<string> Deliverables { get; init; } = new();
        public List<string> ExitCriteria { get; init; } = new();
    }
}
