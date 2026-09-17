using System.Net;
using System.Text;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DecisionReplay.Tests;

[CollectionDefinition("AI environment", DisableParallelization = true)]
public sealed class AiEnvironmentCollection;

[Collection("AI environment")]
public sealed class OllamaLanguageServiceTests : IDisposable
{
    private readonly Dictionary<string, string?> _original = new();

    public OllamaLanguageServiceTests()
    {
        Set("OLLAMA_BASE_URL", "http://ollama.test:11434");
        Set("OLLAMA_MODEL", "test-model:3b");
        Set("OLLAMA_REQUEST_TIMEOUT_SECONDS", "5");
        Set("GROQ_API_KEY", "test-key");
        Set("GROQ_BASE_URL", "https://groq.test/v1");
    }

    [Fact]
    public async Task HybridKeepsExtractionOnGroqAndWordingOnOllama()
    {
        var requests = new List<string>();
        var factory = new Factory(async (request, token) =>
        {
            requests.Add(request.RequestUri!.AbsolutePath);
            if (request.RequestUri.Host == "groq.test")
                return Json("""{"choices":[{"message":{"content":"{\"domain\":\"project_planning\",\"title\":\"Extracted by Groq\",\"goal\":\"Build\",\"fields\":{}}"}}]}""");
            if (request.Method == HttpMethod.Get)
                return Json("""{"models":[{"name":"test-model:3b"}]}""");
            var body = await request.Content!.ReadAsStringAsync(token);
            using var payload = System.Text.Json.JsonDocument.Parse(body);
            Assert.Equal("test-model:3b", payload.RootElement.GetProperty("model").GetString());
            Assert.False(payload.RootElement.GetProperty("stream").GetBoolean());
            Assert.Null(request.Headers.Authorization);
            return Json("""{"message":{"content":"Local explanation"}}""");
        });
        var hybrid = new HybridLanguageService(new GroqLanguageService(factory,
            NullLogger<GroqLanguageService>.Instance), Create(factory));
        var extracted = await hybrid.ExtractDecisionAsync("Build a project", Array.Empty<DecisionDomainTemplate>());
        Assert.Equal("Extracted by Groq", extracted.Title);
        Assert.Equal("Local explanation", await hybrid.ExplainResultAsync(extracted, new FeasibilityAssessment()));
        Assert.Equal(new[] { "/v1/chat/completions", "/api/tags", "/api/chat" }, requests);
    }

    [Fact]
    public async Task MissingModelPreventsInference()
    {
        var calls = 0;
        var service = Create(new Factory((_, _) =>
        {
            calls++;
            return Task.FromResult(Json("""{"models":[{"name":"different-model"}]}"""));
        }));
        Assert.Equal("missing_model", (await service.CheckAvailabilityAsync()).Status);
        Assert.Equal(string.Empty, await service.ExplainResultAsync(new(), new()));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task UnavailableRuntimeIsReported()
    {
        var service = Create(new Factory((_, _) => throw new HttpRequestException("offline")));
        Assert.Equal("runtime_unavailable", (await service.CheckAvailabilityAsync()).Status);
        Assert.Equal(string.Empty, await service.SummarizeReplayAsync(new()));
    }

    [Theory]
    [InlineData("{\"error\":\"model crashed\"}", HttpStatusCode.OK)]
    [InlineData("{}", HttpStatusCode.OK)]
    [InlineData("not JSON", HttpStatusCode.OK)]
    [InlineData("{}", HttpStatusCode.InternalServerError)]
    public async Task FailedInferencePreservesDeterministicFallback(string body, HttpStatusCode code)
    {
        var service = Create(new Factory((request, _) => Task.FromResult(
            request.Method == HttpMethod.Get
                ? Json("""{"models":[{"name":"test-model:3b"}]}""")
                : Json(body, code))));
        Assert.Equal(string.Empty, await service.ExplainResultAsync(new(), new()));
        Assert.Empty(await service.EnhancePlanTasksAsync(new(), new(), new()));
    }

    [Fact]
    public async Task TimeoutAndCallerCancellationAreDistinct()
    {
        Set("OLLAMA_REQUEST_TIMEOUT_SECONDS", "0.05");
        var service = Create(new Factory(async (_, token) =>
        {
            await Task.Delay(10000, token);
            return Json("{}");
        }));
        Assert.Equal("timeout", (await service.CheckAvailabilityAsync()).Status);
        Assert.Equal(string.Empty, await service.ExplainResultAsync(new(), new()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ExplainResultAsync(new(), new(), cancellation.Token));
    }

    private static OllamaLanguageService Create(Factory factory) => new(factory, NullLogger<OllamaLanguageService>.Instance);
    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private void Set(string name, string value)
    {
        _original.TryAdd(name, Environment.GetEnvironmentVariable(name));
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose()
    {
        foreach (var (name, value) in _original) Environment.SetEnvironmentVariable(name, value);
    }

    private sealed class Factory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new Handler(send));
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
    }
}
