using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class PlanReplayComparisonService
{
    private readonly ReplayComparisonService _replayComparison;

    public PlanReplayComparisonService(ReplayComparisonService replayComparison)
    {
        _replayComparison = replayComparison;
    }

    public IReadOnlyList<PlanChange> Compare(
        DecisionVersion previous,
        DecisionVersion current,
        DecisionDomainTemplate template) =>
        _replayComparison.Compare(previous, current, template).PlanChanges;
}
