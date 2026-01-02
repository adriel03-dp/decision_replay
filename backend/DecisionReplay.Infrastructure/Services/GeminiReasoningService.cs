using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using Microsoft.Extensions.Http;
using System.Text;
using System.Text.Json;

namespace DecisionReplay.Infrastructure.Services;

public class GeminiReasoningService : IAIReasoningService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private static readonly SemaphoreSlim _rateLimiter = new(5, 5); // 5 concurrent requests max
    private static readonly Queue<DateTime> _requestTimes = new();
    private static readonly int _maxRequestsPerMinute = 5;

    public GeminiReasoningService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    private async Task WaitForRateLimitAsync()
    {
        await _rateLimiter.WaitAsync();
        try
        {
            var now = DateTime.UtcNow;

            // Remove requests older than 1 minute
            while (_requestTimes.Count > 0 && (now - _requestTimes.Peek()).TotalMinutes >= 1)
            {
                _requestTimes.Dequeue();
            }

            // If we've hit the limit, wait
            if (_requestTimes.Count >= _maxRequestsPerMinute)
            {
                var oldestRequest = _requestTimes.Peek();
                var waitTime = TimeSpan.FromMinutes(1) - (now - oldestRequest);
                if (waitTime.TotalMilliseconds > 0)
                {
                    Console.WriteLine($"Rate limit reached. Waiting {waitTime.TotalSeconds:F0} seconds...");
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

    public async Task<object> GenerateReasoningAsync(Decision decision)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            Console.WriteLine("Gemini API key not configured. Returning placeholder.");
            return new
            {
                DecisionId = decision.Id,
                Analysis = "AI reasoning is not available. Please configure the GEMINI_API_KEY environment variable.",
                Recommendation = "Configure AI service to enable reasoning",
                Confidence = 0.0,
                IsPlaceholder = true
            };
        }

        try
        {
            await WaitForRateLimitAsync();

            var systemPrompt = @"You are an expert planning feasibility analyst for project and product decision-makers.

Your role is to analyze planning decisions (project plans, product launches, resource allocations) and provide honest, actionable feasibility assessments.

STRICT GUIDELINES:
1. ONLY analyze planning decisions - scope, timelines, resources, constraints
2. Be brutally honest about feasibility - identify unrealistic timelines, resource gaps, and constraint conflicts
3. Provide specific, actionable recommendations (e.g., 'Add 2 developers' or 'Extend timeline by 4 weeks')
4. Refuse off-topic questions politely: 'I only analyze planning decisions. Please ask about feasibility, risks, or resource gaps.'
5. Never make up data - only analyze what's provided

Your analysis should help decision-makers understand:
- Is this plan realistic?
- What resources are missing?
- What could go wrong?
- How to improve the plan";

            var taskDescription = "Provide a comprehensive feasibility analysis with overallFeasibility score (0-100), recommendation, analysis, riskFactors, recommendations, and confidence level.";

            var prompt = $@"{systemPrompt}

PLANNING DECISION TO ANALYZE:
Title: {decision.Title}
Scope: {decision.Scope}
Timeline: {decision.Timeline}
Resources: {decision.Resources}
Constraints: {decision.Constraints}
Current Status: {decision.Status}
Current Outcome: {decision.Outcome}
Feasibility Score: {decision.FeasibilityScore}%
Created: {decision.CreatedAt}
Created By: {decision.CreatedBy}

TASK:
{taskDescription}
Be specific and honest. If the plan is unrealistic, say so clearly with concrete alternatives.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}";

            Console.WriteLine($"Calling Gemini API for decision {decision.Id}...");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini API error: {response.StatusCode} - {error}");
                throw new Exception($"Gemini API returned {response.StatusCode}: {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            var generatedText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "No response generated";

            Console.WriteLine($"Gemini API response received for decision {decision.Id}");

            return new
            {
                DecisionId = decision.Id,
                Analysis = generatedText,
                Recommendation = "See analysis for details",
                Confidence = 0.85,
                GeneratedAt = DateTime.UtcNow,
                Model = "gemini-pro"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating reasoning: {ex.Message}");
            return new
            {
                DecisionId = decision.Id,
                Analysis = $"Failed to generate AI reasoning: {ex.Message}",
                Recommendation = "Manual review recommended",
                Confidence = 0.0,
                Error = true
            };
        }
    }

    public async Task<object> AnswerDecisionQueryAsync(Decision decision, string userQuery)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return new
            {
                Response = "AI service is not configured.",
                IsError = true
            };
        }

        // Validate query is planning decision-related
        var offTopicKeywords = new[] {
            "write code", "create app", "recipe", "weather", "joke", "story",
            "movie", "game", "sports", "news", "celebrity", "song", "math homework"
        };

        if (offTopicKeywords.Any(keyword => userQuery.ToLower().Contains(keyword)))
        {
            return new
            {
                Response = "I only analyze planning decisions (feasibility, resources, timelines, risks). Please ask about this specific project plan.",
                IsOffTopic = true
            };
        }

        try
        {
            await WaitForRateLimitAsync();

            var systemPrompt = @"You are a planning feasibility analyst. Answer questions ONLY about the specific planning decision provided below.

STRICT RULES:
1. ONLY discuss this planning decision - no general advice, no off-topic content
2. If asked unrelated questions, respond: 'I only answer questions about this planning decision. Ask about feasibility, resources, timeline, risks, or scope.'
3. Focus on: feasibility assessment, resource gaps, timeline realism, constraint conflicts, risk mitigation
4. Be specific and actionable - reference actual data from the plan
5. Never generate code, tell stories, or discuss unrelated topics";

            var prompt = $@"{systemPrompt}

PLANNING DECISION CONTEXT:
Title: {decision.Title}
Scope: {decision.Scope}
Timeline: {decision.Timeline}
Resources: {decision.Resources}
Constraints: {decision.Constraints}
Status: {decision.Status}
Outcome: {decision.Outcome}
Feasibility Score: {decision.FeasibilityScore}%
Created: {decision.CreatedAt}
Created By: {decision.CreatedBy}

USER QUESTION: {userQuery}

Provide a focused, professional answer based ONLY on the planning decision above. If the question is unrelated to this plan, politely refuse.

Guidelines:
- Reference specific plan details (timeline, resources, scope) in your answer
- Be honest about feasibility issues
- Provide actionable suggestions
- Keep responses concise (2-4 sentences)";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Gemini API error: {response.StatusCode} - {error}");
                throw new Exception($"Gemini API returned {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            var generatedText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "No response generated";

            return new
            {
                Response = generatedText,
                DecisionId = decision.Id,
                Query = userQuery,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error answering query: {ex.Message}");
            return new
            {
                Response = $"Failed to process query: {ex.Message}",
                IsError = true
            };
        }
    }

    // Response models for JSON deserialization
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
