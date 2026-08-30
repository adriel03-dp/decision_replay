using DecisionReplay.API.DTOs;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Api.Tests;

public sealed class RequirementsApiTests(WorkflowApiFactory factory) : IClassFixture<WorkflowApiFactory>
{
    [Fact]
    public async Task DomainSchemaIsPublicButDraftExtractionRequiresAuthentication()
    {
        using var client = factory.CreateClient();
        var domains = await ApiWorkflowData.Success<List<RequirementDomain>>(await client.GetAsync("/api/v2/requirements/domains"));
        Assert.Equal(5, domains.Count);
        Assert.Contains(domains.Single(domain => domain.Domain == "project_planning").Fields, field => field.Name == "required_scope" && field.Type == "List");
        var unauthorized = await client.PostAsJsonAsync("/api/v2/requirements/preview", new { input = ApiWorkflowData.Narrative });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task PreviewAndCorrectionDoNotSaveAnAssessment()
    {
        using var client = factory.AuthenticatedClient();
        var preview = await ApiWorkflowData.Success<RequirementsReview>(await client.PostAsJsonAsync("/api/v2/requirements/preview", new { input = ApiWorkflowData.Narrative }));
        Assert.Equal("100000", preview.Data.Get("budget"));
        var invalid = ApiWorkflowData.Request(); invalid.Fields["timeline_months"] = "first semester";
        var checkedDraft = await ApiWorkflowData.Success<RequirementsReview>(await client.PostAsJsonAsync("/api/v2/requirements/validate", invalid));
        Assert.False(checkedDraft.CanSubmit);
        var decisions = await ApiWorkflowData.Success<List<DecisionEngineSummaryResponse>>(await client.GetAsync("/api/v2/decisions"));
        Assert.Empty(decisions);
    }

    [Fact]
    public async Task ConfirmedCorrectionIsUsedInsteadOfReextractingTheOriginalNarrative()
    {
        using var client = factory.AuthenticatedClient();
        var corrected = ApiWorkflowData.Request(); corrected.Fields["budget"] = "110000";
        var check = await ApiWorkflowData.Success<RequirementsReview>(await client.PostAsJsonAsync("/api/v2/requirements/validate", corrected));
        Assert.True(check.CanSubmit);
        var decision = await ApiWorkflowData.Success<DecisionEngineResponse>(await client.PostAsJsonAsync("/api/v2/decisions/analyze-structured", corrected));
        Assert.Equal("110000", decision.StructuredFields["budget"]);
        Assert.Equal(ApiWorkflowData.Narrative, decision.NaturalLanguageInput);
    }

    [Fact]
    public async Task UnknownMustHaveIsReturnedAsAReviewError()
    {
        using var client = factory.AuthenticatedClient();
        var request = ApiWorkflowData.Request(); request.Fields["required_scope"] = "unlisted feature";
        var check = await ApiWorkflowData.Success<RequirementsReview>(await client.PostAsJsonAsync("/api/v2/requirements/validate", request));
        Assert.False(check.CanSubmit);
        Assert.Contains(check.Validation.Errors, error => error.Contains("required_scope"));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v2/decisions/analyze-structured", request)).StatusCode);
    }
}
