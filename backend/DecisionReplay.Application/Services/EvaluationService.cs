using System.Diagnostics;
using System.Text.RegularExpressions;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public static class EvaluationScorer
{
    // Deterministic, auditable proxies. These are not semantic truth or calibrated quality scores.
    public static EvaluationMetrics Score(GoldenDecisionCase item, DecisionAiAnalysis? output, double latency)
    {
        if (output == null) return new() { LatencyMs = latency, FailureRate = 1 };
        double Coverage(List<List<string>> expected, IEnumerable<string> actual)
        {
            var text = string.Join(" ", actual);
            return expected.Count == 0 ? 1 : expected.Count(group => group.Any(keyword => Regex.IsMatch(text,
                @"(?<!\w)" + Regex.Escape(keyword) + @"(?!\w)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))) / (double)expected.Count;
        }
        var ids = item.Context.Evidence.Select(evidence => evidence.Id).ToHashSet(StringComparer.Ordinal);
        var references = output.Assumptions.Concat(output.Risks).SelectMany(claim => claim.EvidenceIds)
            .Concat(output.Alternatives.SelectMany(alternative => alternative.EvidenceIds)).ToList();
        return new()
        {
            StructuredValidity = 1,
            MissingInformationCoverage = Coverage(item.ExpectedMissingInformation, output.MissingInformation),
            RiskConceptCoverage = Coverage(item.ExpectedRiskConcepts, output.Risks.Select(claim => claim.Text)),
            AlternativeConceptCoverage = Coverage(item.ExpectedAlternativeConcepts, output.Alternatives.Select(alternative => alternative.Name + " " + alternative.Tradeoff)),
            CitationIntegrity = references.Count == 0 ? 1 : references.Count(ids.Contains) / (double)references.Count,
            UncitedNonHypothesisClaims = output.Assumptions.Concat(output.Risks).Count(claim => !claim.IsHypothesis && claim.EvidenceIds.Count == 0)
                + output.Alternatives.Count(alternative => !alternative.IsHypothesis && alternative.EvidenceIds.Count == 0),
            LatencyMs = latency
        };
    }

    public static EvaluationMetrics Aggregate(IReadOnlyList<EvaluationCaseResult> results)
    {
        if (results.Count == 0) return new();
        return new()
        {
            StructuredValidity = results.Average(item => item.Metrics.StructuredValidity),
            MissingInformationCoverage = results.Average(item => item.Metrics.MissingInformationCoverage),
            RiskConceptCoverage = results.Average(item => item.Metrics.RiskConceptCoverage),
            AlternativeConceptCoverage = results.Average(item => item.Metrics.AlternativeConceptCoverage),
            CitationIntegrity = results.Average(item => item.Metrics.CitationIntegrity),
            UncitedNonHypothesisClaims = results.Sum(item => item.Metrics.UncitedNonHypothesisClaims),
            LatencyMs = results.Average(item => item.Metrics.LatencyMs),
            FailureRate = results.Average(item => item.Metrics.FailureRate)
        };
    }
}

public sealed class EvaluationService(IGoldenDatasetCatalog datasets, IPromptCatalog prompts, AiWorkflowService ai,
    IAiRunRepository repository, IAiProviderResolver providers, AiExecutionScope scope)
{
    private static readonly SemaphoreSlim EvaluationGate = new(1, 1);

    public async Task<IReadOnlyList<AiExperiment>> RunAsync(string owner, string datasetVersion, string promptVersion,
        IReadOnlyList<AiTarget> targets, CancellationToken cancellationToken = default)
    {
        if (targets.Count is < 1 or > 3 || targets.Distinct().Count() != targets.Count)
            throw new ArgumentException("Choose between one and three distinct provider/model targets.");
        var dataset = datasets.Resolve(datasetVersion);
        var prompt = prompts.Resolve("decision-analysis", promptVersion);
        foreach (var target in targets)
        {
            if (string.IsNullOrWhiteSpace(target.Model) || target.Model.Length > 200) throw new ArgumentException("Invalid benchmark model.");
            var availability = await providers.Resolve(target.Provider).CheckAvailabilityAsync(target.Model, cancellationToken);
            if (availability.Status is not ("healthy" or "configured")) throw new AiException(availability.Status, availability.Error ?? "Benchmark provider is unavailable.");
        }
        // Fail clearly rather than allow several HTTP requests to queue entire experiments.
        if (!await EvaluationGate.WaitAsync(0, cancellationToken)) throw new AiException("evaluation_busy", "Another evaluation is running. Try again when it finishes.");
        var experiments = new List<AiExperiment>();
        try
        {
            foreach (var target in targets)
            {
                var experiment = new AiExperiment
                {
                    OwnerId = owner, DatasetVersion = dataset.Version, DatasetHash = dataset.Hash, Target = target,
                    PromptVersion = promptVersion, PromptHash = prompt.Hash, Parameters = providers.Parameters, RequestTimeoutSeconds = providers.RequestTimeout.TotalSeconds, MaxRetries = providers.MaxRetries
                };
                experiment.OutputContractVersion = promptVersion == "v3" ? AiOutputSchema.Version : null;
                experiments.Add(experiment);
                await repository.SaveExperimentAsync(experiment, cancellationToken);
                try
                {
                    foreach (var item in dataset.Cases)
                    {
                        var clock = Stopwatch.StartNew();
                        var result = new EvaluationCaseResult { CaseId = item.Id };
                        try
                        {
                            var generated = await ai.GenerateAsync<DecisionAiAnalysis>("decision-analysis", promptVersion,
                                ReplayContextService.T0Input(item.Context), output => AiOutputValidation.Analysis(output, item.Context, promptVersion == "v3"),
                                scope.Context with { OwnerId = owner, ExperimentId = experiment.Id, DecisionId = null, ReplayId = null, ContextVersion = 1 },
                                target, allowFallback: false, outputSchema: promptVersion == "v3" ? AiOutputSchema.Analysis(item.Context) : null,
                                cancellationToken: cancellationToken);
                            result.Success = true;
                            result.ExecutionId = generated.ExecutionId;
                            result.Output = generated.Value;
                        }
                        catch (AiException ex)
                        {
                            result.ErrorCode = ex.Code;
                            // Include failed execution IDs through the experiment's execution query, even without a validated result.
                            var executions = await repository.GetExecutionsAsync(owner, cancellationToken: cancellationToken);
                            result.ExecutionId = executions.FirstOrDefault(execution => execution.ExperimentId == experiment.Id
                                && execution.InputHash == AiWorkflowService.Hash(System.Text.Json.JsonSerializer.Serialize(ReplayContextService.T0Input(item.Context), AiWorkflowService.Json)))?.Id;
                        }
                        result.Metrics = EvaluationScorer.Score(item, result.Output, clock.Elapsed.TotalMilliseconds);
                        experiment.Results.Add(result);
                        experiment.Metrics = EvaluationScorer.Aggregate(experiment.Results);
                        await repository.SaveExperimentAsync(experiment, cancellationToken);
                    }
                    experiment.Status = experiment.Results.All(item => item.Success) ? "completed" : "completed_with_failures";
                }
                catch (OperationCanceledException) { experiment.Status = "cancelled"; throw; }
                catch { experiment.Status = "failed"; throw; }
                finally
                {
                    experiment.FinishedAt = DateTime.UtcNow;
                    using var persistenceTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await repository.SaveExperimentAsync(experiment, persistenceTimeout.Token);
                }
            }
            return experiments;
        }
        finally { EvaluationGate.Release(); }
    }
}
