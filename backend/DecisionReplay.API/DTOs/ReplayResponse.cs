namespace DecisionReplay.API.DTOs;

public record ReplayResponse(
    string DecisionId,
    IEnumerable<DecisionEventResponse> Events
);
