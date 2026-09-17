using System.Net;
using System.Text;
using System.Text.Json;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.AI;
using DecisionReplay.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace DecisionReplay.Tests;

public sealed class AiInfrastructureTests
{
    [Theory]
    [InlineData("hybrid", "extraction", "groq")]
    [InlineData("hybrid", "explanation", "ollama")]
    [InlineData("ollama", "extraction", "ollama")]
    [InlineData("groq", "decision-analysis", "groq")]
    public void ProviderSelectionPreservesHybridAndSupportsKeyFreeMode(string selection, string operation, string expected)
    {
        var config = AiTestHarness.Configuration(selection);
        Assert.Equal(expected, config.Target(operation).Provider);
        config.ValidateSelectedProviders();
    }

    [Fact]
    public void CloudSelectionWithoutCredentialsFailsClearly()
    {
        var config = new AiConfiguration { Provider = "groq" };
        Assert.Contains("GROQ_API_KEY", Assert.Throws<InvalidOperationException>(config.ValidateSelectedProviders).Message);
    }

    [Fact]
    public void LocalSelectionNeedsNoCloudCredentials()
    {
        new AiConfiguration { Provider = "ollama", OllamaBaseUrl = "http://ollama.test:11434", OllamaModel = "test:3b" }.ValidateSelectedProviders();
    }

    [Fact]
    public void PromptVersionsAreResolvedAndHashed()
    {
        var prompts = new EmbeddedPromptCatalog();
        var first = prompts.Resolve("decision-analysis", "v1");
        var second = prompts.Resolve("decision-analysis", "v2");
        Assert.NotEqual(first.Hash, second.Hash);
        Assert.Equal(64, first.Hash.Length);
        Assert.Throws<ArgumentException>(() => prompts.Resolve("decision-analysis", "v9"));
    }

    [Fact]
    public void MissingAndUnexpectedSchemaMembersAreRejected()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionAiAnalysis>("{}", AiWorkflowService.Json));
        var body = JsonSerializer.Serialize(AiTestHarness.ValidAnalysis(), AiWorkflowService.Json);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<DecisionAiAnalysis>(body[..^1] + ",\"actualOutcome\":\"leak\"}", AiWorkflowService.Json));
    }

    [Fact]
    public void InvalidEvidenceReferencesAndUnlabelledSpeculationAreRejected()
    {
        var output = AiTestHarness.ValidAnalysis();
        output.Risks[0].EvidenceIds = new() { "unknown" };
        Assert.NotNull(AiOutputValidation.Analysis(output, AiTestHarness.Context()));
        output.Risks[0].EvidenceIds.Clear();
        Assert.NotNull(AiOutputValidation.Analysis(output, AiTestHarness.Context()));
        output.Risks[0].IsHypothesis = true;
        Assert.Null(AiOutputValidation.Analysis(output, AiTestHarness.Context()));
        output.Confidence = double.NaN;
        Assert.NotNull(AiOutputValidation.Analysis(output, AiTestHarness.Context()));
    }

    [Fact]
    public async Task MalformedOutputIsRepairedWithBoundedRetryAndProvenance()
    {
        var calls = 0;
        var harness = new AiTestHarness(send: (_, _) => Task.FromResult(new AiGenerationResponse(++calls == 1 ? "broken JSON" : AiTestHarness.AnalysisJson(), 10, 20)));
        var result = await harness.Analyse();
        var execution = await harness.Runs.GetExecutionAsync(result.ExecutionId, "test");
        Assert.Equal(2, calls);
        Assert.Equal("succeeded", execution!.Status);
        Assert.Equal(1, execution.RetryCount);
        Assert.Equal("malformed_json", execution.Attempts[0].ErrorCode);
        Assert.Equal("v1", execution.PromptVersion);
        Assert.Equal(64, execution.InputHash.Length);
        Assert.Equal(20, execution.Attempts[1].OutputTokens);
        Assert.Contains("previous attempt failed", harness.Provider.Requests[1].UserPrompt);
    }

    [Fact]
    public async Task SchemaFailureIsRetriedThenFailsExplicitly()
    {
        var output = AiTestHarness.ValidAnalysis(); output.Risks[0].EvidenceIds = new() { "invented" };
        var harness = new AiTestHarness(send: (_, _) => Task.FromResult(new AiGenerationResponse(JsonSerializer.Serialize(output, AiWorkflowService.Json))));
        var error = await Assert.ThrowsAsync<AiException>(() => harness.Analyse());
        Assert.Equal("schema_validation", error.Code);
        Assert.Equal(2, harness.Provider.Requests.Count);
        Assert.Equal("failed", (await harness.Runs.GetExecutionsAsync("test")).Single().Status);
    }

    [Theory]
    [InlineData("rate_limited", true, 2)]
    [InlineData("runtime_unavailable", true, 2)]
    [InlineData("missing_model", false, 1)]
    public async Task RetriesOnlyRecoverableFailures(string code, bool transient, int expectedCalls)
    {
        var harness = new AiTestHarness(send: (_, _) => throw new AiException(code, "fake provider failure", transient));
        await Assert.ThrowsAsync<AiException>(() => harness.Analyse());
        Assert.Equal(expectedCalls, harness.Provider.Requests.Count);
    }

    [Fact]
    public async Task ExplicitFallbackIsRecorded()
    {
        var failing = new AiTestProvider("ollama", (_, _) => throw new AiException("runtime_unavailable", "offline", true));
        var fallback = new AiTestProvider("groq", (_, _) => Task.FromResult(new AiGenerationResponse(AiTestHarness.AnalysisJson())));
        var configuration = new AiConfiguration
        {
            Provider = "ollama", OllamaModel = "test:3b", MaxRetries = 0, Fallback = new("groq", "cloud-test")
        };
        var resolver = new AiProviderResolver(new[] { failing, fallback }, configuration);
        var runs = new InMemoryAiRunRepository();
        var ai = new AiWorkflowService(resolver, new EmbeddedPromptCatalog(), runs, new(), NullLogger<AiWorkflowService>.Instance);
        var result = await ai.GenerateAsync<DecisionAiAnalysis>("decision-analysis", "v1", ReplayContextService.T0Input(AiTestHarness.Context()),
            output => AiOutputValidation.Analysis(output, AiTestHarness.Context()), new("test", "fallback"));
        var execution = (await runs.GetExecutionAsync(result.ExecutionId, "test"))!;
        Assert.True(execution.UsedFallback);
        Assert.Equal("groq", execution.Attempts.Last().Provider);
    }

    [Fact]
    public async Task TimeoutIsRecordedWithoutSwallowingCallerCancellation()
    {
        var harness = new AiTestHarness(maxRetries: 0, timeout: TimeSpan.FromMilliseconds(30), send: async (_, token) =>
        { await Task.Delay(10000, token); return new("never"); });
        Assert.Equal("timeout", (await Assert.ThrowsAsync<AiException>(() => harness.Analyse())).Code);
        var cancellable = new AiTestHarness(maxRetries: 0, timeout: TimeSpan.FromSeconds(10), send: async (_, token) =>
        { await Task.Delay(10000, token); return new("never"); });
        using var cancellation = new CancellationTokenSource(30);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancellable.Analyse(cancellation.Token));
        Assert.Contains(await cancellable.Runs.GetExecutionsAsync("test"), execution => execution.Status == "cancelled");
    }

    [Fact]
    public async Task OllamaHttpContractChecksModelAndReadsMessageContent()
    {
        var paths = new List<string>();
        var factory = new TestHttpFactory(async (request, token) =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            Assert.Null(request.Headers.Authorization);
            if (request.Method == HttpMethod.Get) return Response("""{"models":[{"name":"test:3b"}]}""");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
            Assert.Equal("test:3b", body.RootElement.GetProperty("model").GetString());
            return Response("""{"message":{"content":"{\"text\":\"local answer\"}"},"prompt_eval_count":12,"eval_count":8}""");
        });
        var provider = new OllamaProvider(factory, AiTestHarness.Configuration());
        var result = await provider.GenerateAsync(new("system", "user", "test:3b", new()));
        Assert.Contains("local answer", result.Content);
        Assert.Equal(12, result.InputTokens);
        Assert.Equal(new[] { "/api/tags", "/api/chat" }, paths);
    }

    [Fact]
    public async Task MissingModelDoesNotLaunchInference()
    {
        var calls = 0;
        var provider = new OllamaProvider(new TestHttpFactory((_, _) =>
        { calls++; return Task.FromResult(Response("""{"models":[]}""")); }), AiTestHarness.Configuration());
        Assert.Equal("missing_model", (await Assert.ThrowsAsync<AiException>(() => provider.GenerateAsync(new("system", "user", "test:3b", new())))).Code);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CloudProviderHttpContractIsIsolatedFromDomain()
    {
        var provider = new GroqProvider(new TestHttpFactory(async (request, token) =>
        {
            Assert.Equal("/v1/chat/completions", request.RequestUri!.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.Equal("json_object", body.RootElement.GetProperty("response_format").GetProperty("type").GetString());
            return Response("""{"choices":[{"message":{"content":"{\"text\":\"cloud answer\"}"}}],"usage":{"prompt_tokens":3,"completion_tokens":4}}""");
        }), AiTestHarness.Configuration("groq"));
        var response = await provider.GenerateAsync(new("system", "user", "test-cloud", new()));
        Assert.Contains("cloud answer", response.Content);
        Assert.Equal(4, response.OutputTokens);
    }

    [Fact]
    public async Task LanguageAdapterKeepsGroqExtractionAndOllamaWording()
    {
        var groq = new AiTestProvider("groq", (_, _) => Task.FromResult(new AiGenerationResponse("""{"domain":"project_planning","title":"Groq extraction","goal":"Build portal","fields":{},"assumptions":[],"missingFields":[]}""")));
        var ollama = new AiTestProvider("ollama", (_, _) => Task.FromResult(new AiGenerationResponse("""{"text":"Local wording"}""")));
        var resolver = new AiProviderResolver(new[] { groq, ollama }, AiTestHarness.Configuration("hybrid"));
        var language = new AiLanguageService(new(resolver, new EmbeddedPromptCatalog(), new InMemoryAiRunRepository(), new(),
            NullLogger<AiWorkflowService>.Instance), resolver, NullLogger<AiLanguageService>.Instance);
        Assert.Equal("Groq extraction", (await language.ExtractDecisionAsync("Build a portal", new DomainTemplateService().GetAll())).Title);
        Assert.Equal("Local wording", await language.ExplainResultAsync(new(), new()));
        Assert.Single(groq.Requests); Assert.Single(ollama.Requests);
    }

    [Fact]
    public async Task DeterministicExtractionFallbackIsExplicit()
    {
        var harness = new AiTestHarness(maxRetries: 0, send: (_, _) => throw new AiException("runtime_unavailable", "offline"));
        var language = new AiLanguageService(harness.Workflow, harness.Resolver, NullLogger<AiLanguageService>.Instance);
        var result = await language.ExtractDecisionAsync("Project budget 60000 for 3 months with team 2", new DomainTemplateService().GetAll());
        Assert.Equal("60000", result.Fields["budget"]);
        Assert.Contains(result.Assumptions, assumption => assumption.Contains("deterministic parser"));
        Assert.Equal("failed", (await harness.Runs.GetExecutionsAsync("test")).Single().Status);
    }

    private static HttpResponseMessage Response(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private sealed class TestHttpFactory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(new Handler(send)); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token); }
}

internal sealed class AiTestProvider(string name, Func<AiGenerationRequest, CancellationToken, Task<AiGenerationResponse>> send) : IAiProvider
{
    public string Name => name;
    public List<AiGenerationRequest> Requests { get; } = new();
    public Task<AiAvailability> CheckAvailabilityAsync(string model, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiAvailability("healthy", Name, model));
    public Task<AiGenerationResponse> GenerateAsync(AiGenerationRequest request, CancellationToken cancellationToken = default)
    { Requests.Add(request); return send(request, cancellationToken); }
}

internal sealed class AiTestHarness
{
    public InMemoryAiRunRepository Runs { get; } = new();
    public AiTestProvider Provider { get; }
    public AiWorkflowService Workflow { get; }
    public AiProviderResolver Resolver { get; }
    public AiExecutionScope Scope { get; } = new() { Context = new("test", "request") };
    public AiTestHarness(int maxRetries = 1, TimeSpan? timeout = null,
        Func<AiGenerationRequest, CancellationToken, Task<AiGenerationResponse>>? send = null)
    {
        Provider = new("ollama", send ?? ((_, _) => Task.FromResult(new AiGenerationResponse(AnalysisJson()))));
        Resolver = new(new[] { Provider }, new AiConfiguration
        { Provider = "ollama", OllamaModel = "test:3b", MaxRetries = maxRetries, RequestTimeout = timeout ?? TimeSpan.FromSeconds(5) });
        Workflow = new(Resolver, new EmbeddedPromptCatalog(), Runs, Scope, NullLogger<AiWorkflowService>.Instance);
    }
    public Task<ValidatedAiResult<DecisionAiAnalysis>> Analyse(CancellationToken token = default) =>
        Workflow.GenerateAsync<DecisionAiAnalysis>("decision-analysis", "v1", ReplayContextService.T0Input(Context()),
            output => AiOutputValidation.Analysis(output, Context()), cancellationToken: token);
    public static AiConfiguration Configuration(string provider = "ollama") => new()
    { Provider = provider, OllamaBaseUrl = "http://ollama.test:11434", OllamaModel = "test:3b", GroqBaseUrl = "https://groq.test/v1", GroqModel = "test-cloud", GroqApiKey = "fake-test-key" };
    public static DecisionContextSnapshot Context() => new()
    {
        DecisionAt = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc), DecisionText = "Build a customer portal", ChosenAction = "Run a pilot",
        ExpectedOutcome = "Validate demand", Fields = new() { ["budget"] = "100000", ["timeline_months"] = "4", ["team_size"] = "2" },
        Evidence = new() { new() { Id = "e1", Text = "No paying customer has been confirmed", KnownAt = new(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc) } },
        Assumptions = new() { new() { Id = "a1", Text = "Demand will be high", KnownAt = new(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc) } }
    };
    public static DecisionAiAnalysis ValidAnalysis() => new()
    {
        Analysis = "T0 evidence is limited. Validate the plan before committing.", Confidence = .4,
        Assumptions = new() { new() { Text = "Demand may be high", IsHypothesis = true } },
        Risks = new() { new() { Text = "Demand is unvalidated", EvidenceIds = new() { "e1" } } },
        MissingInformation = new() { "Pricing and delivery costs" },
        Alternatives = new() { new() { Name = "Smaller paid pilot", Tradeoff = "Lower scope and funding exposure", IsHypothesis = true } }
    };
    public static string AnalysisJson() => JsonSerializer.Serialize(ValidAnalysis(), AiWorkflowService.Json);
}
