using DecisionReplay.Domain.Entities;
using DecisionReplay.API.DTOs;

namespace DecisionReplay.API.Mapping;

public static class DecisionMapping
{
    public static DecisionResponse ToResponse(this Decision decision)
        => new(
            decision.Id.ToString(),
            decision.Type,
            decision.Status.ToString(),
            decision.Outcome.ToString(),
            decision.CreatedAt
        );

    public static DecisionEventResponse ToResponse(this DecisionEvent e)
        => new(
            e.EventType.ToString(),
            e.Timestamp,
            e.Payload
        );
}
