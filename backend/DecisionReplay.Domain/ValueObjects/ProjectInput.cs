namespace DecisionReplay.Domain.ValueObjects;

/// <summary>
/// Structured input model for the hybrid decision engine.
/// Replaces unstructured free-text when users supply explicit parameters.
/// </summary>
public class ProjectInput
{
    public string ProjectType { get; }
    public IReadOnlyList<string> Features { get; }
    public decimal BudgetUsd { get; }
    public double TimelineMonths { get; }
    public int TeamSize { get; }
    public bool IsStructured { get; }
    public string? RawInput { get; }

    public ProjectInput(
        string projectType,
        IReadOnlyList<string> features,
        decimal budgetUsd,
        double timelineMonths,
        int teamSize,
        string? rawInput = null,
        bool isStructured = true)
    {
        ProjectType    = string.IsNullOrWhiteSpace(projectType) ? "General" : projectType.Trim();
        Features       = features ?? new List<string>();
        BudgetUsd      = budgetUsd >= 0 ? budgetUsd : 0;
        TimelineMonths = timelineMonths > 0 ? timelineMonths : 1;
        TeamSize       = teamSize >= 1 ? teamSize : 1;
        RawInput       = rawInput;
        IsStructured   = isStructured;
    }
}
