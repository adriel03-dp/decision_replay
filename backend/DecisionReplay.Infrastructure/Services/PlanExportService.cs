using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

public sealed class PlanExportService
{
    private readonly PdfExportService _pdf;
    private readonly ExcelExportService _excel;

    public PlanExportService(
        PdfExportService pdf,
        ExcelExportService excel)
    {
        _pdf = pdf;
        _excel = excel;
    }

    public ExportedFile ExportPdf(
        DecisionV2 decision,
        DecisionVersion version,
        ReplayComparison? replay = null) =>
        new(
            _pdf.Export(decision, version, replay),
            "application/pdf",
            $"decision-{decision.Id}-v{version.Version}-plan.pdf");

    public ExportedFile ExportExcel(
        DecisionV2 decision,
        DecisionVersion version,
        ReplayComparison? replay = null) =>
        new(
            _excel.Export(decision, version, replay),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"decision-{decision.Id}-v{version.Version}-plan.xlsx");
}

public sealed record ExportedFile(
    byte[] Content,
    string ContentType,
    string FileName);
