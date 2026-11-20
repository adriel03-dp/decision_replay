using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.AI;

internal static class AiHttp
{
    public static async Task<JsonDocument> ReadAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            var transient = status == 429 || status >= 500 || status == 408;
            var retryAfter = response.Headers.RetryAfter?.Delta
                ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : (TimeSpan?)null);
            throw new AiException(status == 429 ? "rate_limited" : "provider_http_error",
                $"AI provider returned HTTP {status}.", transient) { RetryAfter = retryAfter > TimeSpan.Zero ? retryAfter : null };
        }
        var body = await response.Content.ReadAsStringAsync(token);
        if (body.Length > 131072) throw new AiException("invalid_output", "AI response exceeded the size limit.");
        try { return JsonDocument.Parse(body); }
        catch (JsonException ex) { throw new AiException("malformed_json", "AI provider returned malformed JSON.", inner: ex); }
    }

    public static string Content(JsonElement message)
    {
        if (message.ValueKind != JsonValueKind.Object || !message.TryGetProperty("content", out var content)
            || content.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(content.GetString()))
            throw new AiException("empty_response", "AI provider returned no message content.");
        return content.GetString()!.Trim();
    }

    public static int? Count(JsonElement root, string name) => root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var count) && count >= 0 ? count : null;
}

public sealed class OllamaProvider(IHttpClientFactory factory, AiConfiguration configuration) : IAiProvider
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    public string Name => "ollama";

    private HttpClient Client()
    {
        var client = factory.CreateClient(nameof(OllamaProvider));
        client.Timeout = Timeout.InfiniteTimeSpan;
        return client;
    }

    public async Task<AiAvailability> CheckAvailabilityAsync(string model, CancellationToken cancellationToken = default)
    {
        try
        {
            configuration.ValidateTarget(new(Name, model));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(configuration.RequestTimeout);
            using var client = Client();
            using var response = await client.GetAsync($"{configuration.OllamaBaseUrl.TrimEnd('/')}/api/tags", timeout.Token);
            using var body = await AiHttp.ReadAsync(response, timeout.Token);
            if (body.RootElement.ValueKind != JsonValueKind.Object
                || !body.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                throw new AiException("invalid_response", "Ollama returned an invalid model list.");
            var installed = models.EnumerateArray().FirstOrDefault(item => item.ValueKind == JsonValueKind.Object &&
                new[] { "name", "model" }.Any(key => item.TryGetProperty(key, out var value)
                    && value.ValueKind == JsonValueKind.String && value.GetString() == model));
            if (installed.ValueKind == JsonValueKind.Undefined)
                return new("missing_model", Name, model, "The configured model is not installed in the existing Ollama runtime.");
            var revision = installed.TryGetProperty("digest", out var digest) && digest.ValueKind == JsonValueKind.String ? digest.GetString() : null;
            return new("healthy", Name, model, ModelRevision: revision);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return new("timeout", Name, model, "Ollama model verification timed out."); }
        catch (HttpRequestException) { return new("runtime_unavailable", Name, model, "Cannot reach the configured Ollama runtime."); }
        catch (AiException ex) { return new(ex.Code, Name, model, ex.Message); }
        catch (InvalidOperationException ex) { return new("configuration_error", Name, model, ex.Message); }
    }

    public async Task<AiGenerationResponse> GenerateAsync(AiGenerationRequest request, CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var availability = await CheckAvailabilityAsync(request.Model, cancellationToken);
            if (availability.Status != "healthy")
                throw new AiException(availability.Status, availability.Error ?? "Ollama is unavailable.",
                    availability.Status is "runtime_unavailable" or "timeout" or "provider_http_error" or "rate_limited");
            using var client = Client();
            var payload = new Dictionary<string, object>
            {
                ["model"] = request.Model,
                ["messages"] = new object[] { new { role = "system", content = request.SystemPrompt }, new { role = "user", content = request.UserPrompt } },
                ["stream"] = false,
                ["options"] = new { temperature = request.Parameters.Temperature, num_predict = request.Parameters.MaxOutputTokens }
            };
            if (request.JsonOutput) payload["format"] = request.JsonSchema == null
                ? "json" : JsonSerializer.Deserialize<JsonElement>(request.JsonSchema);
            using var response = await client.PostAsJsonAsync($"{configuration.OllamaBaseUrl.TrimEnd('/')}/api/chat", payload, cancellationToken);
            using var body = await AiHttp.ReadAsync(response, cancellationToken);
            var root = body.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _)
                || !root.TryGetProperty("message", out var message))
                throw new AiException("inference_error", "Ollama returned an invalid inference envelope.");
            return new(AiHttp.Content(message), AiHttp.Count(root, "prompt_eval_count"), AiHttp.Count(root, "eval_count"),
                availability.ModelRevision, request.JsonSchema != null);
        }
        finally { Gate.Release(); }
    }
}

public sealed class GroqProvider(IHttpClientFactory factory, AiConfiguration configuration) : IAiProvider
{
    public string Name => "groq";
    public Task<AiAvailability> CheckAvailabilityAsync(string model, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            configuration.ValidateTarget(new(Name, model));
            // Credential validation only; an inference verifies cloud reachability.
            return Task.FromResult(new AiAvailability("configured", Name, model));
        }
        catch (InvalidOperationException ex) { return Task.FromResult(new AiAvailability("configuration_error", Name, model, ex.Message)); }
    }

    public async Task<AiGenerationResponse> GenerateAsync(AiGenerationRequest request, CancellationToken cancellationToken = default)
    {
        configuration.ValidateTarget(new(Name, request.Model));
        using var client = factory.CreateClient(nameof(GroqProvider));
        client.Timeout = Timeout.InfiniteTimeSpan;
        using var message = new HttpRequestMessage(HttpMethod.Post, $"{configuration.GroqBaseUrl.TrimEnd('/')}/chat/completions");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.GroqApiKey);
        var payload = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["temperature"] = request.Parameters.Temperature,
            ["max_tokens"] = request.Parameters.MaxOutputTokens,
            ["messages"] = new object[] { new { role = "system", content = request.SystemPrompt }, new { role = "user", content = request.UserPrompt } }
        };
        if (request.JsonOutput) payload["response_format"] = new { type = "json_object" };
        message.Content = JsonContent.Create(payload);
        using var response = await client.SendAsync(message, cancellationToken);
        using var body = await AiHttp.ReadAsync(response, cancellationToken);
        var root = body.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0
            || choices[0].ValueKind != JsonValueKind.Object
            || !choices[0].TryGetProperty("message", out var content))
            throw new AiException("inference_error", "Groq returned an invalid inference envelope.");
        var usage = root.TryGetProperty("usage", out var tokens) ? tokens : default;
        return new(AiHttp.Content(content), AiHttp.Count(usage, "prompt_tokens"), AiHttp.Count(usage, "completion_tokens"));
    }
}
