namespace DecisionReplay.API.DTOs;

public record DecisionResponse(
    string Id,
    string Type,
    string Status,
    string CurrentOutcome,
    DateTime CreatedAt
);
