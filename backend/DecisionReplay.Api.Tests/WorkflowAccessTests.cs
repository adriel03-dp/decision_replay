using DecisionReplay.API.DTOs;

namespace DecisionReplay.Api.Tests;

public sealed class WorkflowAccessTests(WorkflowApiFactory factory) : IClassFixture<WorkflowApiFactory>
{
    [Fact]
    public async Task AnotherOwnerCannotReadOrAdoptThePlan()
    {
        using var owner = factory.AuthenticatedClient(); using var other = factory.AuthenticatedClient();
        var decision = await ApiWorkflowData.Create(owner);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v2/decisions/{decision.DecisionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PostAsJsonAsync($"/api/v2/decisions/{decision.DecisionId}/improvement/accept", new { sourceVersion = 1 })).StatusCode);
    }

    [Theory]
    [InlineData("pdf", "application/pdf")]
    [InlineData("excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task OriginalAndAdoptedPlansRemainExportable(string format, string contentType)
    {
        using var client = factory.AuthenticatedClient();
        var decision = await ApiWorkflowData.Create(client);
        var accepted = await ApiWorkflowData.Success<ReplayDecisionResponse>(await client.PostAsJsonAsync($"/api/v2/decisions/{decision.DecisionId}/improvement/accept", new { sourceVersion = 1 }));
        foreach (var plan in new[] { decision.Plan!, accepted.Decision.Plan! })
        {
            var response = await client.GetAsync($"/api/v2/decisions/{decision.DecisionId}/plans/{plan.PlanId}/export/{format}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(contentType, response.Content.Headers.ContentType!.MediaType);
            Assert.True((await response.Content.ReadAsByteArrayAsync()).Length > 100);
        }
    }
}
