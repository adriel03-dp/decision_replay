using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

public sealed class DomainTemplateService
{
    private readonly IReadOnlyDictionary<string, DecisionDomainTemplate> _templates;

    public DomainTemplateService()
    {
        _templates = BuildTemplates().ToDictionary(
            template => template.Domain,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<DecisionDomainTemplate> GetAll() => _templates.Values.ToList();

    public DecisionDomainTemplate Get(string domain) =>
        _templates.TryGetValue(domain, out var template)
            ? template
            : _templates["project_planning"];

    public bool IsSupported(string domain) => _templates.ContainsKey(domain);

    private static IEnumerable<DecisionDomainTemplate> BuildTemplates()
    {
        yield return Template(
            "business_startup",
            "Business Startup",
            Fields(
                Required("business_idea", "The product or service being created", DecisionFieldType.Text),
                Required("budget", "Available startup budget", DecisionFieldType.Currency),
                Required("timeline_months", "Time available before launch", DecisionFieldType.Number),
                Required("team_size", "People available to execute", DecisionFieldType.Integer),
                Required("target_market", "Primary customer segment", DecisionFieldType.Text),
                Optional("market_evidence", "Evidence of customer demand", DecisionFieldType.Level),
                Optional("legal_readiness", "Legal and regulatory preparedness", DecisionFieldType.Level),
                Optional("operating_experience", "Relevant operating experience", DecisionFieldType.Level),
                Optional("scope", "Initial launch scope", DecisionFieldType.List)),
            Factors(
                Factor("budget_feasibility", .25, "budget_feasibility", "budget", "timeline_months", "team_size"),
                Factor("timeline_feasibility", .20, "timeline_feasibility", "timeline_months", "scope", "team_size"),
                Factor("market_risk", .20, "market_evidence", "market_evidence", "target_market"),
                Factor("resource_availability", .15, "resource_availability", "team_size", "operating_experience"),
                Factor("operational_complexity", .10, "operational_complexity", "scope", "team_size"),
                Factor("legal_risk", .10, "legal_readiness", "legal_readiness")),
            Risks(
                Risk("budget_feasibility", 55, "High", "Available budget is below the deterministic operating estimate."),
                Risk("market_risk", 50, "High", "Market demand has not been validated sufficiently."),
                Risk("legal_risk", 45, "High", "Legal or regulatory readiness is incomplete.")),
            new[] { "budget", "timeline_months", "team_size", "scope", "market_evidence", "legal_readiness" },
            PlanTasks(
                Task("validate", "Validate customer demand", "Interview the target market and test the core problem.", "Document at least 20 qualified responses."),
                Task("setup", "Establish operating foundations", "Confirm legal structure, ownership, budget controls, and delivery responsibilities.", "Legal and operating checklist is approved."),
                Task("execute", "Build the minimum viable offer", "Deliver only the scope required to test willingness to pay.", "A usable offer is available to pilot customers."),
                Task("optimize", "Measure pilot economics", "Track conversion, delivery cost, retention signals, and operational bottlenecks.", "Pilot metrics and corrective actions are documented."),
                Task("launch", "Launch with controls", "Release to the selected segment with monitoring and contingency thresholds.", "Launch criteria and stop conditions are signed off.")));

        yield return Template(
            "career_decision",
            "Career Decision",
            Fields(
                Required("current_role", "Current role or situation", DecisionFieldType.Text),
                Required("target_role", "Desired role or outcome", DecisionFieldType.Text),
                Required("timeline_months", "Desired transition period", DecisionFieldType.Number),
                Required("savings_months", "Months of financial runway", DecisionFieldType.Number),
                Required("skill_readiness", "Current skill readiness", DecisionFieldType.Level),
                Optional("market_demand", "Demand for the target role", DecisionFieldType.Level),
                Optional("support_level", "Mentor or network support", DecisionFieldType.Level),
                Optional("constraints", "Personal constraints", DecisionFieldType.List)),
            Factors(
                Factor("skill_readiness", .25, "skill_readiness", "skill_readiness"),
                Factor("financial_resilience", .20, "financial_resilience", "savings_months"),
                Factor("market_demand", .20, "market_demand", "market_demand"),
                Factor("timeline_feasibility", .15, "career_timeline", "timeline_months", "skill_readiness"),
                Factor("support_network", .10, "support_level", "support_level"),
                Factor("constraint_load", .10, "constraint_load", "constraints")),
            Risks(
                Risk("financial_resilience", 50, "High", "Financial runway may be insufficient for the transition."),
                Risk("skill_readiness", 50, "High", "Current skills do not yet meet the target role threshold.")),
            new[] { "target_role", "timeline_months", "savings_months", "skill_readiness", "market_demand" },
            PlanTasks(
                Task("validate", "Validate the target role", "Compare the target against real job descriptions and practitioner interviews.", "A documented target-role competency profile exists."),
                Task("setup", "Close priority skill gaps", "Create a focused learning and portfolio plan.", "The top three skill gaps have evidence-based learning tasks."),
                Task("execute", "Build transition evidence", "Complete portfolio work, applications, networking, or internal transfer actions.", "At least two credible transition signals are produced."),
                Task("optimize", "Review market feedback", "Use interview and application results to adjust positioning.", "Positioning changes are tied to observed feedback."),
                Task("launch", "Commit to the transition", "Execute the selected move with financial and fallback controls.", "Offer, transfer, or validated transition milestone is achieved.")));

        yield return Template(
            "education_path",
            "Education Path",
            Fields(
                Required("program", "Program, qualification, or learning path", DecisionFieldType.Text),
                Required("timeline_months", "Program duration", DecisionFieldType.Number),
                Required("budget", "Available education budget", DecisionFieldType.Currency),
                Required("weekly_hours", "Weekly study capacity", DecisionFieldType.Number),
                Required("career_alignment", "Alignment to the intended outcome", DecisionFieldType.Level),
                Optional("entry_readiness", "Readiness for entry requirements", DecisionFieldType.Level),
                Optional("program_value", "Evidence of program quality and outcomes", DecisionFieldType.Level),
                Optional("constraints", "Work, family, or location constraints", DecisionFieldType.List)),
            Factors(
                Factor("budget_feasibility", .20, "education_budget", "budget", "timeline_months"),
                Factor("time_capacity", .20, "time_capacity", "weekly_hours"),
                Factor("career_alignment", .25, "career_alignment", "career_alignment"),
                Factor("entry_readiness", .15, "entry_readiness", "entry_readiness"),
                Factor("program_value", .10, "program_value", "program_value"),
                Factor("constraint_load", .10, "constraint_load", "constraints")),
            Risks(
                Risk("time_capacity", 50, "High", "Weekly study capacity is below the expected workload."),
                Risk("career_alignment", 50, "High", "The program is weakly aligned with the intended outcome.")),
            new[] { "program", "timeline_months", "budget", "weekly_hours", "career_alignment" },
            PlanTasks(
                Task("validate", "Validate program outcomes", "Check curriculum, completion rates, accreditation, and graduate outcomes.", "Program evidence supports the intended outcome."),
                Task("setup", "Prepare study capacity", "Reserve weekly time, funding, tools, and prerequisite learning.", "A sustainable weekly schedule and budget are confirmed."),
                Task("execute", "Complete core learning", "Follow the curriculum while producing evidence of competency.", "Required assessments and portfolio evidence are on track."),
                Task("optimize", "Review learning effectiveness", "Adjust study methods based on assessment results and workload.", "Weak areas have corrective study actions."),
                Task("launch", "Convert learning into outcome", "Use the qualification or skills in applications, projects, or progression.", "A measurable career or education outcome is achieved.")));

        yield return Template(
            "product_launch",
            "Product Launch",
            Fields(
                Required("product_goal", "The product and launch objective", DecisionFieldType.Text),
                Required("target_market", "Primary users or buyers", DecisionFieldType.Text),
                Required("budget", "Available launch budget", DecisionFieldType.Currency),
                Required("timeline_months", "Launch timeline", DecisionFieldType.Number),
                Required("team_size", "Delivery team size", DecisionFieldType.Integer),
                Required("scope", "Features included in launch", DecisionFieldType.List),
                Optional("market_evidence", "Validation evidence", DecisionFieldType.Level),
                Optional("technical_readiness", "Technical readiness", DecisionFieldType.Level),
                Optional("dependencies", "External dependencies", DecisionFieldType.List)),
            Factors(
                Factor("budget_feasibility", .20, "product_budget", "budget", "timeline_months", "team_size"),
                Factor("timeline_feasibility", .20, "timeline_feasibility", "timeline_months", "scope", "team_size"),
                Factor("market_risk", .20, "market_evidence", "market_evidence", "target_market"),
                Factor("scope_clarity", .15, "scope_clarity", "scope"),
                Factor("technical_readiness", .15, "technical_readiness", "technical_readiness"),
                Factor("dependency_risk", .10, "dependency_risk", "dependencies")),
            Risks(
                Risk("market_risk", 50, "High", "Launch demand is insufficiently validated."),
                Risk("technical_readiness", 50, "High", "Technical readiness is below the launch threshold."),
                Risk("dependency_risk", 45, "High", "External dependencies create material launch risk.")),
            new[] { "budget", "timeline_months", "team_size", "scope", "market_evidence", "technical_readiness", "dependencies" },
            PlanTasks(
                Task("validate", "Validate launch demand", "Test the target segment, problem, positioning, and willingness to adopt.", "Validation evidence meets the launch threshold."),
                Task("setup", "Freeze launch scope", "Define the minimum launch scope, owners, dependencies, and release controls.", "Scope and dependency register are approved."),
                Task("execute", "Build the launch candidate", "Deliver the approved scope with instrumentation and support readiness.", "Launch candidate passes internal acceptance criteria."),
                Task("optimize", "Test and harden", "Run user acceptance, reliability, security, and operational readiness checks.", "Critical defects are closed and go-live metrics are ready."),
                Task("launch", "Release and review", "Launch in controlled stages and monitor adoption and operational health.", "Go-live success criteria are met or rollback is executed.")));

        yield return Template(
            "project_planning",
            "Project Planning",
            Fields(
                Required("project_goal", "The intended project outcome", DecisionFieldType.Text),
                Required("budget", "Available project budget", DecisionFieldType.Currency),
                Required("timeline_months", "Delivery timeline", DecisionFieldType.Number),
                Required("team_size", "Available team size", DecisionFieldType.Integer),
                Required("scope", "Project deliverables", DecisionFieldType.List),
                Optional("dependencies", "External or internal dependencies", DecisionFieldType.List),
                Optional("technical_readiness", "Delivery readiness", DecisionFieldType.Level),
                Optional("constraints", "Known constraints", DecisionFieldType.List)),
            Factors(
                Factor("budget_feasibility", .25, "project_budget", "budget", "timeline_months", "team_size"),
                Factor("timeline_feasibility", .25, "timeline_feasibility", "timeline_months", "scope", "team_size"),
                Factor("resource_availability", .20, "resource_availability", "team_size", "technical_readiness"),
                Factor("scope_clarity", .15, "scope_clarity", "scope"),
                Factor("dependency_risk", .10, "dependency_risk", "dependencies"),
                Factor("constraint_load", .05, "constraint_load", "constraints")),
            Risks(
                Risk("budget_feasibility", 50, "High", "Budget does not cover the deterministic delivery estimate."),
                Risk("timeline_feasibility", 50, "High", "The selected timeline is shorter than the calculated delivery duration."),
                Risk("dependency_risk", 45, "High", "Dependencies are likely to delay delivery.")),
            new[] { "budget", "timeline_months", "team_size", "scope", "dependencies", "constraints" },
            PlanTasks(
                Task("validate", "Validate requirements", "Confirm the outcome, users, constraints, and acceptance criteria.", "Requirements and acceptance criteria are approved."),
                Task("setup", "Establish delivery controls", "Assign owners, dependencies, budget controls, and delivery cadence.", "Delivery baseline and responsibility matrix are approved."),
                Task("execute", "Deliver the core scope", "Execute the prioritized work in measurable increments.", "Core deliverables pass their acceptance criteria."),
                Task("optimize", "Test and correct", "Validate quality, performance, and stakeholder readiness.", "Critical issues are resolved or accepted."),
                Task("launch", "Release and close", "Deploy, hand over, measure outcomes, and record lessons.", "Outcome metrics and ownership transfer are complete.")));
    }

    private static DecisionDomainTemplate Template(
        string domain,
        string displayName,
        IReadOnlyList<DecisionFieldDefinition> fields,
        IReadOnlyList<ScoringFactorDefinition> factors,
        IReadOnlyList<DomainRiskRule> risks,
        IReadOnlyList<string> replayFields,
        IReadOnlyList<DomainPlanTaskTemplate> planTasks) =>
        new()
        {
            Domain = domain,
            DisplayName = displayName,
            Fields = fields,
            ScoringFactors = factors,
            RiskRules = risks,
            ReplaySensitiveFields = replayFields,
            PlanTasks = planTasks
        };

    private static IReadOnlyList<DecisionFieldDefinition> Fields(params DecisionFieldDefinition[] fields) => fields;
    private static IReadOnlyList<ScoringFactorDefinition> Factors(params ScoringFactorDefinition[] factors) => factors;
    private static IReadOnlyList<DomainRiskRule> Risks(params DomainRiskRule[] risks) => risks;
    private static IReadOnlyList<DomainPlanTaskTemplate> PlanTasks(params DomainPlanTaskTemplate[] tasks) => tasks;

    private static DecisionFieldDefinition Required(string name, string description, DecisionFieldType type) =>
        new() { Name = name, Description = description, Type = type, Required = true };

    private static DecisionFieldDefinition Optional(string name, string description, DecisionFieldType type) =>
        new() { Name = name, Description = description, Type = type, Required = false };

    private static ScoringFactorDefinition Factor(
        string name,
        double weight,
        string evaluator,
        params string[] fields) =>
        new() { Name = name, Weight = weight, Evaluator = evaluator, SourceFields = fields };

    private static DomainRiskRule Risk(string factor, double threshold, string severity, string message) =>
        new() { Factor = factor, TriggerBelow = threshold, Severity = severity, Message = message };

    private static DomainPlanTaskTemplate Task(
        string phase,
        string name,
        string description,
        string successCriteria) =>
        new()
        {
            PhaseKey = phase,
            TaskName = name,
            Description = description,
            SuccessCriteria = successCriteria
        };
}
