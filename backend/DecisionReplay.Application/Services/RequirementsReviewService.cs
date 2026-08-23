using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Application.Services;

/// <summary>Read-only requirement preparation. No assessment, plan or decision is persisted.</summary>
public sealed class RequirementsReviewService(DecisionParserService parser, DomainTemplateService templates,
    DecisionValidationService validation)
{
    public IReadOnlyList<RequirementDomain> Domains() => templates.GetAll().Select(template => new RequirementDomain(
        template.Domain, template.DisplayName, GoalField(template.Domain),
        template.Fields.Select(field => new RequirementField(field.Name, field.Description, field.Type.ToString(), field.Required)).ToList(),
        template.Fields.Any(field => field.Name == "fixed_fields"))).ToList();

    public async Task<RequirementsReview> PreviewAsync(string input, IReadOnlyCollection<string>? fixedFields = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(input) || input.Trim().Length is < 10 or > 20000)
            throw new ArgumentException("Describe the requirements in 10 to 20000 characters.");
        var data = await parser.ParseAsync(input, cancellationToken);
        var template = templates.Get(data.Domain);
        ImprovementConstraintPolicy.ApplyTextPolicy(data, template, input, fixedFields);
        return Review(data);
    }

    public RequirementsReview Review(StructuredDecisionData data)
    {
        if (!templates.IsSupported(data.Domain)) throw new ArgumentException("Unsupported decision domain.");
        if (data.Fields.Count > 100 || data.Fields.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > 100 || pair.Value == null || pair.Value.Length > 20000))
            throw new ArgumentException("Supply at most 100 bounded, non-null fields.");
        data.Domain = templates.Get(data.Domain).Domain;
        data.Goal = data.Goal.Trim();
        data.Title = string.IsNullOrWhiteSpace(data.Title) ? data.Goal[..Math.Min(80, data.Goal.Length)] : data.Title.Trim();
        var checkedData = validation.Validate(data, templates.Get(data.Domain));
        if (data.Goal.Length is < 10 or > 20000) checkedData.Errors.Add("The goal must contain 10 to 20000 characters.");
        if (data.Title.Length > 200) checkedData.Errors.Add("The title must contain at most 200 characters.");
        data.MissingFields = checkedData.MissingFields.ToList();
        return new(data, checkedData);
    }

    private static string? GoalField(string domain) => domain switch
    { "project_planning" => "project_goal", "product_launch" => "product_goal", "business_startup" => "business_idea", "education_path" => "program", _ => null };
}
