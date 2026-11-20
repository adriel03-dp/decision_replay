using System.Text.Json;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class ReplayContextService
{
    public static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, AiWorkflowService.Json), AiWorkflowService.Json)!;

    public static void Validate(DecisionContextSnapshot context)
    {
        if (context.DecisionAt.Kind != DateTimeKind.Utc || context.DecisionAt > DateTime.UtcNow
            || context.DecisionAt < DateTime.UnixEpoch) throw new ArgumentException("DecisionAt must be a UTC timestamp between 1970 and now.");
        if (!AiOutputValidation.Text(context.DecisionText, 20000) || !AiOutputValidation.Text(context.ChosenAction)
            || !AiOutputValidation.Text(context.ExpectedOutcome) || !AiOutputValidation.Strings(context.Alternatives, 20)
            || context.Fields == null || context.Fields.Count > 100 || context.Fields.Any(pair => !AiOutputValidation.Text(pair.Key, 100)
                || !AiOutputValidation.Text(pair.Value, 20000))) throw new ArgumentException("Decision context has invalid text, alternatives or fields.");
        if (context.Evidence == null || context.Assumptions == null || context.Constraints == null
            || context.Evidence.Count > 50 || context.Assumptions.Count > 50 || context.Constraints.Count > 50)
            throw new ArgumentException("Context statements exceed their limits.");
        var statements = context.Evidence.Concat(context.Assumptions).Concat(context.Constraints).ToList();
        if (statements.Any(item => item == null || !AiOutputValidation.Text(item.Id, 64)
            || !System.Text.RegularExpressions.Regex.IsMatch(item.Id, "^[A-Za-z][A-Za-z0-9_-]{0,63}$") || !AiOutputValidation.Text(item.Text)
            || item.KnownAt.Kind != DateTimeKind.Utc || item.KnownAt > context.DecisionAt || item.KnownAt < DateTime.UnixEpoch)
            || statements.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != statements.Count)
            throw new ArgumentException("Every context statement needs a unique ID and a UTC KnownAt timestamp no later than T0. Later evidence belongs outside the T0 snapshot.");
    }

    public static DecisionContextSnapshot Snapshot(DecisionVersion version)
    {
        if (version.Context != null) return Copy(version.Context);
        // Legacy records establish what was recorded, not independently verified historical availability.
        return new()
        {
            DecisionAt = DateTime.SpecifyKind(version.CreatedAt, DateTimeKind.Utc),
            DecisionText = version.NaturalLanguageInput,
            ChosenAction = version.StructuredData.Goal,
            ExpectedOutcome = version.StructuredData.Goal,
            Fields = new(version.StructuredData.Fields),
            Assumptions = version.StructuredData.Assumptions.Select((text, index) => new ContextStatement
            { Id = $"a{index + 1}", Text = text, KnownAt = DateTime.SpecifyKind(version.CreatedAt, DateTimeKind.Utc) }).ToList(),
            Provenance = "legacy-record-time; historical availability not independently verified"
        };
    }

    // Explicit allowlist projection. Never serialize a DecisionV2, recorded outcome or execution history here.
    public static object T0Input(DecisionContextSnapshot snapshot)
    {
        Validate(snapshot);
        return new
        {
            snapshot.DecisionAt, snapshot.DecisionText, snapshot.ChosenAction, snapshot.ExpectedOutcome,
            snapshot.Fields, snapshot.Evidence, snapshot.Assumptions, snapshot.Constraints, snapshot.Alternatives,
            snapshot.Provenance
        };
    }
}
