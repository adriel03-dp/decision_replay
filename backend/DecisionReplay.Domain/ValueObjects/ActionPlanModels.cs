namespace DecisionReplay.Domain.ValueObjects;

public sealed class ActionPlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();
    public Guid DecisionId { get; set; }
    public int Version { get; set; }
    public double TimelineMonths { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double FeasibilityScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<PlanPhase> Phases { get; set; } = new();
    public List<PlanMilestone> Milestones { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PlanPhase
{
    public string PhaseKey { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;
    public int StartWeek { get; set; }
    public int EndWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Goal { get; set; } = string.Empty;
    public List<PlanTask> Tasks { get; set; } = new();
}

public sealed class PlanTask
{
    public Guid TaskId { get; set; } = Guid.NewGuid();
    public string TaskName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string EstimatedEffort { get; set; } = "Medium";
    public List<string> Dependencies { get; set; } = new();
    public string RiskNotes { get; set; } = string.Empty;
    public string SuccessCriteria { get; set; } = string.Empty;
}

public sealed class PlanMilestone
{
    public string Name { get; set; } = string.Empty;
    public int TargetWeek { get; set; }
    public DateTime TargetDate { get; set; }
    public string SuccessCriteria { get; set; } = string.Empty;
}

public sealed class PlanTaskEnhancement
{
    public Guid TaskId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string RiskMitigation { get; set; } = string.Empty;
}
