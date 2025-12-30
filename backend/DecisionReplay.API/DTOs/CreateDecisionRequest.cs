namespace DecisionReplay.API.DTOs;

public record CreateDecisionRequest(
    string Type,
    string CreatedBy,
    Dictionary<string, object> InputData
);
