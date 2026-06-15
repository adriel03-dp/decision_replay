using System.IO.Compression;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.Enums;
using DecisionReplay.Domain.ValueObjects;
using DecisionReplay.Infrastructure.Services;

namespace DecisionReplay.Tests;

public sealed class PlanExportServiceTests
{
    [Fact]
    public void Exports_ProduceValidPdfAndWorkbookSignatures()
    {
        var version = new DecisionVersion
        {
            Version = 1,
            NaturalLanguageInput = "Launch a product & validate demand",
            StructuredData = new StructuredDecisionData
            {
                Domain = "product_launch",
                Title = "Launch <pilot>",
                Goal = "Validate demand & launch safely",
                Fields = new Dictionary<string, string>
                {
                    ["budget"] = "35000",
                    ["timeline_months"] = "6"
                }
            },
            Validation = new DecisionValidationResult(),
            Feasibility = new FeasibilityAssessment
            {
                FeasibilityScore = 68,
                RiskLevel = DecisionRiskLevel.Medium,
                FactorBreakdown =
                {
                    new FactorBreakdown
                    {
                        Factor = "budget_feasibility",
                        Score = 70,
                        Weight = .25,
                        WeightedScore = 17.5,
                        Reason = "Budget is acceptable & constrained.",
                        Confidence = FactorConfidence.High
                    }
                }
            },
            Explanation = "The backend calculated a constrained but viable result."
        };
        var decision = new DecisionV2(Guid.NewGuid(), version.NaturalLanguageInput, "test-user", version);
        var exports = new PlanExportService(new PdfExportService(), new ExcelExportService());

        var pdf = exports.ExportPdf(decision, version);
        var excel = exports.ExportExcel(decision, version);

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Content, 0, 4));
        Assert.Equal(new byte[] { 0x50, 0x4B }, excel.Content[..2]);
        using var archive = new ZipArchive(new MemoryStream(excel.Content), ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.Equal(5, archive.Entries.Count(entry => entry.FullName.StartsWith("xl/worksheets/")));
    }
}
