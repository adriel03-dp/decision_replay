using Microsoft.AspNetCore.Mvc;
using DecisionReplay.Application.Services;
using DecisionReplay.API.DTOs;
using DecisionReplay.API.Mapping;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Route("api/decisions")]
public class DecisionsController : ControllerBase
{
    private readonly DecisionService _service;

    public DecisionsController(DecisionService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<DecisionResponse>> Create(
        CreateDecisionRequest request)
    {
        var decision = await _service.CreateDecisionAsync(
            request.Type,
            request.CreatedBy
        );

        return Ok(decision.ToResponse());
    }
}
