using System.Text.Json;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.AI;

public sealed class EmbeddedPromptCatalog : IPromptCatalog
{
    private readonly Dictionary<string, PromptTemplate> _prompts = new(StringComparer.Ordinal);
    public EmbeddedPromptCatalog()
    {
        var assembly = typeof(EmbeddedPromptCatalog).Assembly;
        foreach (var name in assembly.GetManifestResourceNames().Where(name => name.Contains(".Prompts.") && name.EndsWith(".json")))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var item = JsonSerializer.Deserialize<PromptFile>(json, AiWorkflowService.Json)
                ?? throw new InvalidOperationException("Prompt file is empty.");
            if (!item.User.Contains("{{context}}", StringComparison.Ordinal)) throw new InvalidOperationException("Prompt lacks its context placeholder.");
            _prompts.Add($"{item.Operation}/{item.Version}", new(item.Operation, item.Version, item.System, item.User, AiWorkflowService.Hash(json)));
        }
    }
    public PromptTemplate Resolve(string operation, string version) => _prompts.TryGetValue($"{operation}/{version}", out var prompt)
        ? prompt : throw new ArgumentException("Requested prompt operation/version does not exist.");
    public IReadOnlyList<PromptTemplate> List() => _prompts.Values.OrderBy(prompt => prompt.Operation).ThenBy(prompt => prompt.Version).ToList();
    private sealed record PromptFile(string Operation, string Version, string System, string User);
}
