using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DecisionReplay.Application.Services;

/// <summary>Compatibility facade: shared prompts and validation, no provider-specific transport.</summary>
public sealed class AiLanguageService(AiWorkflowService workflow, IAiProviderResolver providers,
    ILogger<AiLanguageService> logger) : IAiLanguageService
{
    public bool IsConfigured => true; // Selected configuration is validated at startup.

    public async Task<StructuredDecisionData> ExtractDecisionAsync(string input,
        IReadOnlyCollection<DecisionDomainTemplate> templates, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await workflow.GenerateAsync<ExtractionOutput>("extraction", "v1", new
            {
                templates = templates.Select(template => new
                {
                    template.Domain,
                    fields = template.Fields.Select(field => new { field.Name, field.Description, type = field.Type.ToString(), field.Required })
                }),
                userInput = input
            }, output =>
            {
                var template = templates.FirstOrDefault(template => template.Domain == output.Domain);
                if (template == null || !AiOutputValidation.Text(output.Title, 200) || !AiOutputValidation.Text(output.Goal, 20000)
                    || output.Fields == null || output.Fields.Count > 100 || output.Fields.Any(pair =>
                        !template.Fields.Any(field => field.Name == pair.Key) || !AiOutputValidation.Text(pair.Value, 20000))
                    || !AiOutputValidation.Strings(output.Assumptions) || !AiOutputValidation.Strings(output.MissingFields)
                    || output.MissingFields.Any(name => !template.Fields.Any(field => field.Name == name)))
                    return "Extraction contains invalid fields or an unsupported domain.";
                return null;
            }, cancellationToken: cancellationToken);
            var parsed = result.Value;
            return new() { Domain = parsed.Domain, Title = parsed.Title, Goal = parsed.Goal, Fields = parsed.Fields,
                Assumptions = parsed.Assumptions, MissingFields = parsed.MissingFields };
        }
        catch (AiException ex) when (providers.DeterministicFallback)
        {
            logger.LogWarning("Structured extraction used deterministic fallback. ErrorCode={ErrorCode}", ex.Code);
            return ExtractWithFallback(input);
        }
    }

    public async Task<string> ExplainResultAsync(StructuredDecisionData decision, FeasibilityAssessment assessment,
        CancellationToken cancellationToken = default) => await TextAsync("explanation", new { decision, assessment }, cancellationToken);

    public async Task<string> SummarizeReplayAsync(ReplayComparison comparison,
        CancellationToken cancellationToken = default) => await TextAsync("replay-summary", comparison, cancellationToken);

    private async Task<string> TextAsync(string operation, object input, CancellationToken token)
    {
        try
        {
            return (await workflow.GenerateAsync<TextOutput>(operation, "v1", input,
                output => AiOutputValidation.Text(output.Text, 16000) ? null : "Text output is empty or too long.", cancellationToken: token)).Value.Text;
        }
        catch (AiException ex) when (providers.DeterministicFallback)
        {
            logger.LogWarning("Optional {Operation} used deterministic wording. ErrorCode={ErrorCode}", operation, ex.Code);
            return "";
        }
    }

    public async Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(StructuredDecisionData decision,
        FeasibilityAssessment assessment, ActionPlan plan, CancellationToken cancellationToken = default)
    {
        var tasks = plan.Phases.SelectMany(phase => phase.Tasks).ToList();
        try
        {
            var result = await workflow.GenerateAsync<PlanOutput>("plan-enhancement", "v1", new { decision, assessment, plan }, output =>
            {
                if (output.Tasks == null || output.Tasks.Count != tasks.Count || output.Tasks.Any(task => task == null
                    || !tasks.Any(source => source.TaskId == task.TaskId) || !AiOutputValidation.Text(task.Description, 450)
                    || !AiOutputValidation.Text(task.RiskMitigation, 450)) || output.Tasks.Select(task => task.TaskId).Distinct().Count() != tasks.Count)
                    return "Plan enhancement must contain every original task ID exactly once with bounded wording.";
                return null;
            }, cancellationToken: cancellationToken);
            return result.Value.Tasks;
        }
        catch (AiException ex) when (providers.DeterministicFallback)
        {
            logger.LogWarning("Plan enhancement retained original wording. ErrorCode={ErrorCode}", ex.Code);
            return Array.Empty<PlanTaskEnhancement>();
        }
    }

    private static StructuredDecisionData ExtractWithFallback(string input)
    {
        var text = input.ToLowerInvariant();
        var domain = Regex.IsMatch(text, @"\b(startup|start a business|new business|company)\b") ? "business_startup"
            : Regex.IsMatch(text, @"\b(career|job|role|resign|promotion)\b") ? "career_decision"
            : Regex.IsMatch(text, @"\b(degree|course|university|study|education)\b") ? "education_path"
            : Regex.IsMatch(text, @"\b(product launch|launch|mvp|customers?)\b") ? "product_launch" : "project_planning";
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, pattern) in new[]
        {
            ("budget", @"(?:budget|cost|funding)[^\d]{0,20}\$?\s*([\d,]+(?:\.\d+)?)"),
            ("timeline_months", @"(\d+(?:\.\d+)?)\s*(?:months?|mos?)"),
            ("team_size", @"(?:team|staff|people|developers?)[^\d]{0,15}(\d+)"),
            ("weekly_hours", @"(\d+(?:\.\d+)?)\s*(?:hours?|hrs?)\s*(?:per|a)?\s*week")
        })
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            if (match.Success) fields[name] = match.Groups[1].Value.Replace(",", "");
        }
        var goalField = domain switch { "project_planning" => "project_goal", "product_launch" => "product_goal", "business_startup" => "business_idea", "education_path" => "program", _ => null };
        if (goalField != null) fields[goalField] = input.Trim();
        var singleLine = input.ReplaceLineEndings(" ").Trim();
        return new() { Domain = domain, Title = singleLine[..Math.Min(80, singleLine.Length)], Goal = input.Trim(), Fields = fields,
            Assumptions = new() { "AI extraction failed; a deterministic parser extracted only explicit values and the stated goal. See AI execution metadata." } };
    }

    private sealed class ExtractionOutput
    {
        [JsonRequired] public string Domain { get; set; } = "";
        [JsonRequired] public string Title { get; set; } = "";
        [JsonRequired] public string Goal { get; set; } = "";
        [JsonRequired] public Dictionary<string, string> Fields { get; set; } = new();
        [JsonRequired] public List<string> Assumptions { get; set; } = new();
        [JsonRequired] public List<string> MissingFields { get; set; } = new();
    }
    private sealed class TextOutput { [JsonRequired] public string Text { get; set; } = ""; }
    private sealed class PlanOutput { [JsonRequired] public List<PlanTaskEnhancement> Tasks { get; set; } = new(); }
}
