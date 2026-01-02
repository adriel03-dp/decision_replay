using Microsoft.AspNetCore.Mvc;
using DecisionReplay.Application.Services;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.API.DTOs;
using DecisionReplay.API.Mapping;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Route("api/decisions")]
public class DecisionsController : ControllerBase
{
    private readonly DecisionService _service;
    private readonly IDecisionRepository _repository;
    private readonly IAIReasoningService _aiService;

    public DecisionsController(
        DecisionService service,
        IDecisionRepository repository,
        IAIReasoningService aiService)
    {
        _service = service;
        _repository = repository;
        _aiService = aiService;
    }

    [HttpPost]
    public async Task<ActionResult<DecisionResponse>> Create(
        CreateDecisionRequest request)
    {
        var decision = await _service.CreateDecisionAsync(
            request.Title,
            request.Scope,
            request.Timeline,
            request.Resources,
            request.Constraints,
            request.CreatedBy
        );

        return Ok(decision.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DecisionResponse>>> GetAll()
    {
        var decisions = await _repository.GetAllAsync();
        return Ok(decisions.Select(d => d.ToResponse()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DecisionResponse>> GetById(string id)
    {
        var decision = await _repository.GetByIdAsync(Guid.Parse(id));
        if (decision == null)
        {
            return NotFound();
        }
        return Ok(decision.ToResponse());
    }

    [HttpGet("{id}/events")]
    public async Task<ActionResult<IEnumerable<DecisionEventResponse>>> GetEvents(string id)
    {
        var events = await _repository.GetEventsAsync(Guid.Parse(id));
        return Ok(events.Select(e => e.ToResponse()));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DecisionResponse>> Update(string id, [FromBody] UpdateDecisionRequest request)
    {
        var decision = await _repository.GetByIdAsync(Guid.Parse(id));
        if (decision == null)
        {
            return NotFound();
        }

        // Update decision fields as needed
        // Add update logic here based on your requirements

        await _repository.UpdateAsync(decision);
        return Ok(decision.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id)
    {
        await _repository.DeleteAsync(Guid.Parse(id));
        return NoContent();
    }

    [HttpPost("{id}/analyze")]
    public async Task<ActionResult> AnalyzeDecision(string id)
    {
        var decision = await _repository.GetByIdAsync(Guid.Parse(id));
        if (decision == null)
        {
            return NotFound();
        }

        // Generate AI reasoning and append as event
        await _service.RunAIReasoningAsync(Guid.Parse(id));

        // Get all events including the new reasoning
        var events = await _repository.GetEventsAsync(Guid.Parse(id));
        var latestReasoning = events.LastOrDefault(e => e.EventType == Domain.Enums.DecisionEventType.FeasibilityGenerated);

        return Ok(new
        {
            DecisionId = id,
            Reasoning = latestReasoning?.Payload,
            EventId = latestReasoning?.Id,
            GeneratedAt = latestReasoning?.Timestamp
        });
    }

    [HttpPost("{id}/ask")]
    public async Task<ActionResult> AskQuestion(string id, [FromBody] AskQuestionRequest request)
    {
        var decision = await _repository.GetByIdAsync(Guid.Parse(id));
        if (decision == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { error = "Question is required" });
        }

        var response = await _aiService.AnswerDecisionQueryAsync(decision, request.Question);
        return Ok(response);
    }
}

public record AskQuestionRequest(string Question);
