using System.ComponentModel.DataAnnotations;

namespace DecisionReplay.API.DTOs;

public sealed record AcceptImprovementRequest
{
    [Range(1, 100)] public int SourceVersion { get; init; }
}
