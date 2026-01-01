namespace DecisionReplay.API.DTOs;

public record DecisionResponse(
    string Id,
    string Title,
    string Scope,
    string Timeline,
    string Resources,
    string Constraints,
    string Status,              // DRAFT, IN_REVIEW, FINALIZED
    string Outcome,             // DRAFT, FEASIBLE, RISKY_BUT_POSSIBLE, NEEDS_ADJUSTMENT, COMMITTED
    double FeasibilityScore,    // 0-100 AI-generated score
    DateTime CreatedAt,
    string CreatedBy
);
