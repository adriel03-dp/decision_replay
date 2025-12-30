using DecisionReplay.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace DecisionReplay.Domain.Entities;

public class DecisionEvent
{
    public Guid Id { get; private set; }
    public Guid DecisionId { get; private set; }
    public DecisionEventType EventType { get; private set; }
    public DateTime Timestamp { get; private set; }
    public required object Payload { get; init; }

    private DecisionEvent() { }

    [SetsRequiredMembers]
    public DecisionEvent(
        Guid decisionId,
        DecisionEventType eventType,
        object payload)
    {
        Id = Guid.NewGuid();
        DecisionId = decisionId;
        EventType = eventType;
        Payload = payload;
        Timestamp = DateTime.UtcNow;
    }
}
