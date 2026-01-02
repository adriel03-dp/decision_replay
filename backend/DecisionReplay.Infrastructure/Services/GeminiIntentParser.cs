using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using System.Text;
using System.Text.Json;

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
    private readonly string? _apiKey;

    public GeminiIntentParser(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    }

    public async Task<DecisionContext> ParseInputAsync(string naturalLanguageInput, string userId)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageInput))
            throw new ArgumentException("Input cannot be empty", nameof(naturalLanguageInput));

        var inferredAttributes = new Dictionary<string, object>();

        if (string.IsNullOrEmpty(_apiKey))
        {
            // Fallback: Basic parsing without AI
            inferredAttributes["domain"] = "Unknown";
            inferredAttributes["rawInput"] = naturalLanguageInput;
            inferredAttributes["parsedWithAI"] = false;

            return new DecisionContext(naturalLanguageInput, inferredAttributes);
        }

        try
        {
            var prompt = BuildIntentParsingPrompt(naturalLanguageInput);
            var response = await CallGeminiApiAsync(prompt);
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
            Console.WriteLine($"Intent parsing failed: {ex.Message}. Using fallback.");

            // Fallback
            inferredAttributes["domain"] = "Unknown";
            inferredAttributes["rawInput"] = naturalLanguageInput;
            inferredAttributes["parsedWithAI"] = false;
            inferredAttributes["error"] = ex.Message;

            return new DecisionContext(naturalLanguageInput, inferredAttributes);
        }
    }

    public async Task<DecisionSchema> GenerateSchemaAsync(DecisionContext context)
    {
        var domainType = context.GetAttribute<string>("domain") ?? "Unknown";

        var fields = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(_apiKey))
        {
            // Fallback schema
            fields["input"] = "Original user input";
            fields["intent"] = "What the user wants to decide";
            return new DecisionSchema(domainType, fields);
        }

        try
        {
            var prompt = BuildSchemaGenerationPrompt(context);
            var response = await CallGeminiApiAsync(prompt);
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
        return $@"You are an intent parser for a decision analysis system. 
Analyze this user input and extract key information in JSON format:

User Input: ""{input}""

Extract and return JSON with:
{{
  ""domain"": ""<DomainType>"",  // e.g., SoftwareDevelopment, Construction, Logistics, PersonalPlanning
  ""intent"": ""<What user wants to decide>"",
  ""timeline"": ""<Any time-related info>"",
  ""resources"": ""<Team, budget, tools mentioned>"",
  ""constraints"": ""<Limitations, risks, dependencies>"",
  ""assumptions"": [""<List of implicit assumptions>""],
  ""keywords"": [""<Important keywords>""]
}}

Only extract what's explicitly mentioned or clearly implied. Use ""Not specified"" for missing info.";
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

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error: {response.StatusCode} - {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);
        return geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "";
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
