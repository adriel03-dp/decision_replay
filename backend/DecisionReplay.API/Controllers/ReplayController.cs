using Microsoft.AspNetCore.Mvc;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.API.DTOs;
using DecisionReplay.API.Mapping;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Route("api/replay")]
public class ReplayController : ControllerBase
{
    private readonly IDecisionRepository _repository;

    public ReplayController(IDecisionRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{decisionId}")]
    public async Task<ActionResult<ReplayResponse>> Replay(string decisionId)
    {
        var events = await _repository.GetEventsAsync(Guid.Parse(decisionId));

        return Ok(new ReplayResponse(
            decisionId,
            events.Select(e => e.ToResponse())
        ));
    }
}
