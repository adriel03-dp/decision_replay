using DecisionReplay.API.DTOs;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DecisionReplay.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v2/requirements")]
public sealed class RequirementsController(RequirementsReviewService review) : ControllerBase
{
    [HttpGet("domains")]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<RequirementDomain>> Domains() => Ok(review.Domains());

    [HttpPost("preview")]
    public async Task<ActionResult<RequirementsReview>> Preview(AnalyzeDecisionRequest request, CancellationToken token) =>
        Ok(await review.PreviewAsync(request.Input, request.FixedFields, token));

    [HttpPost("validate")]
    public ActionResult<RequirementsReview> Validate(AnalyzeStructuredDecisionRequest request) =>
        Ok(review.Review(DecisionRequestMapper.Structured(request)));
}
