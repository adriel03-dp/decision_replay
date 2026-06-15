namespace DecisionReplay.Domain.Enums;

public enum DecisionRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum DecisionFieldType
{
    Text,
    Number,
    Currency,
    Integer,
    Date,
    List,
    Level
}

public enum FactorConfidence
{
    Low,
    Medium,
    High
}

public enum AuditActionType
{
    DecisionCreated,
    InputParsed,
    ValidationCompleted,
    FeasibilityCalculated,
    ExplanationGenerated,
    PlanGenerated,
    ReplayCreated,
    PlanExported
}
