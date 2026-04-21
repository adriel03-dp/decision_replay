using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Generates a deterministic phased project timeline based on the
/// feasibility result and project input. No AI dependency.
/// </summary>
public class TimelineGenerator : ITimelineGenerator
{
    public ProjectPlan Generate(ProjectInput input, FeasibilityResult feasibility)
    {
        var totalMonths = Math.Max(feasibility.EstimatedMonths, input.TimelineMonths);
        var phases = BuildPhases(input, feasibility, totalMonths);
        var milestones = BuildMilestones(phases, totalMonths);

        return new ProjectPlan
        {
            TotalMonths = Math.Round(totalMonths, 1),
            Phases      = phases,
            Milestones  = milestones,
        };
    }

    // ── Phase construction ────────────────────────────────────────────────

    private static List<TimelinePhase> BuildPhases(ProjectInput input, FeasibilityResult feasibility, double totalMonths)
    {
        // Phase percentage allocations (sum = 100%)
        //   Planning/Architecture, Initial Dev, Core Features, Testing & QA, Deployment
        double[] percentages = totalMonths switch
        {
            <= 1   => new[] { 0.10, 0.35, 0.35, 0.10, 0.10 },
            <= 3   => new[] { 0.12, 0.28, 0.38, 0.14, 0.08 },
            <= 6   => new[] { 0.15, 0.20, 0.40, 0.17, 0.08 },
            _      => new[] { 0.15, 0.18, 0.42, 0.18, 0.07 },
        };

        // Increase planning/architecture share for high complexity
        if (feasibility.Score < 60)
        {
            percentages[0] += 0.05;
            percentages[2] -= 0.05;
        }

        var phaseTemplates = new[]
        {
            PhaseTemplate("Planning & Architecture",
                tasks: new[] { "Requirements gathering", "Architecture design", "Tech stack selection", "Risk assessment" },
                deliverables: new[] { "Technical spec", "Architecture diagram", "Project roadmap" }),

            PhaseTemplate("Initial Development",
                tasks: new[] { "Environment setup", "CI/CD pipeline", "Core infrastructure", "Database schema" },
                deliverables: new[] { "Dev environment", "Base project scaffold", "Database setup" }),

            PhaseTemplate("Core Feature Development",
                tasks: BuildFeatureTasks(input.Features).ToArray(),
                deliverables: BuildFeatureDeliverables(input.Features).ToArray()),

            PhaseTemplate("Testing & Quality Assurance",
                tasks: new[] { "Unit tests", "Integration tests", "Performance testing", "Bug fixes", "Code review" },
                deliverables: new[] { "Test suite", "Bug report", "Performance benchmarks" }),

            PhaseTemplate("Deployment & Launch",
                tasks: new[] { "Production deployment", "Monitoring setup", "Documentation", "Stakeholder demo" },
                deliverables: new[] { "Live application", "Monitoring dashboard", "User documentation" }),
        };

        var phases = new List<TimelinePhase>();
        double cursor = 0;

        for (int i = 0; i < phaseTemplates.Length; i++)
        {
            var pct      = percentages[i];
            var duration = Math.Max(0.1, Math.Round(totalMonths * pct, 1));
            var template = phaseTemplates[i];

            phases.Add(new TimelinePhase
            {
                Name              = template.Name,
                StartMonth        = Math.Round(cursor, 1),
                EndMonth          = Math.Round(cursor + duration, 1),
                DurationMonths    = duration,
                Tasks             = template.Tasks,
                Deliverables      = template.Deliverables,
                PercentageOfTotal = Math.Round(pct * 100, 1),
            });

            cursor += duration;
        }

        return phases;
    }

    private static (string Name, IReadOnlyList<string> Tasks, IReadOnlyList<string> Deliverables)
        PhaseTemplate(string name, string[] tasks, string[] deliverables) =>
            (name, tasks.ToList(), deliverables.ToList());

    private static IEnumerable<string> BuildFeatureTasks(IReadOnlyList<string> features)
    {
        yield return "Implement core data models";
        foreach (var f in features.Where(f => !string.IsNullOrWhiteSpace(f)).Take(6))
            yield return $"Build {f}";
        yield return "API integration & validation";
    }

    private static IEnumerable<string> BuildFeatureDeliverables(IReadOnlyList<string> features)
    {
        yield return "Working feature set";
        foreach (var f in features.Where(f => !string.IsNullOrWhiteSpace(f)).Take(4))
            yield return $"{f} module";
        yield return "API endpoints";
    }

    private static List<string> BuildMilestones(List<TimelinePhase> phases, double totalMonths)
    {
        var milestones = new List<string>();
        foreach (var p in phases)
            milestones.Add($"Month {p.EndMonth:F1}: {p.Name} complete");
        milestones.Add($"Month {totalMonths:F1}: Project launch");
        return milestones;
    }
}
