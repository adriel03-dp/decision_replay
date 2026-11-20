using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class ScopePriorityRules
{
    public static string? Validate(StructuredDecisionData data)
    {
        var scope = DeliveryCostRules.SplitList(data.Get("scope"));
        var required = DeliveryCostRules.SplitList(data.Get("required_scope"));
        return required.Any(item => !scope.Contains(item, StringComparer.OrdinalIgnoreCase))
            ? "Every required_scope item must also appear in scope. Correct the scope or explicitly revise the must-have selection." : null;
    }

    public static bool PreservesRequiredScope(StructuredDecisionData original, StructuredDecisionData proposed) =>
        DeliveryCostRules.SplitList(original.Get("required_scope")).All(item =>
            DeliveryCostRules.SplitList(proposed.Get("scope")).Contains(item, StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<string> ReducedScopes(StructuredDecisionData original)
    {
        var scope = DeliveryCostRules.SplitList(original.Get("scope")).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var required = DeliveryCostRules.SplitList(original.Get("required_scope"));
        var optional = scope.Where(item => !required.Contains(item, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (scope.Length < 2 || optional.Length == 0) return Array.Empty<string>();
        var keep = required.Concat(optional.Take((int)Math.Ceiling(optional.Length / 2d))).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<string>();
        Add(keep);
        if (required.Length > 0) Add(required.ToHashSet(StringComparer.OrdinalIgnoreCase));
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        void Add(HashSet<string> retained)
        {
            var selected = scope.Where(retained.Contains).ToArray();
            if (selected.Length > 0 && selected.Length < scope.Length) candidates.Add(string.Join(", ", selected));
        }
    }
}
