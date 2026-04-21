namespace DecisionReplay.Domain.ValueObjects;

/// <summary>
/// Deterministic project timeline produced by TimelineGenerator.
/// Independent of AI – always available.
/// </summary>
public class ProjectPlan
{
    public double                       TotalMonths { get; init; }
    public IReadOnlyList<TimelinePhase> Phases      { get; init; } = new List<TimelinePhase>();
    public IReadOnlyList<string>        Milestones  { get; init; } = new List<string>();
    public DateTime                     GeneratedAt { get; init; } = DateTime.UtcNow;
}

public class TimelinePhase
{
    public string              Name             { get; init; } = "";
    public double              StartMonth       { get; init; }
    public double              EndMonth         { get; init; }
    public double              DurationMonths   { get; init; }
    public IReadOnlyList<string> Tasks          { get; init; } = new List<string>();
    public IReadOnlyList<string> Deliverables   { get; init; } = new List<string>();
    public double              PercentageOfTotal{ get; init; }
}
