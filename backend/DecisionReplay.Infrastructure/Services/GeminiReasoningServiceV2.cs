using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Domain.Entities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static int _globalCurrentKeyIndex = 0;  // Shared across all instances
    private static readonly object _globalKeyRotationLock = new();  // Shared lock
    private readonly SemaphoreSlim _rateLimiter = new(8, 8); // Instance-based rate limiter
    // Limit concurrent requests across all service instances to avoid hammering the API
    private static readonly SemaphoreSlim _globalConcurrencyLimiter = new SemaphoreSlim(4, 4);
    private readonly List<KeyState> _apiKeyStates = new();
    private readonly Queue<DateTime> _requestTimes = new(); // Instance-based request tracking
    private const int MaxRequestsPerMinute = 15; // Match API key limit

    public GeminiReasoningServiceV2(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(45); // Increased for detailed analysis
        _apiKeys = new List<string>();

        var key1 = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var key2 = Environment.GetEnvironmentVariable("GEMINI_API_KEY_2");
        var key3 = Environment.GetEnvironmentVariable("GEMINI_API_KEY_3");

        if (!string.IsNullOrEmpty(key1)) _apiKeys.Add(key1);
        if (!string.IsNullOrEmpty(key2)) _apiKeys.Add(key2);
        if (!string.IsNullOrEmpty(key3)) _apiKeys.Add(key3);

        // Initialize per-key state
        foreach (var k in _apiKeys)
        {
            _apiKeyStates.Add(new KeyState { ApiKey = k });
        }

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

        return $@"You are an expert decision analysis consultant. Analyze this decision and provide comprehensive, actionable feedback to ensure 100% success.

DECISION TO ANALYZE:
{context.NaturalLanguageInput}

ANALYSIS REQUIREMENTS:
1. Assess current plan feasibility (timeline, scope, budget, resources)
2. Identify all risks and provide specific mitigation strategies  
3. Create optimized plan that maximizes success probability
4. Provide detailed, actionable recommendations

REQUIRED JSON OUTPUT:
{{
  ""feasibilityScore"": <0-100 for current plan>,
  ""feasibilityVerdict"": ""FEASIBLE|RISKY_BUT_POSSIBLE|NEEDS_ADJUSTMENT|NOT_FEASIBLE"",
  ""executiveSummary"": ""Comprehensive assessment with key insights and success factors"",
  ""currentPlanAnalysis"": {{
    ""timelineAssessment"": ""Detailed analysis of proposed timeline with specific concerns"",
    ""scopeAssessment"": ""Thorough scope analysis with clarity and completeness evaluation"",
    ""budgetAssessment"": ""Complete budget analysis with allocation recommendations"",
    ""resourceAssessment"": ""Detailed resource analysis including skills, capacity, and gaps""
  }},
  ""pros"": [""Specific strengths of current approach"", ""Market advantages"", ""Resource benefits""],
  ""cons"": [""Critical weaknesses requiring attention"", ""High-risk areas"", ""Resource constraints""],
  ""optimizedSolution"": {{
    ""improvedTimeline"": ""Realistic timeline with specific milestones and buffer time"",
    ""clarifiedScope"": ""Refined scope with clear deliverables and success criteria"",
    ""budgetOptimization"": ""Optimized budget allocation with contingency planning"",
    ""resourceStrategy"": ""Comprehensive resource plan including hiring and skill development"",
    ""successProbability"": <0-100 improved success rate>
  }},
  ""optimizedPros"": [""Benefits of optimized approach"", ""Competitive advantages"", ""Risk mitigation benefits""],
  ""optimizedCons"": [""Trade-offs in optimized plan"", ""Additional complexity"", ""Resource requirements""],
  ""risks"": [
    {{""description"": ""Specific risk with context"", ""impact"": ""HIGH|MEDIUM|LOW"", ""mitigation"": ""Detailed action plan to address risk""}}
  ],
  ""assumptions"": [""Critical assumptions with validation needs"", ""Key dependencies""],
  ""recommendations"": [""Immediate priority actions"", ""Strategic next steps"", ""Success enablement factors""],
  ""confidenceLevel"": <0.0-1.0 confidence in analysis>
}}

FOCUS ON:
- Specific, actionable guidance (not generic advice)
- Real timeline and resource constraints
- Practical risk mitigation strategies
- Clear path to 100% success probability";
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
                temperature = 0.2,  // Lower for more consistent, focused analysis
                maxOutputTokens = 8192,  // Maximum for detailed analysis
                topP = 0.8,
                topK = 40
            },
            safetySettings = new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Exception? lastException = null;

        // Limit concurrency across instances
        await _globalConcurrencyLimiter.WaitAsync();
        try
        {
            // Try up to number of keys attempts but pick only available keys (not in cooldown)
            for (int attempt = 0; attempt < _apiKeys.Count; attempt++)
            {
                var keyIndex = await GetAvailableKeyIndexAsync();
                if (keyIndex < 0)
                {
                    // No key available right now - wait a short randomized interval then retry
                    var waitMs = 250 + (new Random()).Next(0, 500);
                    Console.WriteLine($"[GEMINI] No available API key, waiting {waitMs}ms before retry");
                    await Task.Delay(waitMs);
                    continue;
                }

                var apiKey = _apiKeys[keyIndex];
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                Console.WriteLine($"[GEMINI] Attempt {attempt + 1}/{_apiKeys.Count} with key #{keyIndex + 1}");

                try
                {
                    // Backoff with jitter between retries
                    if (attempt > 0)
                    {
                        var baseDelay = 200 * Math.Pow(2, attempt - 1);
                        var jitter = new Random().Next(0, 200);
                        var delay = TimeSpan.FromMilliseconds(baseDelay + jitter);
                        await Task.Delay(delay);
                        Console.WriteLine($"[GEMINI] Waited {delay.TotalMilliseconds}ms before retry");
                    }

                    // Create fresh content per attempt (do not reuse HttpContent across requests)
                    using var contentLocal = new StringContent(json, Encoding.UTF8, "application/json");

                    // Record that we're attempting a request with this key (counts toward per-minute quota)
                    RecordKeyRequest(keyIndex);

                    // Log masked key to verify which key is used (do not log full key)
                    var masked = apiKey != null && apiKey.Length > 6 ? $"****{apiKey.Substring(apiKey.Length - 6)}" : apiKey;
                    Console.WriteLine($"[GEMINI] Sending request with key {masked}, payload size={contentLocal.Headers.ContentLength}");

                    var response = await _httpClient.PostAsync(url, contentLocal);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[GEMINI] Full API response: {responseJson}");
                        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);
                        var analysisText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
                        Console.WriteLine($"[GEMINI] Extracted analysis text: {analysisText}");
                        // Mark key as healthy and record this request
                        MarkKeyHealthy(keyIndex);
                        RecordKeyRequest(keyIndex);
                        return analysisText;
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[GEMINI ERROR] Key #{keyIndex + 1}: {response.StatusCode}");
                        Console.WriteLine($"[GEMINI ERROR] Response: {error.Substring(0, Math.Min(200, error.Length))}");

                        lastException = new Exception(GetUserFriendlyErrorMessage(response.StatusCode, error));

                        if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                            response.StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            // Put key into cooldown with exponential backoff
                            SetKeyCooldown(keyIndex);
                            Console.WriteLine($"[GEMINI] Key #{keyIndex + 1} placed into cooldown");
                            continue; // Try next available key
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            // Unauthorized - mark key dead for a long period
                            SetKeyCooldown(keyIndex, TimeSpan.FromHours(1));
                            Console.WriteLine($"[GEMINI] Key #{keyIndex + 1} unauthorized - disabled temporarily");
                            continue;
                        }
                        else
                        {
                            throw lastException;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GEMINI ERROR] Key #{keyIndex + 1} exception: {ex.Message}");
                    lastException = ex;
                    SetKeyCooldown(keyIndex);
                }
            }

            Console.WriteLine($"[GEMINI ERROR] All {_apiKeys.Count} API keys exhausted or temporarily unavailable");
            throw lastException ?? new Exception("All API keys failed");
        }
        finally
        {
            _globalConcurrencyLimiter.Release();
        }
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
            // Clean the response - remove any markdown formatting
            var cleanResponse = response.Replace("```json", "").Replace("```", "").Trim();

            // Extract JSON more reliably
            var jsonStart = cleanResponse.IndexOf('{');
            var jsonEnd = cleanResponse.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = cleanResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                Console.WriteLine($"[GEMINI] Attempting to parse JSON: {jsonStr.Substring(0, Math.Min(200, jsonStr.Length))}...");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var parsed = JsonSerializer.Deserialize<AnalysisDto>(jsonStr, options);

                if (parsed != null)
                {
                    Console.WriteLine($"[GEMINI] Successfully parsed: Score={parsed.FeasibilityScore}, Pros={parsed.Pros?.Count ?? 0}, Risks={parsed.Risks?.Count ?? 0}");

                    return new DecisionAnalysis(
                        Math.Max(0, Math.Min(100, parsed.FeasibilityScore)), // Clamp to 0-100
                        parsed.FeasibilityVerdict ?? "NEEDS_ASSESSMENT",
                        parsed.ExecutiveSummary ?? "Analysis completed successfully.",
                        parsed.Pros ?? new List<string>(),
                        parsed.Cons ?? new List<string>(),
                        parsed.Risks?.Select(r => new RiskFactor(
                            r.Description ?? "Risk identified",
                            ValidateImpact(r.Impact),
                            r.Mitigation ?? "Mitigation strategy needed"
                        )).ToList() ?? new List<RiskFactor>(),
                        parsed.Assumptions ?? new List<string>(),
                        parsed.Recommendations ?? new List<string>(),
                        Math.Max(0.0, Math.Min(1.0, parsed.ConfidenceLevel)), // Clamp to 0-1
                        "gemini-2.5-flash"
                    )
                    {
                        CurrentPlanAnalysis = parsed.CurrentPlanAnalysis != null ? new CurrentPlanAnalysis(
                            parsed.CurrentPlanAnalysis.TimelineAssessment ?? "Timeline analysis pending",
                            parsed.CurrentPlanAnalysis.ScopeAssessment ?? "Scope analysis pending",
                            parsed.CurrentPlanAnalysis.BudgetAssessment ?? "Budget analysis pending",
                            parsed.CurrentPlanAnalysis.ResourceAssessment ?? "Resource analysis pending"
                        ) : null,
                        OptimizedSolution = parsed.OptimizedSolution != null ? new OptimizedSolution(
                            parsed.OptimizedSolution.ImprovedTimeline ?? "Timeline optimization recommended",
                            parsed.OptimizedSolution.ClarifiedScope ?? "Scope clarification recommended",
                            parsed.OptimizedSolution.BudgetOptimization ?? "Budget optimization recommended",
                            parsed.OptimizedSolution.ResourceStrategy ?? "Resource strategy optimization recommended",
                            Math.Max(0, Math.Min(100, parsed.OptimizedSolution.SuccessProbability))
                        ) : null,
                        OptimizedPros = parsed.OptimizedPros ?? new List<string>(),
                        OptimizedCons = parsed.OptimizedCons ?? new List<string>()
                    };
                }
            }

            Console.WriteLine($"[GEMINI] No valid JSON structure found in response");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[GEMINI] JSON parsing failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GEMINI] Analysis parsing failed: {ex.Message}");
        }

        // Enhanced fallback with more helpful message
        return CreatePlaceholderAnalysis("Analysis completed but response formatting needs improvement. Core insights may be available in executive summary.");
    }

    private string ValidateImpact(string? impact)
    {
        var validImpacts = new[] { "HIGH", "MEDIUM", "LOW" };
        return validImpacts.Contains(impact?.ToUpper()) ? impact.ToUpper() : "MEDIUM";
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
        lock (_globalKeyRotationLock)
        {
            return _apiKeys[_globalCurrentKeyIndex];
        }
    }

    private void RotateApiKey()
    {
        lock (_globalKeyRotationLock)
        {
            _globalCurrentKeyIndex = (_globalCurrentKeyIndex + 1) % _apiKeys.Count;
        }
    }

    private async Task<int> GetAvailableKeyIndexAsync()
    {
        lock (_globalKeyRotationLock)
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < _apiKeyStates.Count; i++)
            {
                var idx = (_globalCurrentKeyIndex + i) % _apiKeyStates.Count;
                // purge requests older than 1 minute for this key
                while (_apiKeyStates[idx].RecentRequests.Count > 0 && (now - _apiKeyStates[idx].RecentRequests.Peek()).TotalMinutes >= 1)
                {
                    _apiKeyStates[idx].RecentRequests.Dequeue();
                }

                if (_apiKeyStates[idx].AvailableAtUtc <= now && _apiKeyStates[idx].RecentRequests.Count < MaxRequestsPerMinute)
                {
                    _globalCurrentKeyIndex = idx; // start next search from this key
                    return idx;
                }
            }
        }

        return -1; // none available
    }

    private void SetKeyCooldown(int index, TimeSpan? overrideCooldown = null)
    {
        lock (_globalKeyRotationLock)
        {
            var state = _apiKeyStates[index];
            state.FailureCount++;
            // exponential backoff base 1s, capped at 5 minutes
            var backoffMs = Math.Min(300000, (int)(1000 * Math.Pow(2, Math.Min(6, state.FailureCount))));
            var cooldown = overrideCooldown ?? TimeSpan.FromMilliseconds(backoffMs);
            // add small jitter
            var jitter = new Random().Next(0, 500);
            state.AvailableAtUtc = DateTime.UtcNow.AddMilliseconds(cooldown.TotalMilliseconds + jitter);
        }
    }

    private void MarkKeyHealthy(int index)
    {
        lock (_globalKeyRotationLock)
        {
            var state = _apiKeyStates[index];
            state.FailureCount = 0;
            state.AvailableAtUtc = DateTime.UtcNow;
        }
    }

    private void RecordKeyRequest(int index)
    {
        lock (_globalKeyRotationLock)
        {
            var state = _apiKeyStates[index];
            var now = DateTime.UtcNow;
            while (state.RecentRequests.Count > 0 && (now - state.RecentRequests.Peek()).TotalMinutes >= 1)
            {
                state.RecentRequests.Dequeue();
            }
            state.RecentRequests.Enqueue(now);
        }
    }

    private class KeyState
    {
        public string? ApiKey { get; set; }
        public DateTime AvailableAtUtc { get; set; } = DateTime.MinValue;
        public int FailureCount { get; set; } = 0;
        public Queue<DateTime> RecentRequests { get; set; } = new Queue<DateTime>();
    }

    // DTOs for deserialization
    private class AnalysisDto
    {
        [JsonPropertyName("feasibilityScore")]
        public double FeasibilityScore { get; set; }

        [JsonPropertyName("feasibilityVerdict")]
        public string? FeasibilityVerdict { get; set; }

        [JsonPropertyName("executiveSummary")]
        public string? ExecutiveSummary { get; set; }

        [JsonPropertyName("currentPlanAnalysis")]
        public CurrentPlanAnalysisDto? CurrentPlanAnalysis { get; set; }

        [JsonPropertyName("pros")]
        public List<string>? Pros { get; set; }

        [JsonPropertyName("cons")]
        public List<string>? Cons { get; set; }

        [JsonPropertyName("optimizedSolution")]
        public OptimizedSolutionDto? OptimizedSolution { get; set; }

        [JsonPropertyName("optimizedPros")]
        public List<string>? OptimizedPros { get; set; }

        [JsonPropertyName("optimizedCons")]
        public List<string>? OptimizedCons { get; set; }

        [JsonPropertyName("risks")]
        public List<RiskDto>? Risks { get; set; }

        [JsonPropertyName("assumptions")]
        public List<string>? Assumptions { get; set; }

        [JsonPropertyName("recommendations")]
        public List<string>? Recommendations { get; set; }

        [JsonPropertyName("confidenceLevel")]
        public double ConfidenceLevel { get; set; }
    }

    private class CurrentPlanAnalysisDto
    {
        [JsonPropertyName("timelineAssessment")]
        public string? TimelineAssessment { get; set; }

        [JsonPropertyName("scopeAssessment")]
        public string? ScopeAssessment { get; set; }

        [JsonPropertyName("budgetAssessment")]
        public string? BudgetAssessment { get; set; }

        [JsonPropertyName("resourceAssessment")]
        public string? ResourceAssessment { get; set; }
    }

    private class OptimizedSolutionDto
    {
        [JsonPropertyName("improvedTimeline")]
        public string? ImprovedTimeline { get; set; }

        [JsonPropertyName("clarifiedScope")]
        public string? ClarifiedScope { get; set; }

        [JsonPropertyName("budgetOptimization")]
        public string? BudgetOptimization { get; set; }

        [JsonPropertyName("resourceStrategy")]
        public string? ResourceStrategy { get; set; }

        [JsonPropertyName("successProbability")]
        public double SuccessProbability { get; set; }
    }

    private class RiskDto
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("impact")]
        public string? Impact { get; set; }

        [JsonPropertyName("mitigation")]
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
