using System.Globalization;
using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class DecisionValidationService
{
    public DecisionValidationResult Validate(
        StructuredDecisionData decision,
        DecisionDomainTemplate template)
    {
        var result = new DecisionValidationResult();

        if (!string.Equals(decision.Domain, template.Domain, StringComparison.OrdinalIgnoreCase))
            result.Errors.Add($"Domain '{decision.Domain}' does not match template '{template.Domain}'.");

        foreach (var unknown in decision.Fields.Keys.Where(key => !template.Fields.Any(field => field.Name.Equals(key, StringComparison.OrdinalIgnoreCase))))
            result.Errors.Add($"Unknown field '{unknown}' for domain '{template.Domain}'.");
        if (template.Fields.Any(field => field.Name == "fixed_fields"))
        {
            try { ImprovementConstraintPolicy.Resolve(decision, template); }
            catch (ArgumentException error) { result.Errors.Add(error.Message); }
        }

        foreach (var field in template.Fields)
        {
            var value = decision.Get(field.Name);
            if (field.Required && string.IsNullOrWhiteSpace(value))
            {
                result.MissingFields.Add(field.Name);
                continue;
            }

            if (string.IsNullOrWhiteSpace(value)) continue;
            if (field.Name == "scope" && DeliveryCostRules.SplitList(value).Length == 0)
            {
                HandleInvalidFieldValue(result, field);
                continue;
            }

            if (field.Type == DecisionFieldType.Level && !IsRecognizedLevel(value))
            {
                HandleInvalidFieldValue(result, field);
                result.Warnings.Add(
                    $"Field '{field.Name}' used a non-standard level value and will be scored conservatively.");
                continue;
            }

            if (!IsValidType(value, field.Type))
            {
                HandleInvalidFieldValue(result, field);
                continue;
            }

            if (field.Type is DecisionFieldType.Number or DecisionFieldType.Currency or DecisionFieldType.Integer
                && TryNumber(value, out var numeric) && (numeric < 0 || numeric == 0 && field.Name is "timeline_months" or "team_size" or "monthly_cost_per_person"
                    || field.Name == "timeline_months" && numeric > 120 || field.Name == "contingency_percent" && numeric > 100))
            {
                HandleInvalidFieldValue(result, field);
                continue;
            }
            if (field.Name == "currency" && !System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Za-z]{3}$"))
            {
                result.Errors.Add("Currency must be a three-letter code. No automatic exchange conversion is performed.");
                continue;
            }

            if (field.ValidationPattern != null &&
                !System.Text.RegularExpressions.Regex.IsMatch(value, field.ValidationPattern))
            {
                HandleInvalidFieldValue(result, field);
                continue;
            }
        }

        // Missing-field authority belongs to the template, not a model's claims.
        result.MissingFields = result.MissingFields.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (result.MissingFields.Count > 0)
            result.Warnings.Add("Missing fields are scored conservatively using documented assumptions.");

        return result;
    }

    private static void HandleInvalidFieldValue(
        DecisionValidationResult result,
        DecisionFieldDefinition field)
    {
        result.InvalidFields.Add(field.Name);
        if (field.Required)
            result.MissingFields.Add(field.Name);

        result.Warnings.Add(
            $"Field '{field.Name}' could not be confidently interpreted and will be treated as an improvement gap.");
    }

    private static bool IsValidType(string value, DecisionFieldType type) =>
        type switch
        {
            DecisionFieldType.Number or DecisionFieldType.Currency =>
                TryNumber(value, out _),
            DecisionFieldType.Integer =>
                TryNumber(value, out var number) && Math.Abs(number - Math.Round(number)) < .001,
            DecisionFieldType.Date =>
                DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _),
            DecisionFieldType.List =>
                value.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length > 0,
            DecisionFieldType.Level =>
                IsRecognizedLevel(value),
            _ => !string.IsNullOrWhiteSpace(value)
        };

    private static bool IsRecognizedLevel(string value) => TryLevelScore(value, out _);

    internal static bool TryLevelScore(string? value, out double score)
    {
        score = 0;
        if (TryNumber(value, out var numeric))
        { score = numeric <= 1 ? numeric * 100 : numeric; return numeric is >= 0 and <= 100; }
        score = value?.Trim().ToLowerInvariant() switch
        {
            "very high" or "ready" or "strong" => 90,
            "high" or "good" => 80,
            "medium" or "partial" or "moderate" => 60,
            "low" or "weak" => 30,
            "none" or "unknown" or "unclear" or "unvalidated" or "not validated" or "not ready" => 15,
            _ => -1
        };
        return score >= 0;
    }

    internal static bool TryNumber(string? value, out double number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var match = System.Text.RegularExpressions.Regex.Match(value,
            @"^\s*(?:(?:USD|LKR|EUR|GBP|Rs\.?)\s*|[$€£]\s*)?([+-]?(?:\d{1,3}(?:,\d{3})+|\d+)(?:\.\d+)?)(?:\s*(?:USD|LKR|EUR|GBP))?\s*$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
        return match.Success && double.TryParse(match.Groups[1].Value.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);
    }
}
