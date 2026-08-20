namespace DecisionReplay.Domain.ValueObjects;

public sealed record RequirementField(string Name, string Description, string Type, bool Required);
public sealed record RequirementDomain(string Domain, string DisplayName, string? GoalField,
    IReadOnlyList<RequirementField> Fields, bool SupportsImprovement);
public sealed record RequirementsReview(StructuredDecisionData Data, DecisionValidationResult Validation)
{
    public bool CanSubmit => Data.Goal.Trim().Length >= 10 && Validation.Errors.Count == 0 && Validation.InvalidFields.Count == 0;
}
