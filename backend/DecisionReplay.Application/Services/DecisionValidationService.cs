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

        foreach (var field in template.Fields)
        {
            var value = decision.Get(field.Name);
            if (field.Required && string.IsNullOrWhiteSpace(value))
            {
                result.MissingFields.Add(field.Name);
                continue;
            }

            if (string.IsNullOrWhiteSpace(value)) continue;

            if (!IsValidType(value, field.Type))
                result.Errors.Add($"Field '{field.Name}' must be a valid {field.Type.ToString().ToLowerInvariant()} value.");

            if (field.ValidationPattern != null &&
                !System.Text.RegularExpressions.Regex.IsMatch(value, field.ValidationPattern))
                result.Errors.Add($"Field '{field.Name}' does not match the required format.");
        }

        result.MissingFields = result.MissingFields
            .Concat(decision.MissingFields)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (result.MissingFields.Count > 0)
            result.Warnings.Add("Missing fields are scored conservatively using documented assumptions.");

        return result;
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
                TryNumber(value, out _) ||
                new[] { "none", "low", "medium", "high", "very high", "ready", "partial", "unknown" }
                    .Contains(value.Trim(), StringComparer.OrdinalIgnoreCase),
            _ => !string.IsNullOrWhiteSpace(value)
        };

    internal static bool TryNumber(string? value, out double number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var cleaned = new string(value.Where(character =>
            char.IsDigit(character) || character is '.' or '-' or ',').ToArray()).Replace(",", "");
        return double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }
}
