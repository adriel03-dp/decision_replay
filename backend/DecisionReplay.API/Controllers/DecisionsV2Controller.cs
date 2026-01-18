using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DecisionReplay.Application.Services;
using DecisionReplay.Application.Interfaces;
using DecisionReplay.API.DTOs;
using DecisionReplay.API.Mapping;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DecisionReplay.API.Controllers;

/// <summary>
/// REFACTORED DecisionsV2 Controller - Domain-Agnostic Decision Management
/// 
/// SOLID Principles Applied:
/// - Single Responsibility: Handles HTTP concerns only, delegates to service layer
/// - Dependency Inversion: Depends on abstractions (services), not implementations
/// - Open/Closed: Extensible via service layer without modifying controller
/// 
/// Clean Architecture:
/// - API/Presentation layer
/// - No business logic - only orchestration
/// - Maps between DTOs and domain entities
/// 
/// Production Features:
/// - Requires authentication (JWT)
/// - Rate-limited via middleware
/// - Domain-agnostic: accepts natural language input
/// - RESTful API design
/// - Decision replay capability
/// - Chart-agnostic visualization endpoints
/// 
/// Improvements over V1:
/// - No hard-coded fields
/// - Natural language input
/// - AI-powered parsing and analysis
/// - Built-in replay capability
/// - Visualization support
/// </summary>
[ApiController]
[Route("api/v2/decisions")]
[Authorize]
public class DecisionsV2Controller : ControllerBase
{
    private readonly DecisionServiceV2 _service;
    private readonly IDecisionV2Repository _repository;
    private readonly IInputValidationService _validationService;
    private readonly ILogger<DecisionsV2Controller> _logger;

    public DecisionsV2Controller(
        DecisionServiceV2 service,
        IDecisionV2Repository repository,
        IInputValidationService validationService,
        ILogger<DecisionsV2Controller> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a decision from natural language input
    /// POST /api/v2/decisions
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DecisionV2Response), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 429)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<DecisionV2Response>> Create([FromBody] NaturalLanguageDecisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            _logger.LogWarning("Create decision failed: empty input from user {User}", User.Identity?.Name);
            return BadRequest(new { error = "ValidationError", message = "Input cannot be empty. Provide a natural language description of your decision." });
        }

        // Validate input content and detect domain
        var validationResult = _validationService.ValidateInput(request.Input);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Input validation failed for user {User}: {Error}", User.Identity?.Name, validationResult.ErrorMessage);
            return BadRequest(new { error = "ValidationError", message = validationResult.ErrorMessage });
        }

        try
        {
            // Extract user ID from JWT claim (NameIdentifier contains user ID, Name contains display name)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? request.CreatedBy;
            _logger.LogInformation("Creating decision for user {User} in domain: {Domain}", userId, validationResult.DomainType);

            // Use sanitized input for decision creation
            var sanitizedInput = validationResult.SanitizedInput ?? request.Input;

            // Create decision (parses intent, generates schema)
            _logger.LogInformation("Step 1: Calling CreateDecisionAsync...");
            var decision = await _service.CreateDecisionAsync(
                sanitizedInput, 
                userId, 
                request.AnalyzeNow, 
                HttpContext.RequestAborted
            );

            // Store detected domain type in the decision
            decision.DomainType = validationResult.DomainType;

            _logger.LogInformation("Step 2: Decision object created, saving to MongoDB...");

            await _repository.CreateAsync(decision);
            _logger.LogInformation("Step 3: Decision {DecisionId} saved to MongoDB", decision.Id);

            _logger.LogInformation("Decision {DecisionId} created successfully for user {User}", decision.Id, userId);
            return Ok(decision.ToResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create decision for user {User}. Error: {Error}", User.Identity?.Name, ex.ToString());

            // Return user-friendly error message
            var userMessage = GetUserFriendlyErrorMessage(ex);
            return StatusCode(500, new
            {
                error = "InternalServerError",
                message = userMessage,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Update a decision
    /// PUT /api/v2/decisions/{id}
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DecisionV2Response), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<DecisionV2Response>> Update(Guid id, [FromBody] UpdateDecisionV2Request request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { error = $"Decision {id} not found" });
        }

        // Update context if new input provided (triggers re-analysis requirement)
        if (!string.IsNullOrEmpty(request.UpdatedInput))
        {
            var newContext = new DecisionContext(request.UpdatedInput, existing.Context.InferredAttributes);
            existing.UpdateContext(newContext);
        }

        await _repository.UpdateAsync(existing);
        return Ok(existing.ToResponse());
    }

    /// <summary>
    /// Delete a decision
    /// DELETE /api/v2/decisions/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Delete(Guid id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { error = $"Decision {id} not found" });
        }

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Get decision by ID
    /// GET /api/v2/decisions/{id}
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DecisionV2Response), 200)]
    [ProducesResponseType(typeof(object), 404)]
    public async Task<ActionResult<DecisionV2Response>> GetById(Guid id)
    {
        _logger.LogInformation("Fetching decision {DecisionId} for user {User}", id, User.Identity?.Name);

        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
        {
            _logger.LogWarning("Decision {DecisionId} not found for user {User}", id, User.Identity?.Name);
            return NotFound(new { error = "Decision not found" });
        }

        _logger.LogInformation("Decision {DecisionId} found, Context is {IsNull}", id, decision.Context == null ? "NULL" : "NOT NULL");

        if (decision.Context == null)
        {
            _logger.LogError("Decision {DecisionId} has NULL Context! CreatedBy: {CreatedBy}, Status: {Status}",
                id, decision.CreatedBy, decision.Status);
            return StatusCode(500, new { error = "InternalServerError", message = "Decision data is corrupted (null context)", timestamp = DateTime.UtcNow });
        }

        return Ok(decision.ToResponse());
    }

    /// <summary>
    /// Temporary analysis without saving decision
    /// POST /api/v2/decisions/analyze-temp
    /// </summary>
    [HttpPost("analyze-temp")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<ActionResult<object>> AnalyzeTemporary([FromBody] NaturalLanguageDecisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            _logger.LogWarning("Temporary analysis failed: empty input from user {User}", User.Identity?.Name);
            return BadRequest(new { error = "ValidationError", message = "Input cannot be empty. Provide a natural language description of your decision." });
        }

        // Validate input content and detect domain
        var validationResult = _validationService.ValidateInput(request.Input);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Input validation failed for temporary analysis, user {User}: {Error}", User.Identity?.Name, validationResult.ErrorMessage);
            return BadRequest(new { error = "ValidationError", message = validationResult.ErrorMessage });
        }

        try
        {
            // Extract user ID from JWT claim (NameIdentifier contains user ID, Name contains display name)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? request.CreatedBy;
            _logger.LogInformation("Creating temporary analysis for user {User} in domain: {Domain}", userId, validationResult.DomainType);

            // Use sanitized input for analysis
            var sanitizedInput = validationResult.SanitizedInput ?? request.Input;

            // Create decision without saving to database
            var decision = await _service.CreateDecisionAsync(sanitizedInput, userId);

            // Generate analysis without persisting
            if (decision.Analysis != null)
            {
                _logger.LogInformation("Temporary analysis completed for user {User}", userId);

                // Log what we're about to return for debugging
                _logger.LogInformation("Analysis data: Score={Score}, Pros={ProsCount}, Cons={ConsCount}, Risks={RisksCount}, Recommendations={RecommendationsCount}",
                    decision.Analysis.FeasibilityScore,
                    decision.Analysis.Pros?.Count ?? 0,
                    decision.Analysis.Cons?.Count ?? 0,
                    decision.Analysis.Risks?.Count ?? 0,
                    decision.Analysis.Recommendations?.Count ?? 0);

                // Return the complete analysis data structure
                return Ok(new
                {
                    // Basic metrics
                    feasibilityScore = decision.Analysis.FeasibilityScore,
                    feasibilityVerdict = decision.Analysis.FeasibilityVerdict,
                    confidence = decision.Analysis.ConfidenceLevel,
                    domainType = validationResult.DomainType,

                    // Main content
                    executiveSummary = decision.Analysis.ExecutiveSummary,
                    reasoning = decision.Analysis.ExecutiveSummary, // Keep for backward compatibility

                    // Analysis sections
                    currentPlanAnalysis = decision.Analysis.CurrentPlanAnalysis != null ? new
                    {
                        timelineAssessment = decision.Analysis.CurrentPlanAnalysis.TimelineAssessment,
                        scopeAssessment = decision.Analysis.CurrentPlanAnalysis.ScopeAssessment,
                        budgetAssessment = decision.Analysis.CurrentPlanAnalysis.BudgetAssessment,
                        resourceAssessment = decision.Analysis.CurrentPlanAnalysis.ResourceAssessment
                    } : null,

                    // Pros and Cons
                    pros = decision.Analysis.Pros ?? new List<string>(),
                    cons = decision.Analysis.Cons ?? new List<string>(),

                    // Optimized solution
                    optimizedSolution = decision.Analysis.OptimizedSolution != null ? new
                    {
                        improvedTimeline = decision.Analysis.OptimizedSolution.ImprovedTimeline,
                        clarifiedScope = decision.Analysis.OptimizedSolution.ClarifiedScope,
                        budgetOptimization = decision.Analysis.OptimizedSolution.BudgetOptimization,
                        resourceStrategy = decision.Analysis.OptimizedSolution.ResourceStrategy,
                        successProbability = decision.Analysis.OptimizedSolution.SuccessProbability
                    } : null,

                    optimizedPros = decision.Analysis.OptimizedPros ?? new List<string>(),
                    optimizedCons = decision.Analysis.OptimizedCons ?? new List<string>(),

                    // Risks with full detail
                    risks = decision.Analysis.Risks?.Select(r => new
                    {
                        description = r.Description,
                        impact = r.Impact,
                        mitigation = r.Mitigation
                    }) ?? Enumerable.Empty<object>(),

                    // Additional data
                    assumptions = decision.Analysis.Assumptions ?? new List<string>(),
                    recommendations = decision.Analysis.Recommendations ?? new List<string>(),

                    // Chart data for visualization
                    chartData = GenerateChartDataForResponse(decision.Analysis),

                    // Metadata
                    timestamp = DateTime.UtcNow,
                    modelUsed = decision.Analysis.ModelUsed ?? "gemini-1.5-flash"
                });
            }
            else
            {
                return Ok(new
                {
                    reasoning = "Analysis completed successfully. The decision context has been parsed and evaluated.",
                    domainType = validationResult.DomainType,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create temporary analysis for user {User}. Error: {Error}", User.Identity?.Name, ex.ToString());

            var userMessage = GetUserFriendlyErrorMessage(ex);
            return StatusCode(500, new
            {
                error = "InternalServerError",
                message = userMessage,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get decision schema
    /// GET /api/v2/decisions/{id}/schema
    /// </summary>
    [HttpGet("{id}/schema")]
    public async Task<ActionResult<SchemaResponse>> GetSchema(Guid id)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        if (decision.Schema == null)
            return NotFound(new { error = "Schema not generated yet" });

        return Ok(decision.Schema.ToResponse());
    }

    /// <summary>
    /// Analyze decision (generate AI reasoning)
    /// POST /api/v2/decisions/{id}/analyze
    /// </summary>
    [HttpPost("{id}/analyze")]
    public async Task<ActionResult<AnalysisResponse>> Analyze(Guid id)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        try
        {
            var analysis = await _service.AnalyzeDecisionAsync(decision, HttpContext.RequestAborted);
            await _repository.UpdateAsync(decision);
            return Ok(analysis.ToResponse());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "InternalServerError", message = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Get analysis for a decision
    /// GET /api/v2/decisions/{id}/analysis
    /// </summary>
    [HttpGet("{id}/analysis")]
    public async Task<ActionResult<AnalysisResponse>> GetAnalysis(Guid id)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        if (decision.Analysis == null)
            return NotFound(new { error = "Analysis not performed yet. Call POST /analyze first." });

        return Ok(decision.Analysis.ToResponse());
    }

    /// <summary>
    /// Replay decision with updated input
    /// POST /api/v2/decisions/{id}/replay
    /// </summary>
    [HttpPost("{id}/replay")]
    public async Task<ActionResult<ReplayResponse>> Replay(Guid id, [FromBody] ReplayDecisionRequest request)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        if (string.IsNullOrWhiteSpace(request.UpdatedInput))
            return BadRequest(new { error = "ValidationError", message = "Updated input cannot be empty" });

        try
        {
            var replayResult = await _service.ReplayDecisionAsync(
                decision,
                request.UpdatedInput,
                decision.CreatedBy,
                HttpContext.RequestAborted
            );

            return Ok(replayResult.ToResponse());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "InternalServerError", message = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Get visualizations for a decision
    /// GET /api/v2/decisions/{id}/visualizations
    /// </summary>
    [HttpGet("{id}/visualizations")]
    public async Task<ActionResult<Dictionary<string, object>>> GetVisualizations(Guid id)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        try
        {
            var visualizations = await _service.GenerateVisualizationsAsync(decision);
            return Ok(visualizations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "InternalServerError", message = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Get analytics for a decision (timeline, cost, risk level)
    /// GET /api/v2/decisions/{id}/analytics
    /// </summary>
    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<object>> GetAnalytics(Guid id)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        if (decision.Analysis == null)
            return NotFound(new { error = "Analysis not performed yet. Call POST /analyze first." });

        // Generate analytics data from analysis
        var analytics = new
        {
            feasibilityScore = (int)decision.Analysis.FeasibilityScore,
            estimatedTimeline = ExtractTimeline(decision),
            riskLevel = DetermineRiskLevel(decision.Analysis),
            timeline = GenerateTimelinePhases(decision),
            costBreakdown = GenerateCostBreakdown(decision),
            totalCost = CalculateTotalCost(decision),
            assumptions = decision.Analysis.Assumptions
        };

        return Ok(new
        {
            decision = decision.ToResponse(),
            analytics
        });
    }

    private string ExtractTimeline(DecisionV2 decision)
    {
        // Try to extract timeline from inferred attributes
        if (decision.Context?.InferredAttributes != null)
        {
            if (decision.Context.InferredAttributes.TryGetValue("estimatedDuration", out var duration))
                return duration?.ToString() ?? "Not specified";
            if (decision.Context.InferredAttributes.TryGetValue("timeline", out var timeline))
                return timeline?.ToString() ?? "Not specified";
            if (decision.Context.InferredAttributes.TryGetValue("timeframe", out var timeframe))
                return timeframe?.ToString() ?? "Not specified";
        }
        return "Not specified";
    }

    private string DetermineRiskLevel(DecisionAnalysis analysis)
    {
        if (analysis.Risks == null || !analysis.Risks.Any())
            return "Low";

        var highRiskCount = analysis.Risks.Count(r => r.Impact == "HIGH");
        var mediumRiskCount = analysis.Risks.Count(r => r.Impact == "MEDIUM");

        if (highRiskCount >= 3) return "Critical";
        if (highRiskCount >= 1) return "High";
        if (mediumRiskCount >= 3) return "Medium";
        return "Low";
    }

    private List<object> GenerateTimelinePhases(DecisionV2 decision)
    {
        // Generate generic timeline phases based on inferred attributes
        var phases = new List<object>();

        // Check if we have specific phases in inferred attributes
        if (decision.Context?.InferredAttributes != null && decision.Context.InferredAttributes.TryGetValue("phases", out var phasesObj))
        {
            // If phases are already defined, use them
            if (phasesObj is List<object> existingPhases)
                return existingPhases;
        }

        // Otherwise, generate generic phases
        phases.Add(new
        {
            name = "Planning & Design",
            duration = "20% of total time",
            description = "Requirements gathering, architecture design, resource planning",
            milestones = new[] { "Requirements finalized", "Design approved", "Team assembled" }
        });

        phases.Add(new
        {
            name = "Implementation",
            duration = "50% of total time",
            description = "Core development and execution phase",
            milestones = new[] { "Milestone 1 complete", "Mid-point review", "Milestone 2 complete" }
        });

        phases.Add(new
        {
            name = "Testing & Validation",
            duration = "20% of total time",
            description = "Quality assurance, user acceptance testing, bug fixes",
            milestones = new[] { "QA testing complete", "UAT passed", "Bug fixes deployed" }
        });

        phases.Add(new
        {
            name = "Deployment & Closure",
            duration = "10% of total time",
            description = "Final deployment, documentation, handoff",
            milestones = new[] { "Production deployment", "Documentation complete", "Project closed" }
        });

        return phases;
    }

    private Dictionary<string, object> GenerateCostBreakdown(DecisionV2 decision)
    {
        var breakdown = new Dictionary<string, object>();

        // Try to extract budget from inferred attributes
        if (decision.Context?.InferredAttributes != null && decision.Context.InferredAttributes.TryGetValue("budget", out var budgetObj))
        {
            if (budgetObj is string budgetStr && decimal.TryParse(budgetStr.Replace("$", "").Replace(",", ""), out var budget))
            {
                // Generate realistic breakdown
                breakdown["labor"] = Math.Round(budget * 0.6m, 2);
                breakdown["materials"] = Math.Round(budget * 0.25m, 2);
                breakdown["overhead"] = Math.Round(budget * 0.10m, 2);
                breakdown["contingency"] = Math.Round(budget * 0.05m, 2);
                return breakdown;
            }
        }

        // Default breakdown if no budget specified
        breakdown["labor"] = "TBD";
        breakdown["materials"] = "TBD";
        breakdown["overhead"] = "TBD";
        breakdown["contingency"] = "TBD";
        return breakdown;
    }

    private object CalculateTotalCost(DecisionV2 decision)
    {
        if (decision.Context?.InferredAttributes != null && decision.Context.InferredAttributes.TryGetValue("budget", out var budgetObj))
        {
            if (budgetObj is string budgetStr)
            {
                // Clean up the string and try to parse
                var cleanBudget = budgetStr.Replace("$", "").Replace(",", "").Trim();
                if (decimal.TryParse(cleanBudget, out var budget))
                    return budget;
                return budgetStr; // Return as-is if can't parse
            }
        }
        return "Not specified";
    }

    /// <summary>
    /// Ask a question about a decision
    /// POST /api/v2/decisions/{id}/query
    /// </summary>
    [HttpPost("{id}/query")]
    public async Task<ActionResult<object>> Query(Guid id, [FromBody] QueryRequest request)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "ValidationError", message = "Question cannot be empty" });

        try
        {
            var response = await _service.QueryDecisionAsync(decision, request.Question, HttpContext.RequestAborted);
            return Ok(new { question = request.Question, answer = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "InternalServerError", message = ex.Message, timestamp = DateTime.UtcNow });
        }
    }

    /// <summary>
    /// Commit a decision (finalize it)
    /// POST /api/v2/decisions/{id}/commit
    /// </summary>
    [HttpPost("{id}/commit")]
    public async Task<ActionResult<DecisionV2Response>> Commit(Guid id)
    {
        var decision = await _repository.GetByIdAsync(id);
        if (decision == null)
            return NotFound(new { error = "Decision not found" });

        try
        {
            decision.CommitDecision();
            await _repository.UpdateAsync(decision);
            return Ok(decision.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "BadRequest", message = ex.Message });
        }
    }

    /// <summary>
    /// Get all decisions
    /// GET /api/v2/decisions
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DecisionV2Response>>> GetAll()
    {
        var decisions = await _repository.GetAllAsync();
        return Ok(decisions.Select(d => d.ToResponse()).ToList());
    }

    /// <summary>
    /// Generate chart data in response format for anonymous objects
    /// </summary>
    private object? GenerateChartDataForResponse(DecisionAnalysis analysis)
    {
        var chartData = analysis.GenerateChartData();
        if (chartData == null) return null;

        return new
        {
            timeline = chartData.Timeline.Select(t => new
            {
                time = t.Time,
                feasibilityScore = t.FeasibilityScore,
                timelinePressure = t.TimelinePressure,
                resourceAdequacy = t.ResourceAdequacy,
                scopeComplexity = t.ScopeComplexity
            }).ToList(),
            performance = chartData.Performance.Select(p => new
            {
                resource = p.Resource,
                allocated = p.Allocated,
                required = p.Required,
                gap = p.Gap
            }).ToList(),
            riskHeatmap = chartData.RiskHeatmap.Select(r => new
            {
                factor = r.Factor,
                impact = r.Impact,
                status = r.Status,
                trend = r.Trend,
                description = r.Description
            }).ToList()
        };
    }

    /// <summary>
    /// Convert technical exception messages to user-friendly error messages
    /// </summary>
    private string GetUserFriendlyErrorMessage(Exception ex)
    {
        var message = ex.Message.ToLower();

        // Check for specific AI/API related errors
        if (message.Contains("ai analysis is temporarily unavailable due to high demand"))
            return "AI analysis is currently at capacity. Please try again in a few minutes.";

        if (message.Contains("ai analysis service is not properly configured"))
            return "AI features are currently unavailable. Your decision will be saved with basic analysis.";

        if (message.Contains("ai analysis took too long"))
            return "The analysis is taking longer than expected. Try using a shorter description.";

        if (message.Contains("mongodb") || message.Contains("database"))
            return "There was an issue saving your decision. Please try again.";

        if (message.Contains("timeout") || message.Contains("took too long"))
            return "The request is taking longer than expected. Please try again with a shorter description.";

        if (message.Contains("authentication") || message.Contains("unauthorized"))
            return "Your session has expired. Please log in again.";

        // Default user-friendly message
        return "We're experiencing technical difficulties. Your request could not be completed at this time. Please try again.";
    }
}

public record QueryRequest(string Question);
