using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class ActionPlanService
{
    private readonly PlanPhaseBuilderService _phaseBuilder;
    private readonly IAiLanguageService _languageService;

    public ActionPlanService(
        PlanPhaseBuilderService phaseBuilder,
        IAiLanguageService languageService)
    {
        _phaseBuilder = phaseBuilder;
        _languageService = languageService;
    }

    public async Task<ActionPlan> GenerateAsync(
        Guid decisionId,
        int version,
        StructuredDecisionData decision,
        DecisionDomainTemplate template,
        FeasibilityAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        var timelineMonths = NumberOr(decision, "timeline_months", 6);
        var startDate = ReadStartDate(decision);
        var phases = _phaseBuilder.Build(timelineMonths, startDate).ToList();
        var risksByFactor = assessment.Risks
            .GroupBy(risk => risk.Factor, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => string.Join(" ", group.Select(risk => risk.Message)));

        foreach (var phase in phases)
        {
            var templates = template.PlanTasks
                .Where(task => string.Equals(task.PhaseKey, phase.PhaseKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (templates.Count == 0)
                templates.Add(DefaultTask(phase));

            phase.Tasks = templates.Select((task, index) => new PlanTask
            {
                TaskName = task.TaskName,
                Description = task.Description,
                Priority = index == 0 ? "High" : "Medium",
                EstimatedEffort = phase.EndWeek - phase.StartWeek >= 6 ? "High" : "Medium",
                Dependencies = index == 0 || phases.IndexOf(phase) == 0
                    ? new List<string>()
                    : new List<string> { phases[phases.IndexOf(phase) - 1].PhaseName },
                RiskNotes = BuildRiskNote(phase, risksByFactor),
                SuccessCriteria = task.SuccessCriteria
            }).ToList();
        }

        var plan = new ActionPlan
        {
            DecisionId = decisionId,
            Version = version,
            TimelineMonths = timelineMonths,
            StartDate = startDate,
            EndDate = startDate.AddDays(Math.Max(1, (int)Math.Round(timelineMonths * 30.4375)) - 1),
            FeasibilityScore = assessment.FeasibilityScore,
            RiskLevel = assessment.RiskLevel.ToString(),
            Phases = phases,
            Milestones = phases.Select(phase => new PlanMilestone
            {
                Name = $"{phase.PhaseName} complete",
                TargetWeek = phase.EndWeek,
                TargetDate = phase.EndDate,
                SuccessCriteria = string.Join(
                    " ",
                    phase.Tasks.Select(task => task.SuccessCriteria).Where(value => !string.IsNullOrWhiteSpace(value)))
            }).ToList()
        };

        var enhancements = await _languageService.EnhancePlanTasksAsync(
            decision,
            assessment,
            plan,
            cancellationToken);
        ApplyEnhancements(plan, enhancements);
        ValidatePlan(plan);
        return plan;
    }

    public void ValidatePlan(ActionPlan plan)
    {
        if (plan.Phases.Count == 0)
            throw new InvalidOperationException("Action plan must contain at least one phase.");
        if (plan.EndDate < plan.StartDate)
            throw new InvalidOperationException("Action plan end date cannot precede its start date.");

        var maximumWeek = Math.Max(1, (int)Math.Ceiling(plan.TimelineMonths * 4.345));
        foreach (var phase in plan.Phases)
        {
            if (phase.StartWeek < 1 || phase.EndWeek < phase.StartWeek || phase.EndWeek > maximumWeek + 1)
                throw new InvalidOperationException($"Phase '{phase.PhaseName}' exceeds the selected timeline.");
            if (phase.Tasks.Count == 0)
                throw new InvalidOperationException($"Phase '{phase.PhaseName}' requires at least one task.");

            foreach (var task in phase.Tasks)
            {
                if (string.IsNullOrWhiteSpace(task.TaskName) ||
                    string.IsNullOrWhiteSpace(task.Description) ||
                    string.IsNullOrWhiteSpace(task.SuccessCriteria))
                    throw new InvalidOperationException("Every plan task requires a name, description, and success criteria.");
            }
        }
    }

    private static void ApplyEnhancements(
        ActionPlan plan,
        IReadOnlyCollection<PlanTaskEnhancement> enhancements)
    {
        var lookup = enhancements
            .Where(item => item.TaskId != Guid.Empty)
            .ToDictionary(item => item.TaskId);
        foreach (var task in plan.Phases.SelectMany(phase => phase.Tasks))
        {
            if (!lookup.TryGetValue(task.TaskId, out var enhancement)) continue;
            if (!string.IsNullOrWhiteSpace(enhancement.Description))
                task.Description = enhancement.Description.Trim();
            if (!string.IsNullOrWhiteSpace(enhancement.RiskMitigation))
                task.RiskNotes = enhancement.RiskMitigation.Trim();
        }
    }

    private static DomainPlanTaskTemplate DefaultTask(PlanPhase phase) =>
        new()
        {
            PhaseKey = phase.PhaseKey,
            TaskName = $"Complete {phase.PhaseName}",
            Description = phase.Goal,
            SuccessCriteria = $"{phase.PhaseName} exit criteria are documented and approved."
        };

    private static string BuildRiskNote(
        PlanPhase phase,
        IReadOnlyDictionary<string, string> risks)
    {
        if (risks.Count == 0) return "Monitor assumptions and escalate material deviations.";
        var selected = phase.PhaseKey switch
        {
            "validate" => risks.Where(item => item.Key.Contains("market") || item.Key.Contains("input")),
            "setup" => risks.Where(item => item.Key.Contains("budget") || item.Key.Contains("resource")),
            "execute" => risks.Where(item => item.Key.Contains("timeline") || item.Key.Contains("dependency")),
            _ => risks
        };
        var note = string.Join(" ", selected.Select(item => item.Value));
        return string.IsNullOrWhiteSpace(note)
            ? "Monitor assumptions and escalate material deviations."
            : note;
    }

    private static DateTime ReadStartDate(StructuredDecisionData decision)
    {
        var value = decision.Get("start_date");
        return DateTime.TryParse(value, out var parsed)
            ? parsed.Date
            : DateTime.UtcNow.Date;
    }

    private static double NumberOr(
        StructuredDecisionData decision,
        string field,
        double fallback) =>
        DecisionValidationService.TryNumber(decision.Get(field), out var value)
            ? Math.Clamp(value, .25, 120)
            : fallback;
}
