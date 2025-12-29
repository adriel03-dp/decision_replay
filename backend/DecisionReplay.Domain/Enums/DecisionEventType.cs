namespace DecisionReplay.Domain.Enums;

public enum DecisionEventType
{
    InputCaptured,
    RuleEvaluated,
    AIReasoningGenerated,
    HumanOverrideApplied,
    DecisionFinalized
}
