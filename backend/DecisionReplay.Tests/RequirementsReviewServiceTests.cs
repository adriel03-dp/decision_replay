using DecisionReplay.Application.Services;

namespace DecisionReplay.Tests;

public sealed class RequirementsReviewServiceTests
{
    [Fact]
    public async Task PreviewExtractsExplicitFieldsAndExposesUnconfirmedGaps()
    {
        var review = await WorkflowFixture.Review().PreviewAsync("Build a mobile app with team six in six months and budget 100000 USD");
        Assert.Equal("100000", review.Data.Get("budget"));
        Assert.Equal("6", review.Data.Get("timeline_months"));
        Assert.Contains("scope", review.Validation.MissingFields);
        Assert.True(review.CanSubmit); // Explicitly acknowledged gaps remain supported by the conservative engine.
    }

    [Fact]
    public void ReviewBlocksMalformedValuesUntilTheyAreCorrected()
    {
        var data = WorkflowFixture.Project(); data.Fields["timeline_months"] = "six to twelve";
        var service = WorkflowFixture.Review();
        var invalid = service.Review(data);
        Assert.False(invalid.CanSubmit);
        Assert.Contains("timeline_months", invalid.Validation.InvalidFields);
        data.Fields["timeline_months"] = "6";
        Assert.True(service.Review(data).CanSubmit);
    }

    [Fact]
    public void ReviewRejectsUnsupportedDomainAndUnknownFields()
    {
        var data = WorkflowFixture.Project(); data.Domain = "fictional";
        Assert.Throws<ArgumentException>(() => WorkflowFixture.Review().Review(data));
        data.Domain = "project_planning"; data.Fields["success_probability"] = "100";
        Assert.False(WorkflowFixture.Review().Review(data).CanSubmit);
    }

    [Fact]
    public void ReviewSchemaUsesNamedTypesAndIncludesPriorityInputsOnlyWhereSupported()
    {
        var domains = WorkflowFixture.Review().Domains();
        Assert.Equal(5, domains.Count);
        Assert.Contains(domains.Single(domain => domain.Domain == "project_planning").Fields, field => field.Name == "budget" && field.Type == "Currency");
        Assert.Contains(domains.Single(domain => domain.Domain == "product_launch").Fields, field => field.Name == "required_scope");
        Assert.DoesNotContain(domains.Single(domain => domain.Domain == "career_decision").Fields, field => field.Name == "required_scope");
    }

    [Fact]
    public async Task ConfirmedStructuredCreationPreservesOriginalNarrativeWithoutReextractingIt()
    {
        var repository = new DecisionReplay.Infrastructure.Persistence.Repositories.InMemoryDecisionV2Repository();
        var data = WorkflowFixture.Project(); data.Fields["budget"] = "110000";
        var decision = await WorkflowFixture.Engine(repository).AnalyzeStructuredAndCreateAsync(data, "owner", originalInput: WorkflowFixture.Narrative);
        Assert.Equal(WorkflowFixture.Narrative, decision.OriginalInput);
        Assert.Equal(WorkflowFixture.Narrative, decision.GetCurrentVersion()!.NaturalLanguageInput);
        Assert.Equal("110000", decision.GetCurrentVersion()!.StructuredData.Get("budget"));
    }

    [Fact]
    public async Task PreviewHonorsExplicitLocksAndCancellation()
    {
        var review = await WorkflowFixture.Review().PreviewAsync(WorkflowFixture.Narrative, new[] { "budget", "timeline_months", "scope" });
        Assert.Equal("budget, timeline_months, scope", review.Data.Get("fixed_fields"));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => WorkflowFixture.Review().PreviewAsync(WorkflowFixture.Narrative, cancellationToken: cancellation.Token));
    }
}
