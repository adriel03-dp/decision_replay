using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DecisionReplay.API.Controllers;

[ApiController, Authorize, Route("api/ai")]
public sealed class AiController(IAiRunRepository runs, IPromptCatalog prompts, EvaluationService evaluation,
    IAiProviderResolver providers) : ControllerBase
{
    private string Owner => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    [HttpGet("configuration")]
    public IActionResult Configuration() => Ok(new
    {
        extraction = providers.DefaultTarget("extraction"),
        analysis = providers.DefaultTarget("decision-analysis"),
        providers.RequestTimeout.TotalSeconds, providers.MaxRetries,
        prompts = prompts.List().Select(prompt => new { prompt.Operation, prompt.Version, prompt.Hash }),
        datasetVersions = new[] { "v1" }
    });

    [HttpGet("executions")]
    public async Task<IActionResult> Executions([FromQuery] Guid? decisionId, CancellationToken token) =>
        Ok((await runs.GetExecutionsAsync(Owner, decisionId, token)).Select(execution => new
        {
            execution.Id, execution.DecisionId, execution.ReplayId, execution.ExperimentId, execution.ContextVersion,
            execution.Operation, execution.PromptVersion, execution.Status, execution.LatencyMs,
            execution.StartedAt, execution.RetryCount, execution.UsedFallback, execution.ErrorCode,
            provider = execution.Attempts.LastOrDefault()?.Provider, model = execution.Attempts.LastOrDefault()?.Model
        }));

    [HttpGet("executions/{executionId:guid}")]
    public async Task<IActionResult> Execution(Guid executionId, CancellationToken token) =>
        Ok(await runs.GetExecutionAsync(executionId, Owner, token) ?? throw new KeyNotFoundException("Execution not found."));

    [HttpPost("experiments")]
    public async Task<IActionResult> Run([FromBody] EvaluationRequest request, CancellationToken token) =>
        Ok(await evaluation.RunAsync(Owner, request.DatasetVersion, request.PromptVersion, request.Targets, token));

    [HttpGet("experiments")]
    public async Task<IActionResult> Experiments(CancellationToken token) => Ok(await runs.GetExperimentsAsync(Owner, token));

    [HttpGet("experiments/{experimentId:guid}")]
    public async Task<IActionResult> Experiment(Guid experimentId, CancellationToken token) =>
        Ok(await runs.GetExperimentAsync(experimentId, Owner, token) ?? throw new KeyNotFoundException("Experiment not found."));

    [HttpGet("experiments/comparison")]
    public async Task<IActionResult> Compare([FromQuery] Guid[] ids, CancellationToken token)
    {
        if (ids.Length is < 2 or > 6) return BadRequest(new { message = "Select between two and six experiment IDs." });
        var results = new List<AiExperiment>();
        foreach (var id in ids.Distinct()) results.Add(await runs.GetExperimentAsync(id, Owner, token)
            ?? throw new KeyNotFoundException("Experiment not found."));
        if (results.Select(result => (result.DatasetHash, result.PromptHash, result.Parameters, result.OutputContractVersion, result.RequestTimeoutSeconds, result.MaxRetries)).Distinct().Count() != 1)
            return BadRequest(new { message = "Comparison requires the same dataset, prompt, generation parameters and request/retry budgets." });
        return Ok(results.Select(result => new { result.Id, result.Target, result.Status, result.Metrics, result.ApiCost }));
    }
}

public sealed class EvaluationRequest
{
    [Required, MaxLength(20)] public string DatasetVersion { get; init; } = "v1";
    [Required, MaxLength(20)] public string PromptVersion { get; init; } = "v1";
    [Required, MinLength(1), MaxLength(3)] public List<AiTarget> Targets { get; init; } = new();
}
