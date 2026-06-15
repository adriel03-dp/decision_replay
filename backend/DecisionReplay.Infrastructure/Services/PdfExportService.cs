using System.Globalization;
using System.Text;
using DecisionReplay.Domain.Entities;
using DecisionReplay.Domain.ValueObjects;

namespace DecisionReplay.Infrastructure.Services;

public sealed class PdfExportService
{
    public byte[] Export(
        DecisionV2 decision,
        DecisionVersion version,
        ReplayComparison? replay = null)
    {
        var lines = BuildLines(decision, version, replay)
            .SelectMany(line => Wrap(line, 92))
            .ToList();
        var pages = lines.Chunk(48).Select(chunk => chunk.ToList()).ToList();
        if (pages.Count == 0) pages.Add(new List<string> { "Decision Replay Report" });

        var objects = new Dictionary<int, byte[]>();
        var pageObjectIds = new List<int>();
        var contentObjectIds = new List<int>();
        var nextId = 4;
        foreach (var _ in pages)
        {
            pageObjectIds.Add(nextId++);
            contentObjectIds.Add(nextId++);
        }

        objects[1] = Bytes("<< /Type /Catalog /Pages 2 0 R >>");
        objects[2] = Bytes(
            $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(" ", pageObjectIds.Select(id => $"{id} 0 R"))}] >>");
        objects[3] = Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        for (var index = 0; index < pages.Count; index++)
        {
            var pageId = pageObjectIds[index];
            var contentId = contentObjectIds[index];
            objects[pageId] = Bytes(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                $"/Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");

            var content = BuildPageContent(pages[index], index + 1, pages.Count);
            var contentBytes = Encoding.ASCII.GetBytes(content);
            objects[contentId] = Combine(
                Bytes($"<< /Length {contentBytes.Length} >>\nstream\n"),
                contentBytes,
                Bytes("\nendstream"));
        }

        return BuildPdf(objects);
    }

    private static IEnumerable<string> BuildLines(
        DecisionV2 decision,
        DecisionVersion version,
        ReplayComparison? replay)
    {
        yield return "DECISION REPLAY - CONSTRAINT-BASED DECISION REPORT";
        yield return "";
        yield return $"Decision ID: {decision.Id}";
        yield return $"Version: {version.Version}";
        yield return $"Domain: {version.StructuredData.Domain}";
        yield return $"Title: {version.StructuredData.Title}";
        yield return $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        yield return "";
        yield return "EXECUTIVE SUMMARY";
        yield return version.Explanation;
        yield return "";
        yield return "DECISION OVERVIEW";
        yield return $"Goal: {version.StructuredData.Goal}";
        foreach (var field in version.StructuredData.Fields.OrderBy(pair => pair.Key))
            yield return $"{field.Key.Replace('_', ' ')}: {field.Value}";
        yield return "";
        yield return $"FEASIBILITY SCORE: {version.Feasibility.FeasibilityScore:0.#}/100";
        yield return $"RISK LEVEL: {version.Feasibility.RiskLevel}";
        yield return "";
        yield return "FACTOR BREAKDOWN";
        foreach (var factor in version.Feasibility.FactorBreakdown)
            yield return $"{factor.Factor}: {factor.Score:0.#}/100 x {factor.Weight:0.##} = {factor.WeightedScore:0.##}. {factor.Reason}";
        yield return "";
        yield return "RISKS";
        foreach (var risk in version.Feasibility.Risks)
            yield return $"[{risk.Severity}] {risk.Message} Mitigation: {risk.Mitigation}";
        if (version.Feasibility.Risks.Count == 0) yield return "No material rule-based risks identified.";
        yield return "";
        yield return "ASSUMPTIONS AND MISSING FIELDS";
        foreach (var assumption in version.StructuredData.Assumptions)
            yield return $"Assumption: {assumption}";
        foreach (var missing in version.Validation.MissingFields)
            yield return $"Missing: {missing}";
        if (version.StructuredData.Assumptions.Count == 0 && version.Validation.MissingFields.Count == 0)
            yield return "No extraction assumptions or required missing fields.";
        yield return "";
        yield return "ACTION PLAN";
        if (version.Plan != null)
        {
            yield return $"Plan period: {version.Plan.StartDate:yyyy-MM-dd} to {version.Plan.EndDate:yyyy-MM-dd}";
            foreach (var phase in version.Plan.Phases)
            {
                yield return $"{phase.PhaseName} (week {phase.StartWeek}-{phase.EndWeek}): {phase.Goal}";
                foreach (var task in phase.Tasks)
                    yield return $"  - {task.TaskName}: {task.Description} Success: {task.SuccessCriteria}";
            }
        }
        yield return "";
        yield return "BACKEND RECOMMENDATIONS";
        foreach (var recommendation in version.Feasibility.Recommendations)
            yield return $"- {recommendation}";

        if (replay != null)
        {
            yield return "";
            yield return "REPLAY COMPARISON";
            yield return $"Versions: {replay.PreviousVersion} to {replay.NewVersion}";
            yield return $"Score delta: {replay.ScoreDelta:+0.0;-0.0;0.0}";
            yield return $"Risk delta: {replay.RiskDelta}";
            yield return $"Main reason: {replay.MainReason}";
            foreach (var change in replay.ChangedFields)
                yield return $"{change.Field}: {change.From ?? "(missing)"} -> {change.To ?? "(missing)"}";
        }
    }

    private static string BuildPageContent(
        IReadOnlyList<string> lines,
        int page,
        int pageCount)
    {
        var builder = new StringBuilder();
        builder.Append("BT\n/F1 10 Tf\n50 750 Td\n14 TL\n");
        foreach (var line in lines)
            builder.Append('(').Append(EscapePdf(line)).Append(") Tj\nT*\n");
        builder.Append($"T*\n(Page {page} of {pageCount}) Tj\nET");
        return builder.ToString();
    }

    private static byte[] BuildPdf(IReadOnlyDictionary<int, byte[]> objects)
    {
        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");
        var offsets = new Dictionary<int, long>();
        foreach (var pair in objects.OrderBy(pair => pair.Key))
        {
            offsets[pair.Key] = stream.Position;
            Write(stream, $"{pair.Key} 0 obj\n");
            stream.Write(pair.Value);
            Write(stream, "\nendobj\n");
        }

        var xref = stream.Position;
        var maxId = objects.Keys.Max();
        Write(stream, $"xref\n0 {maxId + 1}\n");
        Write(stream, "0000000000 65535 f \n");
        for (var id = 1; id <= maxId; id++)
            Write(stream, $"{offsets[id].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
        Write(stream, $"trailer\n<< /Size {maxId + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return stream.ToArray();
    }

    private static IEnumerable<string> Wrap(string value, int width)
    {
        if (string.IsNullOrEmpty(value))
        {
            yield return string.Empty;
            yield break;
        }

        var remaining = value.ReplaceLineEndings(" ");
        while (remaining.Length > width)
        {
            var split = remaining.LastIndexOf(' ', width);
            if (split <= 0) split = width;
            yield return remaining[..split].Trim();
            remaining = remaining[split..].Trim();
        }
        yield return remaining;
    }

    private static string EscapePdf(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
            .Select(character => character <= 127 ? character : '?')
            .Aggregate(new StringBuilder(), (builder, character) => builder.Append(character))
            .ToString();

    private static byte[] Bytes(string value) => Encoding.ASCII.GetBytes(value);
    private static byte[] Combine(params byte[][] parts) => parts.SelectMany(part => part).ToArray();
    private static void Write(Stream stream, string value) => stream.Write(Bytes(value));
}
