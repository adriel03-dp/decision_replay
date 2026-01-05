using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DecisionReplay.Domain.Entities;

/// <summary>
/// REFACTORED Decision Entity - Domain-Agnostic Design
/// 
/// SOLID Principles Applied:
/// - Single Responsibility: Manages decision lifecycle and state
/// - Open/Closed: Extensible via DecisionContext without modifying entity
/// - Dependency Inversion: Depends on value objects (abstractions), not concrete implementations
/// 
/// Clean Architecture:
/// - Pure domain entity with no infrastructure dependencies
/// - Business logic encapsulated within the entity
/// - Immutable where appropriate (Id, CreatedAt)
/// 
/// Domain-Agnostic Design:
/// - Uses natural language input instead of rigid fields
/// - DecisionContext holds flexible metadata inferred by AI
/// - DecisionSchema allows different domains without code changes
/// </summary>
[BsonIgnoreExtraElements]
public class DecisionV2
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    // Core decision data - domain-agnostic
    public DecisionContext Context { get; set; }
    public DecisionSchema? Schema { get; set; }
    public DecisionAnalysis? Analysis { get; set; }

    // Lifecycle
    public DecisionStatus Status { get; set; }
    public DecisionOutcome Outcome { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string CreatedBy { get; set; }

    // Domain classification (detected automatically from input)
    public string? DomainType { get; set; }

    // Public parameterless constructor for MongoDB
    public DecisionV2()
    {
        // MongoDB will set all properties via reflection
        // Don't initialize Context here - let MongoDB deserialize it
        Context = null!;
        CreatedBy = string.Empty;
    }

    [SetsRequiredMembers]
    public DecisionV2(DecisionContext context, string createdBy)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Creator must be specified", nameof(createdBy));

        Id = Guid.NewGuid();
        Context = context;
        CreatedBy = createdBy;
        Status = DecisionStatus.Draft;
        Outcome = DecisionOutcome.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Associates a dynamically generated schema with this decision
    /// </summary>
    public void AssignSchema(DecisionSchema schema)
    {
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Stores AI-generated analysis and updates status based on feasibility
    /// </summary>
    public void StoreAnalysis(DecisionAnalysis analysis)
    {
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        Status = DecisionStatus.Analyzed;

        // Set outcome based on feasibility score
        Outcome = analysis.FeasibilityScore switch
        {
            >= 70 => DecisionOutcome.Feasible,
            >= 50 => DecisionOutcome.RiskyButPossible,
            _ => DecisionOutcome.NeedsAdjustment
        };

        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates decision context (for replay scenarios)
    /// </summary>
    public void UpdateContext(DecisionContext newContext)
    {
        Context = newContext ?? throw new ArgumentNullException(nameof(newContext));
        LastModifiedAt = DateTime.UtcNow;
        // Reset analysis since context changed - must be re-evaluated
        Analysis = null;
    }

    /// <summary>
    /// Commits the decision
    /// </summary>
    public void CommitDecision()
    {
        if (Analysis == null)
            throw new InvalidOperationException("Cannot commit decision without analysis");

        Status = DecisionStatus.Finalized;
        Outcome = DecisionOutcome.Committed;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if decision needs re-analysis (for replay capability)
    /// </summary>
    public bool NeedsReAnalysis()
    {
        return Analysis == null ||
               (LastModifiedAt.HasValue && Analysis.GeneratedAt < LastModifiedAt.Value);
    }
}
