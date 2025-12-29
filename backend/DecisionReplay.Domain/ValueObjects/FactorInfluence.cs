namespace DecisionReplay.Domain.ValueObjects;

public class FactorInfluence
{
    public required string Factor { get; init; }
    public required double Weight { get; init; }
}
