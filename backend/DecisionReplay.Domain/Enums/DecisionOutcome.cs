namespace DecisionReplay.Domain.Enums;

public enum DecisionOutcome
{
    Draft,              // Still being planned
    Feasible,           // Plan looks realistic
    RiskyButPossible,   // High risk but achievable
    NeedsAdjustment,    // Significant issues identified
    Committed           // User has finalized and committed
}
