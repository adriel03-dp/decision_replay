namespace DecisionReplay.Domain.ValueObjects;

public sealed class ContextStatement
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime KnownAt { get; set; }
}

public sealed class DecisionContextSnapshot
{
    public DateTime DecisionAt { get; set; } = DateTime.UtcNow;
    public string DecisionText { get; set; } = "";
    public string ChosenAction { get; set; } = "";
    public string ExpectedOutcome { get; set; } = "";
    public Dictionary<string, string> Fields { get; set; } = new();
    public List<ContextStatement> Evidence { get; set; } = new();
    public List<ContextStatement> Assumptions { get; set; } = new();
    public List<ContextStatement> Constraints { get; set; } = new();
    public List<string> Alternatives { get; set; } = new();
    public string Provenance { get; set; } = "user-attested";
}

public sealed class RecordedOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int DecisionVersion { get; set; }
    public DateTime ObservedAt { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";
    public string OutcomeQuality { get; set; } = "unknown";
}

public sealed class ReplayAnalysisRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExecutionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DecisionAiAnalysis Analysis { get; set; } = new();
}

public sealed class ReplayScenario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int BaseVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DecisionContextSnapshot Context { get; set; } = new();
    public Dictionary<string, string> FieldChanges { get; set; } = new();
    public Dictionary<string, string> AssumptionChanges { get; set; } = new();
    public Dictionary<string, string> ConstraintChanges { get; set; } = new();
    public FeasibilityAssessment Assessment { get; set; } = new();
    public ReplayComparison Comparison { get; set; } = new();
    public List<ReplayAnalysisRecord> Analyses { get; set; } = new();
}

public sealed class DecisionConflictException() : Exception("The decision changed during this request. Reload and retry.");
