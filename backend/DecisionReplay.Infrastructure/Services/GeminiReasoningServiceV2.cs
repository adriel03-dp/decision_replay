using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Domain.Entities;
using System.Text;
using System.Text.Json;
using System.Net;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Gemini-powered Domain-Agnostic Reasoning Service
/// 
/// SOLID Principles:
/// - Single Responsibility: Only handles AI-powered decision analysis
/// - Open/Closed: Can be extended with new analysis types without modification
/// - Dependency Inversion: Implements IAIReasoningServiceV2 abstraction
/// - Liskov Substitution: Can be replaced with any other AI service implementation
/// 
/// Clean Architecture:
/// - Infrastructure layer implementation
/// - No domain logic - only AI interaction
/// - Returns domain value objects (DecisionAnalysis)
/// 
/// Design:
/// - Domain-agnostic: works with any decision type
/// - Uses DecisionContext for flexible input
/// - Produces structured DecisionAnalysis for visualization
/// - Supports replay by comparing contexts
/// </summary>
public class GeminiReasoningServiceV2 : IAIReasoningServiceV2
{
    private readonly HttpClient _httpClient;
    private readonly List<string> _apiKeys;
    private static int _currentKeyIndex = 0;
    private static readonly object _keyRotationLock = new();
    private static readonly SemaphoreSlim _rateLimiter = new(8, 8); // Increased for faster processing
    private static readonly Queue<DateTime> _requestTimes = new();
    private const int MaxRequestsPerMinute = 8; // Increased rate limit

    public GeminiReasoningServiceV2(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKeys = new List<string>();

        var key1 = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var key2 = Environment.GetEnvironmentVariable("GEMINI_API_KEY_2");

        if (!string.IsNullOrEmpty(key1)) _apiKeys.Add(key1);
        if (!string.IsNullOrEmpty(key2)) _apiKeys.Add(key2);

        Console.WriteLine($"[GEMINI INIT] Reasoning Service loaded {_apiKeys.Count} API key(s)");
    }

    public async Task<DecisionAnalysis> AnalyzeDecisionAsync(DecisionContext context, DecisionSchema? schema = null)
    {
        if (_apiKeys.Count == 0)
        {
            return CreatePlaceholderAnalysis("AI service not configured");
        }

        try
        {
            Console.WriteLine("[GEMINI] Starting decision analysis...");
            await WaitForRateLimitAsync();
            Console.WriteLine("[GEMINI] Rate limit check passed...");

            var prompt = BuildAnalysisPrompt(context, schema);
            Console.WriteLine("[GEMINI] Calling Gemini API for analysis...");
            var response = await CallGeminiApiAsync(prompt);
            Console.WriteLine($"[GEMINI] Analysis response received: {response.Substring(0, Math.Min(100, response.Length))}...");
            var analysis = ParseAnalysisResponse(response);

            return analysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Analysis failed: {ex.Message}");
            return CreatePlaceholderAnalysis($"Error: {ex.Message}");
        }
    }

    public async Task<DecisionAnalysis> ReAnalyzeDecisionAsync(
        DecisionContext originalContext,
        DecisionContext updatedContext,
        DecisionSchema? schema = null)
    {
        if (_apiKeys.Count == 0)
        {
            return CreatePlaceholderAnalysis("AI service not configured");
        }

        try
        {
            await WaitForRateLimitAsync();

            var prompt = BuildReplayAnalysisPrompt(originalContext, updatedContext, schema);
            var response = await CallGeminiApiAsync(prompt);
            var analysis = ParseAnalysisResponse(response);

            return analysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Re-analysis failed: {ex.Message}");
            return CreatePlaceholderAnalysis($"Error: {ex.Message}");
        }
    }

    public async Task<string> QueryDecisionAsync(DecisionContext context, string question)
    {
        if (_apiKeys.Count == 0)
        {
            return "AI service not configured.";
        }

        // PRODUCTION: Relevance enforcement - validate question is decision-scoped
        if (!IsQuestionRelevant(question))
        {
            return "I can only answer questions related to your current decision. " +
                   "Please ask about the decision's feasibility, risks, timeline, resources, or recommendations.";
        }

        try
        {
            await WaitForRateLimitAsync();

            var prompt = BuildQueryPrompt(context, question);
            var response = await CallGeminiApiAsync(prompt);

            return response;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// PRODUCTION: Relevance enforcement
    /// Ensures users can only ask decision-related questions
    /// Rejects general chat, off-topic queries
    /// </summary>
    private bool IsQuestionRelevant(string question)
    {
        var lowerQuestion = question.ToLower();

        // Reject obvious off-topic patterns
        var offTopicPatterns = new[]
        {
            "tell me a joke",
            "what is the weather",
            "who won",
            "latest news",
            "write a poem",
            "tell me about",
            "what do you think about",
            "personal opinion",
            "political",
            "religious",
            "how are you",
            "who are you",
            "what's your name"
        };

        foreach (var pattern in offTopicPatterns)
        {
            if (lowerQuestion.Contains(pattern))
                return false;
        }

        // Accept decision-related keywords
        var decisionKeywords = new[]
        {
            "feasible", "feasibility", "risk", "timeline", "schedule",
            "budget", "cost", "resource", "scope", "constraint",
            "recommendation", "alternative", "option", "decision",
            "outcome", "impact", "assumption", "requirement", "dependency",
            "should i", "can i", "will this", "how long", "how much",
            "what if", "why", "explain", "clarify"
        };

        foreach (var keyword in decisionKeywords)
        {
            if (lowerQuestion.Contains(keyword))
                return true;
        }

        // Default: allow if question is reasonably short and decision-related
        return question.Length > 5 && question.Length < 500;
    }

    private string BuildAnalysisPrompt(DecisionContext context, DecisionSchema? schema)
    {
        var domainType = context.GetAttribute<string>("domain") ?? "Unknown";
        var schemaInfo = schema != null
            ? $"Schema fields: {string.Join(", ", schema.Fields.Keys)}"
            : "No schema provided";

        return $@"You are an expert decision analysis consultant. Analyze this decision and provide detailed, actionable feedback.

DECISION TO ANALYZE:
Domain: {domainType}
User's Plan: {context.NaturalLanguageInput}
{schemaInfo}

Key Attributes:
{FormatAttributes(context.InferredAttributes)}

ANALYSIS STRUCTURE REQUIRED:

PART 1: CURRENT PLAN ASSESSMENT
Analyze the user's plan AS-IS and identify:
- Time constraints and timeline feasibility
- Scope definition and clarity  
- Budget allocation and sufficiency
- Resource requirements and availability
- Technical challenges and dependencies

PART 2: OPTIMIZATION RECOMMENDATIONS
Provide an improved version that makes the decision 100% successful by:
- Adjusting timeline for realistic delivery
- Clarifying scope for better execution
- Optimizing budget distribution
- Ensuring adequate resources
- Addressing technical risks

Return analysis in this EXACT JSON format:
{{
  ""feasibilityScore"": <0-100 number for CURRENT plan>,
  ""feasibilityVerdict"": ""FEASIBLE|RISKY_BUT_POSSIBLE|NEEDS_ADJUSTMENT|NOT_FEASIBLE"",
  ""executiveSummary"": ""Overall assessment in 2-3 sentences"",
  ""currentPlanAnalysis"": {{
    ""timelineAssessment"": ""Analysis of proposed timeline"",
    ""scopeAssessment"": ""Analysis of project scope"",
    ""budgetAssessment"": ""Analysis of budget allocation"",
    ""resourceAssessment"": ""Analysis of team/resource requirements""
  }},
  ""pros"": [""Current plan strengths"", ""What works well"", ""Positive aspects""],
  ""cons"": [""Current plan weaknesses"", ""Critical gaps"", ""Risk areas""],
  ""optimizedSolution"": {{
    ""improvedTimeline"": ""Realistic timeline recommendation"",
    ""clarifiedScope"": ""Refined scope definition"",
    ""budgetOptimization"": ""Better budget allocation"",
    ""resourceStrategy"": ""Optimal resource plan"",
    ""successProbability"": <0-100 improved success rate>
  }},
  ""optimizedPros"": [""Benefits of optimized approach"", ""Success enablers"", ""Competitive advantages""],
  ""optimizedCons"": [""Trade-offs in optimized plan"", ""Remaining challenges"", ""Constraints to manage""],
  ""risks"": [
    {{""description"": ""specific risk"", ""impact"": ""HIGH|MEDIUM|LOW"", ""mitigation"": ""concrete action to address""}}
  ],
  ""assumptions"": [""Key assumptions in analysis"", ""Dependencies identified""],
  ""recommendations"": [""Specific actionable steps"", ""Priority actions"", ""Success factors""],
  ""confidenceLevel"": <0.0-1.0 confidence in analysis>
}}

REQUIREMENTS:
- Be specific and actionable in all recommendations
- Focus on Time/Scope/Budget as primary decision factors
- Provide realistic success probability improvements
- Ensure the optimized solution addresses current plan weaknesses
- Make recommendations that enable 100% project success";
    }

    private string BuildReplayAnalysisPrompt(DecisionContext original, DecisionContext updated, DecisionSchema? schema)
    {
        return $@"You are analyzing changes in a decision for replay capability.

ORIGINAL CONTEXT:
{original.NaturalLanguageInput}
{FormatAttributes(original.InferredAttributes)}

UPDATED CONTEXT:
{updated.NaturalLanguageInput}
{FormatAttributes(updated.InferredAttributes)}

TASK:
Compare the two contexts and provide analysis showing:
1. What changed and how it impacts feasibility
2. New risks or opportunities
3. Updated recommendations

Return JSON in same format as standard analysis, but highlight changes in your executive summary.";
    }

    private string BuildQueryPrompt(DecisionContext context, string question)
    {
        return $@"You are a decision analysis assistant. Answer ONLY questions related to THIS specific decision.

STRICT RULES:
- Answer ONLY if the question relates to the decision's feasibility, risks, timeline, budget, scope, resources, or recommendations
- If the question is off-topic, respond: ""I can only answer questions about this decision""
- Do NOT engage in general conversation, jokes, or unrelated topics
- Keep answers focused, concise (2-4 sentences)

DECISION CONTEXT:
{context.NaturalLanguageInput}
{FormatAttributes(context.InferredAttributes)}

USER QUESTION: {question}

ANSWER (decision-scoped only):";
    }

    private string FormatAttributes(Dictionary<string, object> attributes)
    {
        var sb = new StringBuilder();
        foreach (var kvp in attributes)
        {
            sb.AppendLine($"- {kvp.Key}: {kvp.Value}");
        }
        return sb.ToString();
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        var requestBody = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                temperature = 0.3,  // Lower for more consistent JSON structure
                maxOutputTokens = 8192  // Maximum possible tokens for complete detailed analysis
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Exception? lastException = null;

        for (int attempt = 0; attempt < _apiKeys.Count; attempt++)
        {
            var apiKey = GetCurrentApiKey();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            Console.WriteLine($"[GEMINI] Attempt {attempt + 1}/{_apiKeys.Count} with key #{_currentKeyIndex + 1}");

            try
            {
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GEMINI] Full API response: {responseJson}");
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);
                    var analysisText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
                    Console.WriteLine($"[GEMINI] Extracted analysis text: {analysisText}");
                    return analysisText;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GEMINI ERROR] Key #{_currentKeyIndex + 1}: {response.StatusCode}");
                    Console.WriteLine($"[GEMINI ERROR] Response: {error.Substring(0, Math.Min(200, error.Length))}");

                    lastException = new Exception(GetUserFriendlyErrorMessage(response.StatusCode, error));

                    if (response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        RotateApiKey();
                        Console.WriteLine($"[GEMINI] Rotated to key #{_currentKeyIndex + 1}");
                    }
                    else
                    {
                        throw lastException;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GEMINI ERROR] Key #{_currentKeyIndex + 1} exception: {ex.Message}");
                lastException = ex;
                RotateApiKey();
            }
        }

        Console.WriteLine($"[GEMINI ERROR] All {_apiKeys.Count} API keys exhausted");
        throw lastException ?? new Exception("All API keys failed");
    }

    private string GetUserFriendlyErrorMessage(HttpStatusCode statusCode, string error)
    {
        return statusCode switch
        {
            HttpStatusCode.TooManyRequests => "AI analysis is temporarily unavailable due to high demand. Please try again in a few minutes.",
            HttpStatusCode.Unauthorized => "AI analysis service is not properly configured. Please contact support.",
            HttpStatusCode.BadRequest => "Invalid request to AI service. The decision content may be too complex.",
            HttpStatusCode.InternalServerError => "AI analysis service is temporarily down. Please try again later.",
            HttpStatusCode.RequestTimeout => "AI analysis took too long to complete. Please try a shorter description.",
            _ => "AI analysis is temporarily unavailable. Your decision will be saved with basic analysis."
        };
    }

    private DecisionAnalysis ParseAnalysisResponse(string response)
    {
        try
        {
            // Extract JSON
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<AnalysisDto>(jsonStr, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine($"[GEMINI] Extracted analysis text:");
                Console.WriteLine($"[GEMINI] Parsed JSON fields: FeasibilityScore={parsed?.FeasibilityScore}, Verdict={parsed?.FeasibilityVerdict}, Pros={parsed?.Pros?.Count}, Cons={parsed?.Cons?.Count}, Risks={parsed?.Risks?.Count}");

                if (parsed != null)
                {
                    return new DecisionAnalysis(
                        parsed.FeasibilityScore,
                        parsed.FeasibilityVerdict ?? "UNKNOWN",
                        parsed.ExecutiveSummary ?? "",
                        parsed.Pros ?? new List<string>(),
                        parsed.Cons ?? new List<string>(),
                        parsed.Risks?.Select(r => new RiskFactor(
                            r.Description ?? "Unknown risk",
                            r.Impact ?? "MEDIUM",
                            r.Mitigation
                        )).ToList() ?? new List<RiskFactor>(),
                        parsed.Assumptions ?? new List<string>(),
                        parsed.Recommendations ?? new List<string>(),
                        parsed.ConfidenceLevel,
                        "gemini-pro"
                    )
                    {
                        CurrentPlanAnalysis = parsed.CurrentPlanAnalysis != null ? new CurrentPlanAnalysis(
                            parsed.CurrentPlanAnalysis.TimelineAssessment ?? "",
                            parsed.CurrentPlanAnalysis.ScopeAssessment ?? "",
                            parsed.CurrentPlanAnalysis.BudgetAssessment ?? "",
                            parsed.CurrentPlanAnalysis.ResourceAssessment ?? ""
                        ) : null,
                        OptimizedSolution = parsed.OptimizedSolution != null ? new OptimizedSolution(
                            parsed.OptimizedSolution.ImprovedTimeline ?? "",
                            parsed.OptimizedSolution.ClarifiedScope ?? "",
                            parsed.OptimizedSolution.BudgetOptimization ?? "",
                            parsed.OptimizedSolution.ResourceStrategy ?? "",
                            parsed.OptimizedSolution.SuccessProbability
                        ) : null,
                        OptimizedPros = parsed.OptimizedPros ?? new List<string>(),
                        OptimizedCons = parsed.OptimizedCons ?? new List<string>()
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse analysis: {ex.Message}");
        }

        // Fallback: extract what we can
        return CreatePlaceholderAnalysis(response);
    }

    private DecisionAnalysis CreatePlaceholderAnalysis(string message)
    {
        return new DecisionAnalysis(
            50.0,
            "UNKNOWN",
            message,
            new List<string>(),
            new List<string>(),
            new List<RiskFactor>(),
            new List<string>(),
            new List<string> { "Configure AI service for full analysis" },
            0.0,
            "placeholder"
        );
    }

    private async Task WaitForRateLimitAsync()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;
            while (_requestTimes.Count > 0 && (now - _requestTimes.Peek()).TotalMinutes >= 1)
            {
                _requestTimes.Dequeue();
            }

            if (_requestTimes.Count >= MaxRequestsPerMinute)
            {
                var oldestRequest = _requestTimes.Peek();
                var waitTime = TimeSpan.FromMinutes(1) - (now - oldestRequest);
                if (waitTime.TotalMilliseconds > 0)
                {
                    await Task.Delay(waitTime);
                }
                _requestTimes.Dequeue();
            }

            _requestTimes.Enqueue(now);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private string GetCurrentApiKey()
    {
        lock (_keyRotationLock)
        {
            return _apiKeys[_currentKeyIndex];
        }
    }

    private void RotateApiKey()
    {
        lock (_keyRotationLock)
        {
            _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Count;
        }
    }

    // DTOs for deserialization
    private class AnalysisDto
    {
        public double FeasibilityScore { get; set; }
        public string? FeasibilityVerdict { get; set; }
        public string? ExecutiveSummary { get; set; }
        public CurrentPlanAnalysisDto? CurrentPlanAnalysis { get; set; }
        public List<string>? Pros { get; set; }
        public List<string>? Cons { get; set; }
        public OptimizedSolutionDto? OptimizedSolution { get; set; }
        public List<string>? OptimizedPros { get; set; }
        public List<string>? OptimizedCons { get; set; }
        public List<RiskDto>? Risks { get; set; }
        public List<string>? Assumptions { get; set; }
        public List<string>? Recommendations { get; set; }
        public double ConfidenceLevel { get; set; }
    }

    private class CurrentPlanAnalysisDto
    {
        public string? TimelineAssessment { get; set; }
        public string? ScopeAssessment { get; set; }
        public string? BudgetAssessment { get; set; }
        public string? ResourceAssessment { get; set; }
    }

    private class OptimizedSolutionDto
    {
        public string? ImprovedTimeline { get; set; }
        public string? ClarifiedScope { get; set; }
        public string? BudgetOptimization { get; set; }
        public string? ResourceStrategy { get; set; }
        public double SuccessProbability { get; set; }
    }

    private class RiskDto
    {
        public string? Description { get; set; }
        public string? Impact { get; set; }
        public string? Mitigation { get; set; }
    }

    private class GeminiResponse
    {
        public GeminiCandidate[]? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private class GeminiContent
    {
        public GeminiPart[]? Parts { get; set; }
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
    }
}
