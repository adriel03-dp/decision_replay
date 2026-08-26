using DecisionReplay.Application.Services;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Persistence.Repositories;

namespace DecisionReplay.Tests;

internal static class WorkflowFixture
{
    public const string Narrative = "Build a mobile app in six months with a team of six and budget 100000 USD; scope: login, payments, reporting; monthly_cost_per_person: 2000; technical_readiness: partial";
    public static StructuredDecisionData Project() => new()
    {
        Domain = "project_planning", Title = "Mobile application", Goal = "Deliver a mobile application",
        Fields = new(StringComparer.OrdinalIgnoreCase)
        {
            ["project_goal"] = "Deliver a mobile application", ["budget"] = "100000", ["timeline_months"] = "6",
            ["team_size"] = "6", ["currency"] = "USD", ["scope"] = "login, payments, reporting",
            ["monthly_cost_per_person"] = "2000", ["technical_readiness"] = "partial"
        }
    };
    public static DecisionEngineService Engine(InMemoryDecisionV2Repository repository)
    {
        var language = new RulesLanguageService(); var templates = new DomainTemplateService();
        return new(new(language, templates), new(), templates, new(), new(), new(language), new(new(), language), new(), language, repository);
    }
    public static ImprovementAcceptanceService Acceptance(InMemoryDecisionV2Repository repository) =>
        new(repository, new(), new(), new(), new(), new(new(), new()), new(), new());
    public static RequirementsReviewService Review() => new(new(new RulesLanguageService(), new DomainTemplateService()), new(), new());
}
