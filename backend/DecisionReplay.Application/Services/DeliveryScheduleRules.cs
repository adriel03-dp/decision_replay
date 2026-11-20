using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class DeliveryScheduleRules
{
    public static DateTime EndDate(DateTime startDate, double timelineMonths)
    {
        // Whole months use calendar arithmetic; only a fractional month uses the average-day assumption.
        var wholeMonths = (int)Math.Floor(timelineMonths);
        var fractionalDays = (int)Math.Round((timelineMonths - wholeMonths) * 30.4375);
        var exclusiveEnd = startDate.AddMonths(wholeMonths).AddDays(fractionalDays);
        return exclusiveEnd > startDate ? exclusiveEnd.AddDays(-1) : startDate;
    }

    // Shared by scoring and the proposal guard. This is an effort heuristic, not a delivery guarantee.
    public static double EstimateMonths(StructuredDecisionData decision)
    {
        var team = DecisionValidationService.TryNumber(decision.Get("team_size"), out var suppliedTeam) ? Math.Max(1, suppliedTeam) : 1;
        var scope = Math.Max(1, DeliveryCostRules.SplitList(decision.Get("scope")).Length);
        var dependencies = DeliveryCostRules.SplitList(decision.Get("dependencies")).Length;
        return Math.Max(1, (scope * 1.25 + dependencies * .8) / Math.Sqrt(team));
    }
}
