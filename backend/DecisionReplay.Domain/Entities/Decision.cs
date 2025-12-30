using DecisionReplay.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace DecisionReplay.Domain.Entities;

public class Decision
{
    public Guid Id { get; private set; }
    public required string Type { get; init; }
    public DecisionStatus Status { get; private set; }
    public DecisionOutcome Outcome { get; private set; }
    public double RiskScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public required string CreatedBy { get; init; }

    private Decision() { }

    [SetsRequiredMembers]
    public Decision(string type, string createdBy)
    {
        Id = Guid.NewGuid();
        Type = type;
        CreatedBy = createdBy;
        Status = DecisionStatus.Draft;
        Outcome = DecisionOutcome.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void FinalizeDecision(DecisionOutcome outcome, double riskScore)
    {
        Status = DecisionStatus.Finalized;
        Outcome = outcome;
        RiskScore = riskScore;
    }
}
