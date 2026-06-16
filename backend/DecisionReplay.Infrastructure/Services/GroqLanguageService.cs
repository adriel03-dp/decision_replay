using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DecisionReplay.Infrastructure.Services;

public sealed class GroqLanguageService : IAiLanguageService
{
    private const string AccuracyInstruction = """
        Optimize for maximum decision accuracy from the supplied information.
        Treat missing or ambiguous inputs as explicit uncertainty, not permission to invent facts.
        Use director-level judgment: concise, strategic, commercially aware, and execution-focused.
        The output should help the user improve the plan toward the strongest feasible version.
        """;

    private readonly HttpClient _httpClient;
    private readonly ILogger<GroqLanguageService> _logger;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public GroqLanguageService(
        IHttpClientFactory httpClientFactory,
        ILogger<GroqLanguageService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(GroqLanguageService));
        _httpClient.Timeout = TimeSpan.FromSeconds(45);
        _logger = logger;
        _apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        _model = Environment.GetEnvironmentVariable("GROQ_MODEL")
            ?? "llama-3.3-70b-versatile";
        _baseUrl = (Environment.GetEnvironmentVariable("GROQ_BASE_URL")
            ?? "https://api.groq.com/openai/v1").TrimEnd('/');
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<StructuredDecisionData> ExtractDecisionAsync(
        string naturalLanguageInput,
        IReadOnlyCollection<DecisionDomainTemplate> templates,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return ExtractWithFallback(naturalLanguageInput);

        var templatePayload = templates.Select(template => new
        {
            template.Domain,
            fields = template.Fields.Select(field => new
            {
                field.Name,
                field.Description,
                type = field.Type.ToString(),
                field.Required
            })
        });

        var prompt = $$"""
            {{AccuracyInstruction}}

            Extract structured fields from the user's decision.

            Supported templates:
            {{JsonSerializer.Serialize(templatePayload)}}

            User decision:
            {{naturalLanguageInput}}

            Return JSON only:
            {
              "domain": "one supported domain",
              "title": "short factual title",
              "goal": "the user's intended outcome",
              "fields": { "template_field": "value stated or safely normalized from the input" },
              "assumptions": ["only assumptions required because information was not explicit"],
              "missingFields": ["required template fields not supplied by the user"]
            }

            Restrictions:
            - Extract and normalize only.
            - Do not calculate feasibility.
            - Do not assign risk levels or factor scores.
            - Do not recommend a final decision.
            - Do not invent budgets, dates, resources, evidence, or outcomes.
            - Preserve exact user-provided numbers, dates, scope items, and constraints where available.
            - Choose the domain that best matches the user's business or project intent.
            - The title must sound like a concise board-ready plan title, not a generic label.
            - The goal must capture the intended outcome and what success would mean.
            - Level fields must be normalized to one of: none, low, medium, high, very high, ready, partial, unknown.
            - If a level is ambiguous or unstated, use unknown or omit the optional field.
            - Omit unsupported values from fields and list them in missingFields.
            - Lists must be represented as comma-separated strings in fields.
            """;

        try
        {
            var content = await SendAsync(
                "You are a director-level decision intake analyst. Extract only what the user supplied, with maximum fidelity.",
                prompt,
                jsonMode: true,
                cancellationToken);
            var parsed = JsonSerializer.Deserialize<ExtractionResponse>(
                content,
                JsonOptions());
            if (parsed == null) return ExtractWithFallback(naturalLanguageInput);

            return new StructuredDecisionData
            {
                Domain = parsed.Domain,
                Title = parsed.Title,
                Goal = parsed.Goal,
                Fields = parsed.Fields ?? new Dictionary<string, string>(),
                Assumptions = parsed.Assumptions ?? new List<string>(),
                MissingFields = parsed.MissingFields ?? new List<string>(),
                ExtractedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GROQ] Structured extraction failed; using deterministic fallback parser");
            return ExtractWithFallback(naturalLanguageInput);
        }
    }

    public async Task<string> ExplainResultAsync(
        StructuredDecisionData decision,
        FeasibilityAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return string.Empty;

        var immutableResult = new
        {
            decision.Domain,
            decision.Title,
            decision.Goal,
            assessment.FeasibilityScore,
            riskLevel = assessment.RiskLevel.ToString(),
            factors = assessment.FactorBreakdown.Select(factor => new
            {
                factor.Factor,
                factor.Score,
                factor.Weight,
                factor.WeightedScore,
                factor.Reason,
                factor.Assumption,
                confidence = factor.Confidence.ToString()
            }),
            risks = assessment.Risks,
            improvementAdvice = assessment.Recommendations
        };

        var prompt = $$"""
            {{AccuracyInstruction}}

            Write a director-level plan improvement brief from this immutable decision result:
            {{JsonSerializer.Serialize(immutableResult)}}

            Required structure:
            1. Start with the current plan grade and what it means in one direct sentence.
            2. Explain the top weaknesses holding the plan back.
            3. Translate the supplied improvement advice into specific next moves.
            4. End with what would most improve feasibility in the next version.

            Accuracy rules:
            - Preserve every numeric score, weight, risk level, and recommendation exactly.
            - Explain why the weakest factors constrain the plan.
            - Clearly label assumptions and missing evidence.
            - Do not add new scores, new risks, unsupported financial claims, or invented facts.
            - Do not guarantee success or claim the plan is 100% certain.
            - Use 3 to 5 concise paragraphs with practical, executive wording.
            """;

        try
        {
            return await SendAsync(
                "You are a director-level decision advisor. Improve user understanding without changing supplied calculations.",
                prompt,
                jsonMode: false,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GROQ] Result explanation failed");
            return string.Empty;
        }
    }

    public async Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(
        StructuredDecisionData decision,
        FeasibilityAssessment assessment,
        ActionPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return Array.Empty<PlanTaskEnhancement>();

        var immutablePlan = new
        {
            decision.Domain,
            decision.Goal,
            decision.Fields,
            score = assessment.FeasibilityScore,
            riskLevel = assessment.RiskLevel.ToString(),
            risks = assessment.Risks.Select(risk => new { risk.Factor, risk.Message, risk.Mitigation }),
            plan.TimelineMonths,
            plan.StartDate,
            plan.EndDate,
            tasks = plan.Phases.SelectMany(phase => phase.Tasks.Select(task => new
            {
                task.TaskId,
                phase = phase.PhaseName,
                phase.StartWeek,
                phase.EndWeek,
                task.TaskName,
                task.Description,
                task.SuccessCriteria
            }))
        };

        var prompt = $$"""
            {{AccuracyInstruction}}

            Upgrade the wording for this existing action plan into a director-level execution roadmap:
            {{JsonSerializer.Serialize(immutablePlan)}}

            Return JSON only:
            {
              "tasks": [
                {
                  "taskId": "existing task GUID",
                  "description": "specific practical wording",
                  "riskMitigation": "practical mitigation tied to supplied risks"
                }
              ]
            }

            Restrictions:
            - Return exactly one entry per supplied taskId.
            - Do not create, remove, reorder, or rename tasks or phases.
            - Do not change dates, weeks, effort, dependencies, scores, or success criteria.
            - Do not invent unsupported financial or market claims.
            - Each description must state the exact action, intended output, and what a strong version looks like.
            - Each riskMitigation must state the flaw it addresses and the practical control to reduce it.
            - Keep each field below 450 characters.
            """;

        try
        {
            var content = await SendAsync(
                "You are a director-level execution planning advisor. Strengthen task wording without changing plan structure.",
                prompt,
                jsonMode: true,
                cancellationToken);
            var parsed = JsonSerializer.Deserialize<PlanEnhancementResponse>(content, JsonOptions());
            return parsed?.Tasks ?? new List<PlanTaskEnhancement>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GROQ] Plan wording enhancement failed");
            return Array.Empty<PlanTaskEnhancement>();
        }
    }

    public async Task<string> SummarizeReplayAsync(
        ReplayComparison comparison,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return string.Empty;

        var prompt = $$"""
            {{AccuracyInstruction}}

            Summarize this immutable plan replay comparison in two director-level sentences:
            {{JsonSerializer.Serialize(comparison)}}

            Preserve all values. Explain whether the revised plan became stronger or weaker and what most likely drove the change.
            Do not add changes, scores, risks, recommendations, or unsupported claims.
            """;
        try
        {
            return await SendAsync(
                "You are a director-level plan improvement analyst. Summarize replay differences without changing the data.",
                prompt,
                jsonMode: false,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[GROQ] Replay summary failed");
            return string.Empty;
        }
    }

    private async Task<string> SendAsync(
        string systemPrompt,
        string userPrompt,
        bool jsonMode,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new Dictionary<string, object>
        {
            ["model"] = _model,
            ["temperature"] = .1,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };
        if (jsonMode)
            payload["response_format"] = new { type = "json_object" };

        request.Content = JsonContent.Create(payload);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Groq returned {(int)response.StatusCode}: {Truncate(body, 300)}");

        using var envelope = JsonDocument.Parse(body);
        return envelope.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?.Trim() ?? string.Empty;
    }

    private static StructuredDecisionData ExtractWithFallback(string input)
    {
        var domain = DetectDomain(input);
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ExtractNumber(input, @"(?:budget|cost|funding)[^\d]{0,20}\$?\s*([\d,]+(?:\.\d+)?)", fields, "budget");
        ExtractNumber(input, @"(\d+(?:\.\d+)?)\s*(?:months?|mos?)", fields, "timeline_months");
        ExtractNumber(input, @"(?:team|staff|people|developers?)[^\d]{0,15}(\d+)", fields, "team_size");
        ExtractNumber(input, @"(\d+(?:\.\d+)?)\s*(?:hours?|hrs?)\s*(?:per|a)?\s*week", fields, "weekly_hours");

        if (domain == "project_planning") fields["project_goal"] = input.Trim();
        if (domain == "product_launch") fields["product_goal"] = input.Trim();
        if (domain == "business_startup") fields["business_idea"] = input.Trim();
        if (domain == "education_path") fields["program"] = input.Trim();

        return new StructuredDecisionData
        {
            Domain = domain,
            Title = input.ReplaceLineEndings(" ").Trim()[..Math.Min(80, input.Trim().Length)],
            Goal = input.Trim(),
            Fields = fields,
            Assumptions = new List<string>
            {
                "AI extraction was unavailable; only explicit numeric values and the stated goal were parsed."
            }
        };
    }

    private static void ExtractNumber(
        string input,
        string pattern,
        IDictionary<string, string> fields,
        string field)
    {
        var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
        if (match.Success) fields[field] = match.Groups[1].Value.Replace(",", "");
    }

    private static string DetectDomain(string input)
    {
        var text = input.ToLowerInvariant();
        if (Regex.IsMatch(text, @"\b(startup|start a business|new business|company)\b"))
            return "business_startup";
        if (Regex.IsMatch(text, @"\b(career|job|role|resign|promotion)\b"))
            return "career_decision";
        if (Regex.IsMatch(text, @"\b(degree|course|university|study|education)\b"))
            return "education_path";
        if (Regex.IsMatch(text, @"\b(product launch|launch|mvp|customers?)\b"))
            return "product_launch";
        return "project_planning";
    }

    private static JsonSerializerOptions JsonOptions() =>
        new() { PropertyNameCaseInsensitive = true };

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];

    private sealed class ExtractionResponse
    {
        public string Domain { get; init; } = "project_planning";
        public string Title { get; init; } = string.Empty;
        public string Goal { get; init; } = string.Empty;
        public Dictionary<string, string>? Fields { get; init; }
        public List<string>? Assumptions { get; init; }
        public List<string>? MissingFields { get; init; }
    }

    private sealed class PlanEnhancementResponse
    {
        public List<PlanTaskEnhancement> Tasks { get; init; } = new();
    }
}
