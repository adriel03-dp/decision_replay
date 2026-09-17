using System.Text.Json;
using DecisionReplay.Application.Services;

namespace DecisionReplay.Tests;

public sealed class AiOutputSchemaTests
{
    [Fact]
    public void SchemaLimitsCitationsToT0EvidenceRatherThanAssumptionIds()
    {
        using var schema = JsonDocument.Parse(AiOutputSchema.Analysis(AiTestHarness.Context()));
        var ids = schema.RootElement.GetProperty("$defs").GetProperty("evidenceIds").GetProperty("items").GetProperty("enum");
        Assert.Equal("e1", ids[0].GetString());
        Assert.Equal(1, ids.GetArrayLength());
    }

    [Fact]
    public void ContextWithoutEvidenceForbidsCitationEdges()
    {
        var context = AiTestHarness.Context(); context.Evidence.Clear();
        using var schema = JsonDocument.Parse(AiOutputSchema.Analysis(context));
        Assert.Equal(0, schema.RootElement.GetProperty("$defs").GetProperty("evidenceIds").GetProperty("maxItems").GetInt32());
    }

    [Fact]
    public async Task SchemaHashAndProviderNeutralContractAreRecorded()
    {
        var harness = new AiTestHarness();
        var schema = AiOutputSchema.Analysis(AiTestHarness.Context());
        var output = await harness.Workflow.GenerateAsync<DecisionReplay.Domain.ValueObjects.DecisionAiAnalysis>("decision-analysis", "v1",
            ReplayContextService.T0Input(AiTestHarness.Context()), result => AiOutputValidation.Analysis(result, AiTestHarness.Context()), outputSchema: schema);
        Assert.Equal(schema, harness.Provider.Requests.Single().JsonSchema);
        Assert.Equal(AiWorkflowService.Hash(schema), (await harness.Runs.GetExecutionAsync(output.ExecutionId, "test"))!.OutputSchemaHash);
    }
}
