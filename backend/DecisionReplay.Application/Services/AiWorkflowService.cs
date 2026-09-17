using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DecisionReplay.Application.Services;

public sealed class AiWorkflowService(IAiProviderResolver providers, IPromptCatalog prompts,
    IAiRunRepository repository, AiExecutionScope scope, ILogger<AiWorkflowService> logger)
{
    public static readonly ActivitySource Activities = new("DecisionReplay.AI");
    private static readonly Meter Meter = new("DecisionReplay.AI");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("ai.executions");
    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("ai.latency", "ms");
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 32
    };

    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public async Task<ValidatedAiResult<T>> GenerateAsync<T>(string operation, string version, object input,
        Func<T, string?> validate, AiExecutionContext? context = null, AiTarget? target = null,
        bool allowFallback = true, string? outputSchema = null, CancellationToken cancellationToken = default)
    {
        context ??= scope.Context;
        using var activity = Activities.StartActivity(operation);
        var clock = Stopwatch.StartNew();
        var prompt = prompts.Resolve(operation, version);
        var serialized = JsonSerializer.Serialize(input, Json);
        var execution = new AiExecution
        {
            OwnerId = context.OwnerId, RequestId = context.RequestId, DecisionId = context.DecisionId,
            ReplayId = context.ReplayId, ContextVersion = context.ContextVersion, ExperimentId = context.ExperimentId,
            Operation = operation, PromptVersion = prompt.Version, PromptHash = prompt.Hash,
            InputHash = Hash(serialized), Parameters = providers.Parameters, RequestTimeoutSeconds = providers.RequestTimeout.TotalSeconds, MaxRetries = providers.MaxRetries,
            OutputSchemaHash = outputSchema == null ? null : Hash(outputSchema)
        };
        var user = prompt.User.Replace("{{context}}", serialized, StringComparison.Ordinal);
        execution.PromptConstructionMs = clock.Elapsed.TotalMilliseconds;
        activity?.SetTag("ai.execution.id", execution.Id.ToString());
        await repository.SaveExecutionAsync(execution, cancellationToken);
        try
        {
            var primary = target ?? providers.DefaultTarget(operation);
            var targets = new List<AiTarget> { primary };
            if (allowFallback && providers.FallbackTarget is { } fallback && fallback != primary) targets.Add(fallback);
            AiException? lastError = null;
            foreach (var selected in targets)
            {
                execution.UsedFallback = selected != primary;
                var provider = providers.Resolve(selected.Provider);
                for (var attempt = 0; attempt <= providers.MaxRetries; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var record = new AiAttempt { Number = execution.Attempts.Count + 1, Provider = selected.Provider, Model = selected.Model };
                    execution.Attempts.Add(record);
                    var inferenceClock = Stopwatch.StartNew();
                    try
                    {
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        timeout.CancelAfter(providers.RequestTimeout);
                        var previousValidation = execution.Attempts.SkipLast(1).LastOrDefault()?.ValidationError;
                        var repair = "";
                        if (lastError?.Code is "malformed_json" or "schema_validation" or "empty_response" or "invalid_output")
                        {
                            var repairPrompt = prompts.Resolve("output-repair", "v1");
                            record.RepairPromptVersion = repairPrompt.Version;
                            record.RepairPromptHash = repairPrompt.Hash;
                            repair = "\n" + repairPrompt.User.Replace("{{context}}", JsonSerializer.Serialize(
                                new { errorCode = lastError.Code, validationError = previousValidation }, Json), StringComparison.Ordinal);
                        }
                        AiGenerationResponse response;
                        using (Activities.StartActivity("inference"))
                            response = await provider.GenerateAsync(new(prompt.System, user + repair, selected.Model, providers.Parameters, JsonSchema: outputSchema), timeout.Token);
                        record.InferenceMs = inferenceClock.Elapsed.TotalMilliseconds;
                        record.InputTokens = response.InputTokens;
                        record.OutputTokens = response.OutputTokens;
                        record.ModelRevision = response.ModelRevision;
                        record.SchemaEnforced = response.SchemaEnforced;
                        record.Output = response.Content.Length > 65536 ? response.Content[..65536] : response.Content;
                        var validationClock = Stopwatch.StartNew();
                        try
                        {
                            using var validationActivity = Activities.StartActivity("parse-and-validate");
                            if (string.IsNullOrWhiteSpace(response.Content)) throw new AiException("empty_response", "Model output is empty.");
                            if (response.Content.Length > 65536) throw new AiException("invalid_output", "Model output exceeded the allowed length.");
                            T parsed;
                            try { parsed = JsonSerializer.Deserialize<T>(response.Content, Json) ?? throw new AiException("schema_validation", "Model output was null."); }
                            catch (JsonException ex) { throw new AiException("malformed_json", "Model output did not match the JSON contract.", inner: ex); }
                            var validation = validate(parsed);
                            if (validation != null)
                            {
                                record.ValidationError = validation;
                                throw new AiException("schema_validation", "Model output failed schema validation.");
                            }
                            execution.Status = "succeeded";
                            execution.Output = response.Content;
                            return new(parsed, execution.Id);
                        }
                        finally { record.ValidationMs = validationClock.Elapsed.TotalMilliseconds; }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception ex) when (ex is AiException or HttpRequestException or OperationCanceledException or InvalidOperationException)
                    {
                        record.InferenceMs = record.InferenceMs == 0 ? inferenceClock.Elapsed.TotalMilliseconds : record.InferenceMs;
                        lastError = ex switch
                        {
                            AiException ai => ai,
                            OperationCanceledException => new("timeout", "AI request timed out.", true),
                            HttpRequestException => new("runtime_unavailable", "Cannot reach the configured AI runtime.", true),
                            _ => new("configuration_error", "AI provider configuration is invalid.")
                        };
                        record.ErrorCode = lastError.Code;
                        logger.LogWarning("AI attempt failed ExecutionId={ExecutionId} Provider={Provider} Model={Model} Attempt={Attempt} ErrorCode={ErrorCode}",
                            execution.Id, selected.Provider, selected.Model, record.Number, lastError.Code);
                        var retryable = lastError.IsTransient || lastError.Code is "malformed_json" or "schema_validation" or "empty_response" or "invalid_output";
                        if (!retryable || attempt == providers.MaxRetries) break;
                        // Never retry ahead of a long provider rate-limit window.
                        if (lastError.RetryAfter > TimeSpan.FromSeconds(30)) break;
                        execution.RetryCount++;
                        var backoff = TimeSpan.FromMilliseconds((lastError.Code == "rate_limited" ? 2000 : 250) * Math.Pow(2, attempt));
                        await Task.Delay(lastError.RetryAfter is { } requested && requested > backoff ? requested : backoff, cancellationToken);
                    }
                }
            }
            execution.ErrorCode = lastError?.Code ?? "inference_error";
            throw new AiException(execution.ErrorCode, $"AI execution {execution.Id} failed ({execution.ErrorCode}). Inspect its execution record.");
        }
        catch (OperationCanceledException)
        {
            execution.Status = "cancelled";
            execution.ErrorCode = "cancelled";
            throw;
        }
        catch
        {
            execution.Status = "failed";
            execution.ErrorCode ??= "inference_error";
            throw;
        }
        finally
        {
            execution.FinishedAt = DateTime.UtcNow;
            execution.LatencyMs = clock.Elapsed.TotalMilliseconds;
            // Persist terminal status even when the caller has disconnected. Persistence failure is never silent.
            using var persistenceTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await repository.SaveExecutionAsync(execution, persistenceTimeout.Token);
            Requests.Add(1, new KeyValuePair<string, object?>("operation", operation), new("status", execution.Status));
            Duration.Record(execution.LatencyMs, new KeyValuePair<string, object?>("operation", operation));
            logger.LogInformation("AI execution {ExecutionId} Operation={Operation} Prompt={PromptVersion} Status={Status} LatencyMs={LatencyMs} Retries={Retries}",
                execution.Id, operation, version, execution.Status, execution.LatencyMs, execution.RetryCount);
        }
    }
}
