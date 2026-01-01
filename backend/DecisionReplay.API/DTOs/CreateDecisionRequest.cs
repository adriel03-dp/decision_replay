namespace DecisionReplay.API.DTOs;

public record CreateDecisionRequest(
    string Title,              // e.g., "Launch MVP Product"
    string Scope,              // Features, deliverables, quality standards
    string Timeline,           // Target dates, milestones, duration
    string Resources,          // Team size, budget, tools, skills
    string Constraints,        // Risks, dependencies, technical debt, limitations
    string CreatedBy
);
