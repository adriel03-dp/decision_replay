using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DecisionReplay.API.Controllers;

[ApiController, Authorize, Route("api/v2/decisions/{id:guid}")]
public sealed class HistoricalReplayController(HistoricalReplayService history) : ControllerBase
{
    private string Owner => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    [HttpGet("timeline")]
    public async Task<ActionResult<DecisionTimeline>> Timeline(Guid id) => Ok(await history.GetTimelineAsync(id, Owner));

    [HttpPost("context")]
    public async Task<IActionResult> RecordContext(Guid id, [FromBody] DecisionContextSnapshot context) =>
        Ok(new { version = await history.RecordContextAsync(id, Owner, context) });

    [HttpPost("outcomes")]
    public async Task<ActionResult<RecordedOutcome>> Outcome(Guid id, [FromBody] RecordedOutcome outcome) =>
        Ok(await history.RecordOutcomeAsync(id, Owner, outcome));

    [HttpPost("scenarios")]
    public async Task<ActionResult<ReplayScenario>> Scenario(Guid id, [FromBody] ScenarioChanges changes) =>
        Ok(await history.CreateScenarioAsync(id, Owner, changes));

    [HttpPost("ai-analysis")]
    public async Task<ActionResult<ReplayAnalysisRecord>> Analyse(Guid id, [FromBody] HistoricalAnalysisRequest request, CancellationToken token) =>
        Ok(await history.AnalyseAsync(id, Owner, request.Version, request.ScenarioId, request.PromptVersion, token));

    [HttpPost("outcome-reflection")]
    public async Task<ActionResult<ValidatedAiResult<OutcomeReflection>>> Reflect(Guid id, [FromBody] OutcomeReflectionRequest request, CancellationToken token) =>
        Ok(await history.ReflectAsync(id, Owner, request.OutcomeId, request.AnalysisId, null, token));
}

public sealed record HistoricalAnalysisRequest
{
    [Range(1, 100)] public int Version { get; init; }
    public Guid? ScenarioId { get; init; }
    [Required, MaxLength(20)] public string PromptVersion { get; init; } = "v3";
}
public sealed record OutcomeReflectionRequest(Guid OutcomeId, Guid AnalysisId);
