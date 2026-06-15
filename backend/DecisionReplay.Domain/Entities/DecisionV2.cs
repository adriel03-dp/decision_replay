using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DecisionReplay.Domain.Entities;

[BsonIgnoreExtraElements]
public sealed class DecisionV2
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public string DomainType { get; set; } = string.Empty;
    public string OriginalInput { get; set; } = string.Empty;
    public int CurrentVersion { get; set; }
    public List<DecisionVersion> Versions { get; set; } = new();
    public List<AuditTrailEntry> AuditTrail { get; set; } = new();
    public DecisionStatus Status { get; set; } = DecisionStatus.Draft;
    public DecisionOutcome Outcome { get; set; } = DecisionOutcome.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }

    public DecisionV2()
    {
    }

    public DecisionV2(
        Guid id,
        string naturalLanguageInput,
        string createdBy,
        DecisionVersion initialVersion)
    {
        if (string.IsNullOrWhiteSpace(naturalLanguageInput))
            throw new ArgumentException("Decision input is required", nameof(naturalLanguageInput));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Creator is required", nameof(createdBy));
        if (initialVersion == null)
            throw new ArgumentNullException(nameof(initialVersion));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        OriginalInput = naturalLanguageInput.Trim();
        CreatedBy = createdBy;
        DomainType = initialVersion.StructuredData.Domain;
        CurrentVersion = initialVersion.Version;
        Versions = new List<DecisionVersion> { initialVersion };
        CreatedAt = DateTime.UtcNow;
        LastModifiedAt = CreatedAt;
        ApplyAssessmentState(initialVersion.Feasibility);
    }

    public DecisionVersion? GetCurrentVersion() =>
        Versions.OrderByDescending(version => version.Version).FirstOrDefault();

    public void AddVersion(DecisionVersion version)
    {
        if (version.Version != CurrentVersion + 1)
            throw new InvalidOperationException("Decision versions must be sequential");

        Versions.Add(version);
        CurrentVersion = version.Version;
        DomainType = version.StructuredData.Domain;
        LastModifiedAt = DateTime.UtcNow;
        ApplyAssessmentState(version.Feasibility);
    }

    private void ApplyAssessmentState(FeasibilityAssessment assessment)
    {
        Status = DecisionStatus.Analyzed;
        Outcome = assessment.FeasibilityScore switch
        {
            >= 75 => DecisionOutcome.Feasible,
            >= 55 => DecisionOutcome.RiskyButPossible,
            _ => DecisionOutcome.NeedsAdjustment
        };
    }
}
