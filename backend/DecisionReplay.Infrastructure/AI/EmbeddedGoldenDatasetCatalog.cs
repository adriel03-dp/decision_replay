using System.Text.Json;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.AI;

public sealed class EmbeddedGoldenDatasetCatalog : IGoldenDatasetCatalog
{
    public GoldenDataset Resolve(string version)
    {
        if (version != "v1") throw new ArgumentException("Unknown golden dataset version.");
        using var stream = typeof(EmbeddedGoldenDatasetCatalog).Assembly.GetManifestResourceStream(
            "DecisionReplay.Infrastructure.AI.Datasets.decisions.v1.json") ?? throw new InvalidOperationException("Golden dataset is missing.");
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        var dataset = JsonSerializer.Deserialize<GoldenDataset>(content, AiWorkflowService.Json)!;
        dataset.Hash = AiWorkflowService.Hash(content);
        if (dataset.Cases.Count is < 1 or > 20 || dataset.Cases.Select(item => item.Id).Distinct().Count() != dataset.Cases.Count)
            throw new InvalidOperationException("Golden dataset is invalid.");
        foreach (var item in dataset.Cases) ReplayContextService.Validate(item.Context);
        return dataset;
    }
}
