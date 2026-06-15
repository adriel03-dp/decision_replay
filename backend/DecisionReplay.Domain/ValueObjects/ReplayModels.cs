namespace DecisionReplay.Domain.ValueObjects;

public sealed class DecisionFieldChange
{
    public string Field { get; set; } = string.Empty;
    public string? From { get; set; }
    public string? To { get; set; }
}

public sealed class PlanChange
{
    public string Type { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
}

public sealed class ReplayComparison
{
    public int PreviousVersion { get; set; }
    public int NewVersion { get; set; }
    public List<DecisionFieldChange> ChangedFields { get; set; } = new();
    public double ScoreDelta { get; set; }
    public string RiskDelta { get; set; } = string.Empty;
    public string MainReason { get; set; } = string.Empty;
    public string LanguageSummary { get; set; } = string.Empty;
    public List<PlanChange> PlanChanges { get; set; } = new();
    public DateTime ComparedAt { get; set; } = DateTime.UtcNow;
}
