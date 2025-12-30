namespace DecisionReplay.API.DTOs;

public record DecisionEventResponse(
    string EventType,
    DateTime Timestamp,
    object Payload
);
