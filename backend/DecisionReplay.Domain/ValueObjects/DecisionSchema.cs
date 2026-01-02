using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DecisionReplay.Domain.ValueObjects;

/// <summary>
/// Value Object: Dynamic schema generated per decision based on inferred domain
/// SOLID: Single Responsibility - Represents the structure of a decision
/// Design: Allows different domains (software, construction, logistics) without code changes
/// </summary>
[BsonIgnoreExtraElements]
public class DecisionSchema
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; private set; }

    [BsonElement("domainType")]
    public string DomainType { get; private set; } = string.Empty;

    [BsonElement("fields")]
    public Dictionary<string, string> Fields { get; private set; } = new();

    [BsonElement("generatedAt")]
    public DateTime GeneratedAt { get; private set; }

    private DecisionSchema() { }

    public DecisionSchema(string domainType, Dictionary<string, string> fields)
    {
        if (string.IsNullOrWhiteSpace(domainType))
            throw new ArgumentException("Domain type cannot be empty", nameof(domainType));

        Id = Guid.NewGuid();
        DomainType = domainType;
        Fields = fields ?? new Dictionary<string, string>();
        GeneratedAt = DateTime.UtcNow;
    }

    public bool HasField(string fieldName) => Fields.ContainsKey(fieldName);

    public string? GetFieldDescription(string fieldName)
    {
        return Fields.TryGetValue(fieldName, out var description) ? description : null;
    }
}
