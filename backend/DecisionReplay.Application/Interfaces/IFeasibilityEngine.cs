using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Interfaces;

public interface IFeasibilityEngine
{
    FeasibilityResult Evaluate(ProjectInput input);
    double CalculateComplexityScore(IEnumerable<string> features, string projectType);
    decimal EstimateCost(ProjectInput input, double complexityScore);
    double EstimateMonths(ProjectInput input, double complexityScore);
}
