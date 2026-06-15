using System.IO.Compression;
using System.Security;
using System.Text;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

public sealed class ExcelExportService
{
    public byte[] Export(
        DecisionV2 decision,
        DecisionVersion version,
        ReplayComparison? replay = null)
    {
        var sheets = new List<Sheet>
        {
            new("Decision Summary", DecisionSummaryRows(decision, version)),
            new("Feasibility Breakdown", FeasibilityRows(version)),
            new("Action Plan", ActionPlanRows(version.Plan)),
            new("Risks and Assumptions", RiskRows(version)),
            new("Replay Comparison", ReplayRows(replay))
        };

        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddText(archive, "[Content_Types].xml", ContentTypes(sheets.Count));
            AddText(archive, "_rels/.rels", RootRelationships());
            AddText(archive, "xl/workbook.xml", Workbook(sheets));
            AddText(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(sheets.Count));
            AddText(archive, "xl/styles.xml", Styles());
            for (var index = 0; index < sheets.Count; index++)
                AddText(archive, $"xl/worksheets/sheet{index + 1}.xml", Worksheet(sheets[index].Rows));
        }
        return output.ToArray();
    }

    private static List<IReadOnlyList<object?>> DecisionSummaryRows(
        DecisionV2 decision,
        DecisionVersion version) =>
        new List<IReadOnlyList<object?>>
        {
            Row("Field", "Value"),
            Row("Decision ID", decision.Id),
            Row("Version", version.Version),
            Row("Domain", version.StructuredData.Domain),
            Row("Title", version.StructuredData.Title),
            Row("Goal", version.StructuredData.Goal),
            Row("Feasibility Score", version.Feasibility.FeasibilityScore),
            Row("Risk Level", version.Feasibility.RiskLevel),
            Row("Explanation", version.Explanation),
            Row("Created At", version.CreatedAt)
        }
        .Concat(version.StructuredData.Fields.OrderBy(pair => pair.Key)
            .Select(pair => Row(pair.Key, pair.Value)))
        .ToList();

    private static List<IReadOnlyList<object?>> FeasibilityRows(DecisionVersion version)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            Row("Factor", "Score", "Weight", "Weighted Score", "Reason", "Assumption", "Confidence")
        };
        rows.AddRange(version.Feasibility.FactorBreakdown.Select(factor => Row(
            factor.Factor,
            factor.Score,
            factor.Weight,
            factor.WeightedScore,
            factor.Reason,
            factor.Assumption,
            factor.Confidence)));
        return rows;
    }

    private static List<IReadOnlyList<object?>> ActionPlanRows(ActionPlan? plan)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            Row("Phase", "Start Week", "End Week", "Start Date", "End Date", "Goal", "Task", "Description", "Priority", "Effort", "Dependencies", "Risk Notes", "Success Criteria")
        };
        if (plan == null) return rows;
        foreach (var phase in plan.Phases)
        foreach (var task in phase.Tasks)
            rows.Add(Row(
                phase.PhaseName,
                phase.StartWeek,
                phase.EndWeek,
                phase.StartDate,
                phase.EndDate,
                phase.Goal,
                task.TaskName,
                task.Description,
                task.Priority,
                task.EstimatedEffort,
                string.Join(", ", task.Dependencies),
                task.RiskNotes,
                task.SuccessCriteria));
        return rows;
    }

    private static List<IReadOnlyList<object?>> RiskRows(DecisionVersion version)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            Row("Type", "Factor/Field", "Severity", "Description", "Mitigation")
        };
        rows.AddRange(version.Feasibility.Risks.Select(risk => Row(
            "Risk", risk.Factor, risk.Severity, risk.Message, risk.Mitigation)));
        rows.AddRange(version.StructuredData.Assumptions.Select(assumption => Row(
            "Assumption", "", "", assumption, "")));
        rows.AddRange(version.Validation.MissingFields.Select(field => Row(
            "Missing Field", field, "Medium", $"Required field '{field}' was not supplied.", $"Confirm '{field}' and replay the decision.")));
        return rows;
    }

    private static List<IReadOnlyList<object?>> ReplayRows(ReplayComparison? replay)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            Row("Previous Version", "New Version", "Field", "From", "To", "Score Delta", "Risk Delta", "Main Reason")
        };
        if (replay == null) return rows;
        if (replay.ChangedFields.Count == 0)
            rows.Add(Row(replay.PreviousVersion, replay.NewVersion, "", "", "", replay.ScoreDelta, replay.RiskDelta, replay.MainReason));
        else
            rows.AddRange(replay.ChangedFields.Select(change => Row(
                replay.PreviousVersion,
                replay.NewVersion,
                change.Field,
                change.From,
                change.To,
                replay.ScoreDelta,
                replay.RiskDelta,
                replay.MainReason)));
        return rows;
    }

    private static string Worksheet(IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            builder.Append($"<row r=\"{rowIndex + 1}\">");
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Count; columnIndex++)
            {
                var reference = CellReference(columnIndex + 1, rowIndex + 1);
                builder.Append(Cell(reference, rows[rowIndex][columnIndex], rowIndex == 0));
            }
            builder.Append("</row>");
        }
        builder.Append("</sheetData></worksheet>");
        return builder.ToString();
    }

    private static string Cell(string reference, object? value, bool header)
    {
        if (value is byte or short or int or long or float or double or decimal)
            return $"<c r=\"{reference}\" s=\"{(header ? 1 : 0)}\"><v>{Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)}</v></c>";

        var text = value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("yyyy-MM-dd"),
            _ => value.ToString() ?? string.Empty
        };
        return $"<c r=\"{reference}\" t=\"inlineStr\" s=\"{(header ? 1 : 0)}\"><is><t>{XmlEscape(text)}</t></is></c>";
    }

    private static string ContentTypes(int sheetCount)
    {
        var sheets = string.Join("", Enumerable.Range(1, sheetCount).Select(index =>
            $"<Override PartName=\"/xl/worksheets/sheet{index}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"));
        return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>{sheets}</Types>""";
    }

    private static string RootRelationships() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private static string Workbook(IReadOnlyList<Sheet> sheets)
    {
        var entries = string.Join("", sheets.Select((sheet, index) =>
            $"<sheet name=\"{XmlEscape(sheet.Name)}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>"));
        return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>{entries}</sheets></workbook>""";
    }

    private static string WorkbookRelationships(int sheetCount)
    {
        var relationships = string.Join("", Enumerable.Range(1, sheetCount).Select(index =>
            $"<Relationship Id=\"rId{index}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{index}.xml\"/>"));
        return $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{relationships}<Relationship Id="rId{sheetCount + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""";
    }

    private static string Styles() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font/><font><b/></font></fonts><fills count="1"><fill><patternFill patternType="none"/></fill></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf/></cellStyleXfs><cellXfs count="2"><xf xfId="0"/><xf xfId="0" fontId="1" applyFont="1"/></cellXfs></styleSheet>""";

    private static string CellReference(int column, int row)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return $"{name}{row}";
    }

    private static IReadOnlyList<object?> Row(params object?[] values) => values;

    private static string XmlEscape(string value) =>
        SecurityElement.Escape(value) ?? string.Empty;

    private static void AddText(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private sealed record Sheet(string Name, List<IReadOnlyList<object?>> Rows);
}
