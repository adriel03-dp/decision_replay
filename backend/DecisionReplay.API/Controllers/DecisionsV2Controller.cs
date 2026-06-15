using System.Security.Claims;
using DecisionReplay.API.DTOs;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/decisions")]
public sealed class DecisionsV2Controller : ControllerBase
{
    private readonly DecisionEngineService _engine;
    private readonly IDecisionV2Repository _repository;
    private readonly DomainTemplateService _templates;
    private readonly ReplayComparisonService _replayComparison;
    private readonly AuditTrailService _audit;
    private readonly PlanExportService _exports;

    public DecisionsV2Controller(
        DecisionEngineService engine,
        IDecisionV2Repository repository,
        DomainTemplateService templates,
        ReplayComparisonService replayComparison,
        AuditTrailService audit,
        PlanExportService exports)
    {
        _engine = engine;
        _repository = repository;
        _templates = templates;
        _replayComparison = replayComparison;
        _audit = audit;
        _exports = exports;
    }

    [HttpPost]
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(DecisionEngineResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<DecisionEngineResponse>> Analyze(
        [FromBody] AnalyzeDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var decision = await _engine.AnalyzeAndCreateAsync(
            request.Input,
            UserId(),
            cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = decision.Id },
            ToResponse(decision));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DecisionEngineSummaryResponse>>> GetAll()
    {
        var decisions = await _repository.GetByUserAsync(UserId());
        return Ok(decisions
            .Select(ToSummary)
            .Where(summary => summary != null)
            .Cast<DecisionEngineSummaryResponse>()
            .ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DecisionEngineResponse>> GetById(Guid id)
    {
        var decision = await _repository.GetByIdForUserAsync(id, UserId());
        return decision == null
            ? NotFound(ErrorResponse.NotFound("Decision not found.", Request.Path))
            : Ok(ToResponse(decision));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await _repository.DeleteForUserAsync(id, UserId())
            ? NoContent()
            : NotFound(ErrorResponse.NotFound("Decision not found.", Request.Path));
    }

    [HttpPost("{id:guid}/replay")]
    public async Task<ActionResult<ReplayDecisionResponse>> Replay(
        Guid id,
        [FromBody] ReplayDecisionEngineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _engine.ReplayAsync(
            id,
            request.UpdatedInput,
            UserId(),
            cancellationToken);
        return Ok(new ReplayDecisionResponse(
            ToResponse(result.Decision),
            result.Comparison));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<ActionResult<IReadOnlyList<DecisionVersion>>> GetVersions(Guid id)
    {
        var decision = await _repository.GetByIdForUserAsync(id, UserId());
        return decision == null
            ? NotFound(ErrorResponse.NotFound("Decision not found.", Request.Path))
            : Ok(decision.Versions.OrderBy(version => version.Version).ToList());
    }

    [HttpPost("{id:guid}/plans/generate")]
    public async Task<ActionResult<ActionPlan>> GeneratePlan(
        Guid id,
        CancellationToken cancellationToken)
    {
        var plan = await _engine.RegeneratePlanAsync(id, UserId(), cancellationToken);
        return Ok(plan);
    }

    [HttpGet("{id:guid}/plans/{planId:guid}")]
    public async Task<ActionResult<ActionPlan>> GetPlan(Guid id, Guid planId)
    {
        return Ok(await _engine.GetPlanAsync(id, planId, UserId()));
    }

    [HttpPost("{id:guid}/plans/{planId:guid}/replay")]
    public async Task<ActionResult<ReplayDecisionResponse>> ReplayPlan(
        Guid id,
        Guid planId,
        [FromBody] ReplayDecisionEngineRequest request,
        CancellationToken cancellationToken)
    {
        await _engine.GetPlanAsync(id, planId, UserId());
        return await Replay(id, request, cancellationToken);
    }

    [HttpGet("{id:guid}/plans/{planId:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id, Guid planId)
    {
        var (decision, version, replay) = await GetExportContext(id, planId);
        var file = _exports.ExportPdf(decision, version, replay);
        await RecordExport(decision, version, "PDF");
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("{id:guid}/plans/{planId:guid}/export/excel")]
    public async Task<IActionResult> ExportExcel(Guid id, Guid planId)
    {
        var (decision, version, replay) = await GetExportContext(id, planId);
        var file = _exports.ExportExcel(decision, version, replay);
        await RecordExport(decision, version, "Excel");
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("domains")]
    [AllowAnonymous]
    public ActionResult<object> GetDomains() =>
        Ok(_templates.GetAll().Select(template => new
        {
            template.Domain,
            template.DisplayName,
            requiredFields = template.Fields.Where(field => field.Required),
            optionalFields = template.Fields.Where(field => !field.Required),
            template.ScoringFactors,
            template.ReplaySensitiveFields
        }));

    private async Task<(DecisionV2 Decision, DecisionVersion Version, ReplayComparison? Replay)>
        GetExportContext(Guid decisionId, Guid planId)
    {
        var decision = await _repository.GetByIdForUserAsync(decisionId, UserId())
            ?? throw new KeyNotFoundException("Decision not found.");
        var version = decision.Versions.FirstOrDefault(item => item.Plan?.PlanId == planId)
            ?? throw new KeyNotFoundException("Plan not found.");
        ReplayComparison? replay = null;
        var previous = decision.Versions.FirstOrDefault(item => item.Version == version.Version - 1);
        if (previous != null)
            replay = _replayComparison.Compare(
                previous,
                version,
                _templates.Get(version.StructuredData.Domain));
        return (decision, version, replay);
    }

    private async Task RecordExport(
        DecisionV2 decision,
        DecisionVersion version,
        string format)
    {
        decision.AuditTrail.Add(_audit.Create(
            AuditActionType.PlanExported,
            version.Version,
            UserId(),
            $"{format} plan export generated.",
            new Dictionary<string, string>
            {
                ["format"] = format,
                ["planId"] = version.Plan?.PlanId.ToString() ?? string.Empty
            }));
        await _repository.UpdateAsync(decision);
    }

    private string UserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Authenticated user ID is missing.");

    private static DecisionEngineResponse ToResponse(DecisionV2 decision)
    {
        var version = decision.GetCurrentVersion()
            ?? throw new InvalidOperationException("Decision has no current version.");
        return new DecisionEngineResponse(
            decision.Id,
            version.Version,
            version.StructuredData.Domain,
            version.StructuredData.Title,
            version.StructuredData.Goal,
            version.NaturalLanguageInput,
            version.StructuredData.Fields,
            version.Feasibility.FeasibilityScore,
            version.Feasibility.RiskLevel.ToString(),
            version.Feasibility.FactorBreakdown,
            version.Feasibility.Risks,
            version.StructuredData.Assumptions,
            version.Validation.MissingFields,
            version.Feasibility.Recommendations,
            version.Explanation,
            version.Plan,
            decision.AuditTrail.OrderBy(entry => entry.Timestamp).ToList(),
            version.CreatedAt);
    }

    private static DecisionEngineSummaryResponse? ToSummary(DecisionV2 decision)
    {
        var version = decision.GetCurrentVersion();
        if (version == null) return null;
        return new DecisionEngineSummaryResponse(
            decision.Id,
            version.Version,
            version.StructuredData.Domain,
            version.StructuredData.Title,
            version.Feasibility.FeasibilityScore,
            version.Feasibility.RiskLevel.ToString(),
            version.Validation.MissingFields.Count,
            version.Feasibility.Risks.Count,
            decision.LastModifiedAt ?? decision.CreatedAt);
    }
}
