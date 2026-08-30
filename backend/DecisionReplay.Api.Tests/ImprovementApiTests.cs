using DecisionReplay.API.DTOs;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Api.Tests;

public sealed class ImprovementApiTests(WorkflowApiFactory factory) : IClassFixture<WorkflowApiFactory>
{
    [Fact]
    public async Task AdoptedPlanPreservesMustHaveAndOriginalVersion()
    {
        using var client = factory.AuthenticatedClient();
        var decision = await ApiWorkflowData.Create(client);
        var accepted = await ApiWorkflowData.Success<ReplayDecisionResponse>(await client.PostAsJsonAsync($"/api/v2/decisions/{decision.DecisionId}/improvement/accept", new { sourceVersion = 1 }));
        Assert.Equal(2, accepted.Decision.Version);
        Assert.Contains("reporting", accepted.Decision.StructuredFields["scope"]);
        Assert.Equal("100000", accepted.Decision.StructuredFields["budget"]);
        Assert.Equal(decision.Plan!.EndDate, accepted.Decision.Plan!.EndDate);
        var history = await ApiWorkflowData.Success<List<DecisionVersion>>(await client.GetAsync($"/api/v2/decisions/{decision.DecisionId}/versions"));
        Assert.Equal(2, history.Count);
        Assert.Equal("login, payments, reporting", history[0].StructuredData.Get("scope"));
        Assert.Equal(history[1].StructuredData.Get("scope"), history[1].Context!.Fields["scope"]);
    }

    [Fact]
    public async Task DuplicateOrStaleRequestReturnsConflict()
    {
        using var client = factory.AuthenticatedClient();
        var decision = await ApiWorkflowData.Create(client);
        var path = $"/api/v2/decisions/{decision.DecisionId}/improvement/accept";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(path, new { sourceVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync(path, new { sourceVersion = 1 })).StatusCode);
    }

    [Fact]
    public async Task ClientCannotSubmitReplacementProposalFields()
    {
        using var client = factory.AuthenticatedClient();
        var decision = await ApiWorkflowData.Create(client);
        var accepted = await ApiWorkflowData.Success<ReplayDecisionResponse>(await client.PostAsJsonAsync($"/api/v2/decisions/{decision.DecisionId}/improvement/accept",
            new { sourceVersion = 1, proposedFields = new { budget = "99999999" } }));
        Assert.Equal("100000", accepted.Decision.StructuredFields["budget"]);
    }

    [Fact]
    public async Task RulesOnlyReviewAndAdoptionGenerateNoAiExecutions()
    {
        using var client = factory.AuthenticatedClient();
        await ApiWorkflowData.Success<RequirementsReview>(await client.PostAsJsonAsync("/api/v2/requirements/preview", new { input = ApiWorkflowData.Narrative }));
        var decision = await ApiWorkflowData.Create(client);
        await ApiWorkflowData.Success<ReplayDecisionResponse>(await client.PostAsJsonAsync($"/api/v2/decisions/{decision.DecisionId}/improvement/accept", new { sourceVersion = 1 }));
        var executions = await ApiWorkflowData.Success<List<AiExecution>>(await client.GetAsync("/api/ai/executions"));
        Assert.Empty(executions);
    }
}
