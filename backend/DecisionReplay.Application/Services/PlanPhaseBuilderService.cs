using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class PlanPhaseBuilderService
{
    public IReadOnlyList<PlanPhase> Build(double timelineMonths, DateTime startDate)
    {
        var totalWeeks = Math.Max(1, (int)Math.Round(timelineMonths * 4.345));
        var definitions = timelineMonths <= 3
            ? ShortPlan()
            : timelineMonths <= 8
                ? MediumPlan()
                : LongPlan();

        var phases = new List<PlanPhase>();
        var cursor = 1;
        for (var index = 0; index < definitions.Count; index++)
        {
            var definition = definitions[index];
            var remaining = totalWeeks - cursor + 1;
            var duration = index == definitions.Count - 1
                ? remaining
                : Math.Max(1, (int)Math.Round(totalWeeks * definition.Share));
            var endWeek = Math.Min(totalWeeks, cursor + duration - 1);

            phases.Add(new PlanPhase
            {
                PhaseKey = definition.Key,
                PhaseName = definition.Name,
                StartWeek = cursor,
                EndWeek = endWeek,
                StartDate = startDate.AddDays((cursor - 1) * 7),
                EndDate = startDate.AddDays(endWeek * 7 - 1),
                Goal = definition.Goal
            });

            cursor = endWeek + 1;
            if (cursor > totalWeeks) break;
        }

        return phases;
    }

    private static IReadOnlyList<PhaseDefinition> ShortPlan() =>
        new[]
        {
            new PhaseDefinition("validate", "Rapid validation", .25, "Validate the decision before major commitment."),
            new PhaseDefinition("setup", "Focused setup", .15, "Establish the minimum controls and resources required."),
            new PhaseDefinition("execute", "MVP execution", .40, "Deliver the smallest useful outcome."),
            new PhaseDefinition("launch", "Launch and review", .20, "Release, measure, and decide the next step.")
        };

    private static IReadOnlyList<PhaseDefinition> MediumPlan() =>
        new[]
        {
            new PhaseDefinition("validate", "Research and validation", .17, "Validate needs, constraints, and success thresholds."),
            new PhaseDefinition("setup", "Setup and planning", .17, "Baseline scope, owners, controls, and dependencies."),
            new PhaseDefinition("execute", "Execution", .33, "Deliver the prioritized core outcome."),
            new PhaseDefinition("optimize", "Testing and optimization", .17, "Test quality and correct material weaknesses."),
            new PhaseDefinition("launch", "Launch and review", .16, "Release, monitor, and review outcomes.")
        };

    private static IReadOnlyList<PhaseDefinition> LongPlan() =>
        new[]
        {
            new PhaseDefinition("validate", "Quarter 1 - Validate", .25, "Validate the decision and establish evidence."),
            new PhaseDefinition("setup", "Quarter 2 - Prepare", .25, "Build capacity, controls, and readiness."),
            new PhaseDefinition("execute", "Quarter 3 - Execute", .25, "Deliver and measure the core outcome."),
            new PhaseDefinition("launch", "Quarter 4 - Consolidate", .25, "Launch, optimize, and institutionalize the result.")
        };

    private sealed record PhaseDefinition(string Key, string Name, double Share, string Goal);
}
