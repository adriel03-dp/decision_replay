using DecisionReplay.Application.Services;
using DecisionReplay.Infrastructure.AI;

namespace DecisionReplay.Tests;

public sealed class EvaluationTests
{
    [Fact]
    public void GoldenDatasetHasStableVersionAndExplicitT0Provenance()
    {
        var catalog = new EmbeddedGoldenDatasetCatalog();
        var dataset = catalog.Resolve("v1");
        Assert.Equal(3, dataset.Cases.Count);
        Assert.Equal(dataset.Hash, catalog.Resolve("v1").Hash);
        foreach (var item in dataset.Cases)
        {
            Assert.NotEmpty(item.Context.Evidence);
            Assert.NotEmpty(item.ExpectedRiskConcepts);
            ReplayContextService.Validate(item.Context);
        }
    }

    [Fact]
    public void MetricsAreDerivedFromActualOutputAndFailure()
    {
        var item = new EmbeddedGoldenDatasetCatalog().Resolve("v1").Cases[0];
        var metrics = EvaluationScorer.Score(item, AiTestHarness.ValidAnalysis(), 123);
        Assert.Equal(1, metrics.StructuredValidity);
        Assert.Equal(1, metrics.MissingInformationCoverage);
        Assert.Equal(.5, metrics.RiskConceptCoverage);
        Assert.Equal(.5, metrics.AlternativeConceptCoverage);
        Assert.Equal(123, metrics.LatencyMs);
        var failure = EvaluationScorer.Score(item, null, 42);
        Assert.Equal(1, failure.FailureRate);
        Assert.Equal(0, failure.StructuredValidity);
    }

    [Fact]
    public async Task EvaluationPipelinePersistsReproducibleExperimentAndPerCaseExecutions()
    {
        var harness = new AiTestHarness();
        var evaluation = new EvaluationService(new EmbeddedGoldenDatasetCatalog(), new EmbeddedPromptCatalog(),
            harness.Workflow, harness.Runs, harness.Resolver, harness.Scope);
        var experiments = await evaluation.RunAsync("test", "v1", "v1", new[] { new DecisionReplay.Domain.ValueObjects.AiTarget("ollama", "test:3b") });
        var experiment = Assert.Single(experiments);
        Assert.Equal("completed", experiment.Status);
        Assert.Equal(3, experiment.Results.Count);
        Assert.Equal(3, harness.Provider.Requests.Count);
        Assert.Equal(3, (await harness.Runs.GetExecutionsAsync("test")).Count);
        Assert.Equal(1, experiment.Metrics.StructuredValidity);
        Assert.Null(experiment.ApiCost);
        Assert.Equal(harness.Resolver.RequestTimeout.TotalSeconds, experiment.RequestTimeoutSeconds);
        Assert.Equal(harness.Resolver.MaxRetries, experiment.MaxRetries);
        Assert.All(await harness.Runs.GetExecutionsAsync("test"), execution =>
        {
            Assert.Equal(experiment.RequestTimeoutSeconds, execution.RequestTimeoutSeconds);
            Assert.Equal(experiment.MaxRetries, execution.MaxRetries);
        });
        Assert.Null(await harness.Runs.GetExperimentAsync(experiment.Id, "other-user"));
        Assert.All(experiment.Results, result => Assert.Null(result.HumanReview));
    }
}
