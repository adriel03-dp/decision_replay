using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.Entities;
using Microsoft.Extensions.Http;
using System.Text;
using System.Text.Json;

namespace DecisionReplay.Infrastructure.Services;

// TEMPORARY FILE - Will refactor this into domain-agnostic reasoning service
public class GeminiReasoningServiceTemp : IAIReasoningService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private static readonly SemaphoreSlim _rateLimiter = new(5, 5);
    private static readonly Queue<DateTime> _requestTimes = new();
    private static readonly int _maxRequestsPerMinute = 5;

    public GeminiReasoningServiceTemp(IHttpClientFactory httpClientFactory)
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
            while (_requestTimes.Count > 0 && (now - _requestTimes.Peek()).TotalMinutes >= 1)
            {
                _requestTimes.Dequeue();
            }

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
            return new
            {
                DecisionId = decision.Id,
                Analysis = "AI reasoning not available. Configure GEMINI_API_KEY.",
                Confidence = 0.0,
                IsPlaceholder = true
            };
        }

        try
        {
            await WaitForRateLimitAsync();

            var prompt = BuildAnalysisPrompt(decision);
            var response = await CallGeminiApiAsync(prompt);

            return new
            {
                DecisionId = decision.Id,
                Analysis = response,
                GeneratedAt = DateTime.UtcNow,
                Model = "gemini-pro"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                DecisionId = decision.Id,
                Analysis = $"Failed to generate reasoning: {ex.Message}",
                Error = true
            };
        }
    }

    public async Task<object> AnswerDecisionQueryAsync(Decision decision, string userQuery)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return new { Response = "AI service not configured.", IsError = true };
        }

        try
        {
            await WaitForRateLimitAsync();
            var prompt = BuildQueryPrompt(decision, userQuery);
            var response = await CallGeminiApiAsync(prompt);

            return new
            {
                Response = response,
                DecisionId = decision.Id,
                Query = userQuery
            };
        }
        catch (Exception ex)
        {
            return new { Response = $"Error: {ex.Message}", IsError = true };
        }
    }

    private string BuildAnalysisPrompt(Decision decision)
    {
        return $@"Analyze this planning decision and provide feasibility assessment:

Title: {decision.Title}
Scope: {decision.Scope}
Timeline: {decision.Timeline}
Resources: {decision.Resources}
Constraints: {decision.Constraints}

Provide JSON analysis with: overallFeasibility (0-100), recommendation, analysis, riskFactors, recommendations, confidence.";
    }

    private string BuildQueryPrompt(Decision decision, string query)
    {
        return $@"Answer this question about the decision:
Title: {decision.Title}
Scope: {decision.Scope}
Timeline: {decision.Timeline}
Resources: {decision.Resources}
Constraints: {decision.Constraints}

Question: {query}

Provide a focused, professional answer.";
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
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
            throw new Exception($"Gemini API returned {response.StatusCode}: {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);
        return geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "No response";
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
