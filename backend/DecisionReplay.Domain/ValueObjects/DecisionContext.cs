namespace DecisionReplay.Domain.ValueObjects;

/// <summary>
/// Value Object: Represents flexible decision metadata
/// SOLID: Single Responsibility - Only encapsulates decision context data
/// Clean Architecture: Pure domain object with no external dependencies
/// </summary>
public class DecisionContext
{
    public string NaturalLanguageInput { get; private set; } = string.Empty;
    public Dictionary<string, object> InferredAttributes { get; private set; } = new();
    public DateTime CapturedAt { get; private set; }

    private DecisionContext() { }

    public DecisionContext(string naturalLanguageInput, Dictionary<string, object> inferredAttributes)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageInput))
            throw new ArgumentException("Natural language input cannot be empty", nameof(naturalLanguageInput));

        NaturalLanguageInput = naturalLanguageInput;
        InferredAttributes = inferredAttributes ?? new Dictionary<string, object>();
        CapturedAt = DateTime.UtcNow;
    }

    public T? GetAttribute<T>(string key)
    {
        if (InferredAttributes.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public void AddOrUpdateAttribute(string key, object value)
    {
        InferredAttributes[key] = value;
    }
}
