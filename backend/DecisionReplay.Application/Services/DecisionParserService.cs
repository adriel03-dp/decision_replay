using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class DecisionParserService
{
    private readonly IAiLanguageService _languageService;
    private readonly DomainTemplateService _templates;

    public DecisionParserService(
        IAiLanguageService languageService,
        DomainTemplateService templates)
    {
        _languageService = languageService;
        _templates = templates;
    }

    public async Task<StructuredDecisionData> ParseAsync(
        string naturalLanguageInput,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageInput))
            throw new ArgumentException("Decision input cannot be empty.", nameof(naturalLanguageInput));

        var parsed = await _languageService.ExtractDecisionAsync(
            naturalLanguageInput.Trim(),
            _templates.GetAll(),
            cancellationToken);

        if (!_templates.IsSupported(parsed.Domain))
            parsed.Domain = DetectDomain(naturalLanguageInput);

        parsed.Title = string.IsNullOrWhiteSpace(parsed.Title)
            ? BuildTitle(naturalLanguageInput)
            : parsed.Title.Trim();
        parsed.Goal = string.IsNullOrWhiteSpace(parsed.Goal)
            ? naturalLanguageInput.Trim()
            : parsed.Goal.Trim();
        parsed.Fields = new Dictionary<string, string>(
            parsed.Fields
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value?.Trim() ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        return parsed;
    }

    private static string DetectDomain(string input)
    {
        var text = input.ToLowerInvariant();
        if (ContainsAny(text, "startup", "start a business", "new business", "company"))
            return "business_startup";
        if (ContainsAny(text, "career", "job", "role", "resign", "promotion"))
            return "career_decision";
        if (ContainsAny(text, "degree", "course", "university", "study", "education"))
            return "education_path";
        if (ContainsAny(text, "launch", "product", "mvp", "customers"))
            return "product_launch";
        return "project_planning";
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(text.Contains);

    private static string BuildTitle(string input)
    {
        var singleLine = input.ReplaceLineEndings(" ").Trim();
        return singleLine.Length <= 80 ? singleLine : singleLine[..77] + "...";
    }
}
