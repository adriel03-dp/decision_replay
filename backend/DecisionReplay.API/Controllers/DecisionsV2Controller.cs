using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DecisionReplay.Application.Services;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.API.DTOs;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;
using System.Security.Claims;

namespace DecisionReplay.API.Controllers;

/// <summary>
/// Hybrid Decision Controller – primary API surface after re-architecture.
///
/// Endpoints
/// ─────────
/// POST   /api/v2/decisions/evaluate  – Hybrid analysis (no save)
/// POST   /api/v2/decisions/simulate  – Scenario simulation (no save)
/// POST   /api/v2/decisions           – Evaluate + save
/// GET    /api/v2/decisions           – List user's decisions
/// GET    /api/v2/decisions/{id}      – Get saved decision
/// DELETE /api/v2/decisions/{id}      – Delete decision
/// </summary>
[ApiController]
[Route("api/v2/decisions")]
[Authorize]
public class DecisionsV2Controller : ControllerBase
{
    private readonly HybridDecisionService _hybridService;
    private readonly IDecisionV2Repository _repository;
    private readonly ILogger<DecisionsV2Controller> _logger;

    public DecisionsV2Controller(
        HybridDecisionService hybridService,
        IDecisionV2Repository repository,
        ILogger<DecisionsV2Controller> logger)
    {
        _hybridService = hybridService ?? throw new ArgumentNullException(nameof(hybridService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Evaluate (no save) ─────────────────────────────────────────────

    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(ProjectEvaluationResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ProjectEvaluationResponse>> Evaluate(
        [FromBody] ProjectEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!request.Features.Any(f => !string.IsNullOrWhiteSpace(f)))
            return BadRequest(ErrorResponse.BadRequest(
                "At least one non-empty feature must be specified.", HttpContext.Request.Path));

        try
        {
            var input = ToProjectInput(request);
            var result = await _hybridService.EvaluateAsync(input, request.RequestAiEnhancement, cancellationToken);

            _logger.LogInformation("[EVALUATE] User={User} Score={Score} AI={AI}",
                UserId(), result.Feasibility.Score, result.AiAvailable);

            return Ok(ToEvaluationResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVALUATE] Failed for user {User}", UserId());
            return StatusCode(500, ErrorResponse.InternalServerError(
                "Evaluation failed. Please try again.", HttpContext.Request.Path));
        }
    }

    // ── Simulate ────────────────────────────────────────────────────────

    [HttpPost("simulate")]
    [ProducesResponseType(typeof(SimulationResponse), 200)]
    [ProducesResponseType(400)]
    public ActionResult<SimulationResponse> Simulate([FromBody] SimulationRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var baseInput = ToProjectInput(request.BaseProject);
        var adjustments = new SimulationAdjustments
        {
            BudgetUsd      = request.BudgetUsd,
            TimelineMonths = request.TimelineMonths,
            TeamSize       = request.TeamSize,
            AddFeatures    = request.AddFeatures ?? new List<string>(),
            RemoveFeatures = request.RemoveFeatures ?? new List<string>(),
        };

        var sim = _hybridService.Simulate(baseInput, adjustments);

        return Ok(new SimulationResponse(
            ToInputDto(sim.AdjustedInput),
            ToFeasibilityDto(sim.NewFeasibility),
            sim.ScoreDelta,
            sim.VerdictDelta,
            sim.ImpactSummary.ToList()));
    }

    // ── Create (evaluate + save) ────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(SavedDecisionResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SavedDecisionResponse>> Create(
        [FromBody] ProjectEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var input = ToProjectInput(request);
            var result = await _hybridService.EvaluateAsync(input, request.RequestAiEnhancement, cancellationToken);

            var decision = new DecisionV2(input, UserId());
            decision.StoreHybridResult(result.Feasibility, result.Plan, result.AiEnhancement);
            await _repository.CreateAsync(decision);

            _logger.LogInformation("[CREATE] Decision {Id} saved for user {User}", decision.Id, UserId());

            return CreatedAtAction(nameof(GetById), new { id = decision.Id },
                new SavedDecisionResponse(decision.Id, ToEvaluationResponse(result), decision.CreatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CREATE] Failed for user {User}", UserId());
            return StatusCode(500, ErrorResponse.InternalServerError(
                "Failed to save decision.", HttpContext.Request.Path));
        }
    }

    // ── List ────────────────────────────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(List<DecisionSummaryResponse>), 200)]
    public async Task<ActionResult<List<DecisionSummaryResponse>>> GetAll()
    {
        var decisions = await _repository.GetByUserAsync(UserId());
        return Ok(decisions.Select(ToSummary).ToList());
    }

    // ── Get by ID ────────────────────────────────────────────────────────

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SavedDecisionResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<SavedDecisionResponse>> GetById(Guid id)
    {
        var d = await _repository.GetByIdAsync(id);
        if (d == null)
            return NotFound(ErrorResponse.NotFound("Decision not found", HttpContext.Request.Path));

        return Ok(ToSavedResponse(d));
    }

    // ── Delete ──────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (await _repository.GetByIdAsync(id) == null)
            return NotFound(ErrorResponse.NotFound("Decision not found", HttpContext.Request.Path));

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    // ── Mapping helpers ──────────────────────────────────────────────────

    private string UserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "unknown";

    private static ProjectInput ToProjectInput(ProjectEvaluationRequest r) =>
        new(r.ProjectType,
            r.Features.Where(f => !string.IsNullOrWhiteSpace(f)).ToList(),
            r.BudgetUsd, r.TimelineMonths, r.TeamSize, r.RawInput, isStructured: true);

    private static ProjectEvaluationResponse ToEvaluationResponse(HybridAnalysisResult r) =>
        new(ToInputDto(r.Input),
            ToFeasibilityDto(r.Feasibility),
            ToPlanDto(r.Plan),
            r.AiEnhancement != null ? ToAiDto(r.AiEnhancement) : null,
            r.AiAvailable,
            r.GeneratedAt);

    private static ProjectInputDto ToInputDto(ProjectInput i) =>
        new(i.ProjectType, i.Features.ToList(), i.BudgetUsd, i.TimelineMonths, i.TeamSize, i.IsStructured, i.RawInput);

    private static FeasibilityResultDto ToFeasibilityDto(FeasibilityResult f) =>
        new(f.Score, f.Verdict,
            f.BudgetFitScore, f.TimelineFitScore, f.TeamCapacityScore, f.ComplexityScore,
            f.EstimatedCostUsd, f.EstimatedMonths, f.RequiredTeamSize,
            f.Issues.Select(i => new FeasibilityIssueDto(i.Dimension, i.Message, i.Severity)).ToList(),
            f.SuggestedAdjustments.Select(s => new SuggestedAdjustmentDto(s.Parameter, s.Description, s.QuantitativeImpact)).ToList(),
            f.Explainability.ToList(), f.GeneratedAt);

    private static ProjectPlanDto ToPlanDto(ProjectPlan p) =>
        new(p.TotalMonths,
            p.Phases.Select(ph => new TimelinePhaseDto(
                ph.Name, ph.StartMonth, ph.EndMonth, ph.DurationMonths,
                ph.Tasks.ToList(), ph.Deliverables.ToList(), ph.PercentageOfTotal)).ToList(),
            p.Milestones.ToList(), p.GeneratedAt);

    private static AiEnhancementDto ToAiDto(DecisionAnalysis a) =>
        new(a.ExecutiveSummary,
            a.Recommendations.ToList(),
            a.Risks.Select(r => r.Description + (r.Mitigation != null ? $" – {r.Mitigation}" : "")).ToList(),
            a.Pros.ToList(), a.Cons.ToList(),
            a.ConfidenceLevel, a.ModelUsed, a.GeneratedAt);

    private static SavedDecisionResponse ToSavedResponse(DecisionV2 d)
    {
        ProjectEvaluationResponse? eval = null;
        if (d.FeasibilityResult != null)
        {
            var inputDto = d.ProjectInput != null
                ? ToInputDto(d.ProjectInput)
                : new ProjectInputDto(d.DomainType ?? "Unknown", new List<string>(), 0, 0, 1, false, null);

            var planDto = d.ProjectPlan != null
                ? ToPlanDto(d.ProjectPlan)
                : new ProjectPlanDto(0, new List<TimelinePhaseDto>(), new List<string>(), d.CreatedAt);

            var aiDto = d.Analysis != null ? ToAiDto(d.Analysis) : null;

            eval = new ProjectEvaluationResponse(inputDto, ToFeasibilityDto(d.FeasibilityResult), planDto, aiDto, aiDto != null, d.CreatedAt);
        }
        return new SavedDecisionResponse(d.Id, eval, d.CreatedAt);
    }

    private static DecisionSummaryResponse ToSummary(DecisionV2 d) =>
        new(d.Id,
            d.ProjectInput?.ProjectType ?? d.DomainType ?? "Unknown",
            d.ProjectInput?.Features.ToList() ?? new List<string>(),
            d.FeasibilityResult?.Score,
            d.FeasibilityResult?.Verdict,
            d.Status.ToString(),
            d.CreatedAt);
}

