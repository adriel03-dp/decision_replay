namespace DecisionReplay.API.DTOs;

public record UpdateDecisionRequest(
    string? Status,
    string? Outcome,
    double? RiskScore
);
