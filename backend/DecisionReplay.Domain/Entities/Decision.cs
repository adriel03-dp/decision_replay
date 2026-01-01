using DecisionReplay.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace DecisionReplay.Domain.Entities;

public class Decision
{
    public Guid Id { get; private set; }
    public required string Title { get; init; }              // e.g., "Launch MVP Product"
    public required string Scope { get; init; }              // Features, deliverables
    public required string Timeline { get; init; }           // Target dates, milestones
    public required string Resources { get; init; }          // Team, budget, tools
    public required string Constraints { get; init; }        // Risks, dependencies, limitations
    public DecisionStatus Status { get; private set; }
    public DecisionOutcome Outcome { get; private set; }
    public double FeasibilityScore { get; private set; }     // 0-100 score from AI analysis
    public DateTime CreatedAt { get; private set; }
    public required string CreatedBy { get; init; }

    private Decision() { }

    [SetsRequiredMembers]
    public Decision(string title, string scope, string timeline, string resources, string constraints, string createdBy)
    {
        Id = Guid.NewGuid();
        Title = title;
        Scope = scope;
        Timeline = timeline;
        Resources = resources;
        Constraints = constraints;
        CreatedBy = createdBy;
        Status = DecisionStatus.Draft;
        Outcome = DecisionOutcome.Draft;
        CreatedAt = DateTime.UtcNow;
        FeasibilityScore = 0;
    }

    public void UpdateFeasibility(DecisionOutcome outcome, double feasibilityScore)
    {
        Outcome = outcome;
        FeasibilityScore = feasibilityScore;
    }

    public void FinalizeDecision()
    {
        Status = DecisionStatus.Finalized;
        Outcome = DecisionOutcome.Committed;
    }
}
