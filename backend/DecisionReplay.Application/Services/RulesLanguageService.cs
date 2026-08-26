using System.Globalization;
using System.Text.RegularExpressions;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

/// <summary>Extracts explicit values only. Unsupported prose remains a visible information gap.</summary>
public sealed class RulesLanguageService : IAiLanguageService
{
    private const string Quantity = @"(?:\d[\d,]*(?:\.\d+)?|zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty)";
    private static readonly string[] Words = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty" };
    public bool IsConfigured => false;

    public Task<StructuredDecisionData> ExtractDecisionAsync(string input,
        IReadOnlyCollection<DecisionDomainTemplate> templates, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Decision input cannot be empty.");
        var domain = Match(input, @"\b(startup|start a business|new business|company)\b").Success ? "business_startup"
            : Match(input, @"\b(career|job|resign|promotion)\b").Success ? "career_decision"
            : Match(input, @"\b(degree|course|university|study|education)\b").Success ? "education_path"
            : Match(input, @"\b(product launch|launch a product|mvp)\b").Success ? "product_launch" : "project_planning";
        var explicitDomain = Match(input, @"(?:^|[;\r\n])\s*domain\s*[:=]\s*([a-z_]+)");
        if (explicitDomain.Success && templates.Any(t => t.Domain.Equals(explicitDomain.Groups[1].Value, StringComparison.OrdinalIgnoreCase)))
            domain = explicitDomain.Groups[1].Value.ToLowerInvariant();
        var template = templates.Single(t => t.Domain == domain);
        var result = new StructuredDecisionData { Domain = domain, Goal = input.Trim(),
            Title = input.ReplaceLineEndings(" ").Trim()[..Math.Min(80, input.ReplaceLineEndings(" ").Trim().Length)],
            Assumptions = new() { "Rules extracted explicit values only. Review the structured fields; unrecognized details are not inferred." } };

        Extract("budget", $@"\bbudget\s*(?:of|is|=|:)?\s*(?:USD|LKR|EUR|GBP|\$|€|£|Rs\.?)?\s*({Quantity})\s*([km])?\b");
        Extract("timeline_months", $@"\b({Quantity})\s*months?\b");
        Extract("team_size", $@"\b(?:team(?:\s+(?:size|of))?|staff|developers?)\s*(?:of|is|=|:)?\s*({Quantity})\b");
        if (!result.Fields.ContainsKey("team_size")) Extract("team_size", $@"\b({Quantity})\s*(?:people|developers|team members)\b");
        Extract("weekly_hours", $@"\b({Quantity})\s*(?:hours?|hrs?)\s*(?:per|a|/)\s*week\b");
        var currency = Match(input, @"\b(USD|LKR|EUR|GBP)\b");
        if (currency.Success && template.Fields.Any(f => f.Name == "currency")) result.Fields["currency"] = currency.Groups[1].Value.ToUpperInvariant();
        else if (input.Contains('$') && template.Fields.Any(f => f.Name == "currency")) result.Fields["currency"] = "USD";

        // Field labels are also a predictable, AI-free input format for all five domains.
        foreach (var field in template.Fields)
        {
            var label = Regex.Escape(field.Name).Replace("_", "[ _]");
            var match = Match(input, $@"(?:^|[;\r\n])\s*{label}\s*[:=]\s*([^;\r\n]+)");
            if (!match.Success) continue;
            var explicitValue = match.Groups[1].Value.Trim();
            if (field.Type is Domain.Enums.DecisionFieldType.Number or Domain.Enums.DecisionFieldType.Currency or Domain.Enums.DecisionFieldType.Integer)
            {
                var scalar = Match(explicitValue, $@"^({Quantity})\s*([km])?$");
                if (scalar.Success)
                {
                    var word = Array.IndexOf(Words, scalar.Groups[1].Value.ToLowerInvariant());
                    var number = word >= 0 ? word : double.Parse(scalar.Groups[1].Value.Replace(",", ""), CultureInfo.InvariantCulture);
                    if (field.Name == "budget") number *= scalar.Groups[2].Value.ToLowerInvariant() switch { "k" => 1_000, "m" => 1_000_000, _ => 1 };
                    else if (scalar.Groups[2].Success) { result.Fields[field.Name] = explicitValue; continue; }
                    explicitValue = number.ToString(CultureInfo.InvariantCulture);
                }
                else if (result.Fields.ContainsKey(field.Name)) continue;
            }
            result.Fields[field.Name] = explicitValue;
        }
        var goalField = domain switch { "project_planning" => "project_goal", "product_launch" => "product_goal", "business_startup" => "business_idea", "education_path" => "program", _ => null };
        if (goalField != null) result.Fields.TryAdd(goalField, input.Trim());
        return Task.FromResult(result);

        void Extract(string field, string pattern)
        {
            if (!template.Fields.Any(f => f.Name == field)) return;
            var match = Match(input, pattern);
            if (!match.Success) return;
            var text = match.Groups[1].Value.ToLowerInvariant();
            var word = Array.IndexOf(Words, text);
            var value = word >= 0 ? word : double.Parse(text.Replace(",", ""), CultureInfo.InvariantCulture);
            if (field == "budget" && match.Groups.Count > 2)
                value *= match.Groups[2].Value.ToLowerInvariant() switch { "k" => 1_000, "m" => 1_000_000, _ => 1 };
            result.Fields[field] = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static Match Match(string input, string pattern) =>
        Regex.Match(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));

    public Task<string> ExplainResultAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, CancellationToken cancellationToken = default) => Empty(cancellationToken);
    public Task<string> SummarizeReplayAsync(ReplayComparison comparison, CancellationToken cancellationToken = default) => Empty(cancellationToken);
    private static Task<string> Empty(CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.FromResult(""); }
    public Task<IReadOnlyList<PlanTaskEnhancement>> EnhancePlanTasksAsync(StructuredDecisionData decision, FeasibilityAssessment assessment, ActionPlan plan, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult<IReadOnlyList<PlanTaskEnhancement>>(Array.Empty<PlanTaskEnhancement>()); }
}
