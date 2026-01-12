using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Gemini-powered Intent Parser
/// 
/// SOLID Principles:
/// - Single Responsibility: Only handles intent parsing and schema generation
/// - Dependency Inversion: Implements IIntentParser interface
/// - Open/Closed: Can be extended or replaced without changing consumers
/// 
/// Design:
/// Uses Gemini AI to analyze natural language and infer:
/// - Decision domain type
/// - Key attributes (timeline, resources, constraints, etc.)
/// - Implicit assumptions
/// - Appropriate schema structure
/// 
/// This makes the system truly domain-agnostic.
/// </summary>
public class GeminiIntentParser : IIntentParser
{
    private readonly HttpClient _httpClient;
    private readonly List<string> _apiKeys;
    private static int _globalCurrentKeyIndex = 0;  // Shared across all instances
    private static readonly object _globalKeyRotationLock = new();  // Shared lock

    public GeminiIntentParser(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(25); // Faster timeout for quicker feedback
        _apiKeys = new List<string>();

        var key1 = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var key2 = Environment.GetEnvironmentVariable("GEMINI_API_KEY_2");
        var key3 = Environment.GetEnvironmentVariable("GEMINI_API_KEY_3");

        if (!string.IsNullOrEmpty(key1)) _apiKeys.Add(key1);
        if (!string.IsNullOrEmpty(key2)) _apiKeys.Add(key2);
        if (!string.IsNullOrEmpty(key3)) _apiKeys.Add(key3);

        Console.WriteLine($"[GEMINI INIT] Intent Parser loaded {_apiKeys.Count} API key(s)");
    }

    public async Task<DecisionContext> ParseInputAsync(string naturalLanguageInput, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageInput))
            throw new ArgumentException("Input cannot be empty", nameof(naturalLanguageInput));

        var inferredAttributes = new Dictionary<string, object>();

        if (_apiKeys.Count == 0)
        {
            // Fallback: Basic parsing without AI
            inferredAttributes["domain"] = "Unknown";
            inferredAttributes["rawInput"] = naturalLanguageInput;
            inferredAttributes["parsedWithAI"] = false;

            return new DecisionContext(naturalLanguageInput, inferredAttributes);
        }

        try
        {
            Console.WriteLine($"[GEMINI] Starting intent parsing for input: {naturalLanguageInput.Substring(0, Math.Min(50, naturalLanguageInput.Length))}...");
            var prompt = BuildIntentParsingPrompt(naturalLanguageInput);
            Console.WriteLine("[GEMINI] Calling Gemini API for intent parsing...");
            var response = await CallGeminiApiAsync(prompt, cancellationToken);
            Console.WriteLine($"[GEMINI] Received response: {response.Substring(0, Math.Min(100, response.Length))}...");
            var parsed = ParseGeminiResponse(response);

            foreach (var kvp in parsed)
            {
                inferredAttributes[kvp.Key] = kvp.Value;
            }

            inferredAttributes["parsedWithAI"] = true;
            inferredAttributes["userId"] = userId;

            return new DecisionContext(naturalLanguageInput, inferredAttributes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GEMINI] Intent parsing failed: {ex.Message}. Using fallback.");

            // Fallback
            inferredAttributes["domain"] = "Unknown";
            inferredAttributes["rawInput"] = naturalLanguageInput;
            inferredAttributes["parsedWithAI"] = false;
            inferredAttributes["error"] = ex.Message;

            return new DecisionContext(naturalLanguageInput, inferredAttributes);
        }
    }

    public async Task<DecisionSchema> GenerateSchemaAsync(DecisionContext context, CancellationToken cancellationToken = default)
    {
        var domainType = context.GetAttribute<string>("domain") ?? "Unknown";

        var fields = new Dictionary<string, string>();

        if (_apiKeys.Count == 0)
        {
            // Fallback schema
            fields["input"] = "Original user input";
            fields["intent"] = "What the user wants to decide";
            return new DecisionSchema(domainType, fields);
        }

        try
        {
            var prompt = BuildSchemaGenerationPrompt(context);
            var response = await CallGeminiApiAsync(prompt, cancellationToken);
            var parsedFields = ParseSchemaFields(response);

            foreach (var kvp in parsedFields)
            {
                fields[kvp.Key] = kvp.Value;
            }

            return new DecisionSchema(domainType, fields);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Schema generation failed: {ex.Message}. Using fallback.");
            fields["input"] = "Original user input";
            fields["error"] = ex.Message;
            return new DecisionSchema(domainType, fields);
        }
    }

    private string BuildIntentParsingPrompt(string input)
    {
        return $@"Extract decision factors from this input. Return ONLY valid JSON (no markdown):

Input: ""{input}""

JSON format:
{{
  ""domain"": ""<DomainType>"",
  ""intent"": ""<What user wants to decide>"",
  ""timeline"": ""<Duration or deadline if mentioned>"",
  ""budget"": ""<Budget if mentioned>"",
  ""resources"": ""<Team size or resources if mentioned>"",
  ""scope"": ""<Main objectives/deliverables>"",
  ""constraints"": [""<Key limitations>""],
  ""risks"": [""<Potential challenges>""]
}}

Use ""Not specified"" for missing info.";
    }

    private string BuildSchemaGenerationPrompt(DecisionContext context)
    {
        var domain = context.GetAttribute<string>("domain") ?? "Unknown";
        return $@"Generate a decision schema for domain: {domain}

Return JSON with fields relevant to this domain:
{{
  ""field1"": ""Description of what this field represents"",
  ""field2"": ""Description"",
  ...
}}

For example, for SoftwareDevelopment: timeline, scope, resources, technical_constraints, etc.
For Construction: budget, materials, workforce, permits, timeline, etc.
For Logistics: route, capacity, delivery_windows, inventory_levels, etc.

Provide 5-8 relevant fields for {domain}.";
    }

    private async Task<string> CallGeminiApiAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 1024
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Exception? lastException = null;

        for (int attempt = 0; attempt < _apiKeys.Count; attempt++)
        {
            var apiKey = GetCurrentApiKey();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            Console.WriteLine($"[GEMINI] Attempt {attempt + 1}/{_apiKeys.Count} with key #{_globalCurrentKeyIndex + 1}");

            try
            {
                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                Console.WriteLine($"[GEMINI] Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);
                    return geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GEMINI ERROR] Key #{_globalCurrentKeyIndex + 1}: {response.StatusCode}");
                    Console.WriteLine($"[GEMINI ERROR] Response: {error.Substring(0, Math.Min(200, error.Length))}");

                    lastException = new Exception(GetUserFriendlyErrorMessage(response.StatusCode, error));

                    // Always rotate key on error to try next one  
                    if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode == HttpStatusCode.Unauthorized ||
                        response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                        response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        RotateApiKey();
                        Console.WriteLine($"[GEMINI] Rotated to key #{_globalCurrentKeyIndex + 1} due to {response.StatusCode}");
                        continue; // Try next key
                    }
                    else
                    {
                        throw lastException;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GEMINI ERROR] Key #{_globalCurrentKeyIndex + 1} exception: {ex.Message}");
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

    private Dictionary<string, object> ParseGeminiResponse(string response)
    {
        var result = new Dictionary<string, object>();

        try
        {
            // Extract JSON from response (Gemini sometimes includes markdown)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonStr);

                if (parsed != null)
                {
                    foreach (var kvp in parsed)
                    {
                        result[kvp.Key] = ParseJsonElement(kvp.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse Gemini response: {ex.Message}");
            result["rawResponse"] = response;
        }

        return result;
    }

    private object ParseJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ParseJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ParseJsonElement(p.Value)),
            _ => element.ToString()
        };
    }

    private Dictionary<string, string> ParseSchemaFields(string response)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonStr);

                if (parsed != null)
                {
                    return parsed;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse schema fields: {ex.Message}");
        }

        return result;
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
