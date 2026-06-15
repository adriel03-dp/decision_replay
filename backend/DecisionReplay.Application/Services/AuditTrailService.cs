using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class AuditTrailService
{
    public AuditTrailEntry Create(
        AuditActionType action,
        int version,
        string actor,
        string summary,
        IReadOnlyDictionary<string, string>? details = null) =>
        new()
        {
            Action = action,
            Version = version,
            Actor = actor,
            Summary = summary,
            Details = details == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(details),
            Timestamp = DateTime.UtcNow
        };
}
