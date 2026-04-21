using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

public interface ITimelineGenerator
{
    ProjectPlan Generate(ProjectInput input, FeasibilityResult feasibility);
}
