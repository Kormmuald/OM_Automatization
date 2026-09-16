using System.IO.Compression;
using System.Xml.Linq;

namespace BpmSoftSync.Adapters.Excel;

public sealed record WorkbookPairReadBack(Guid RunId, Guid PairId, WorkbookProjection Model, WorkbookProjection Lookup);

public static class WorkbookPairReader
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";

    public static WorkbookPairReadBack Read(string modelPath, string lookupPath)
    {
        var model = ReadWorkbook(modelPath, "Model");
        var lookup = ReadWorkbook(lookupPath, "Lookup");
        var modelManifest = Manifest(model);
        var lookupManifest = Manifest(lookup);
        string[] keys = ["ContractVersion", "PairId", "PullRunId", "PairBaselineHash", "BaselineTargetFingerprint", "TemplateVersion", "PassADigest", "PassBDigest", "ScopeDigest", "SourceIdentity", "ComponentDigest", "CountsDigest"];
        foreach (var key in keys) if (!modelManifest.TryGetValue(key, out var left) || !lookupManifest.TryGetValue(key, out var right) || !string.Equals(left, right, StringComparison.Ordinal)) throw new InvalidDataException("WORKBOOK_PAIR_BINDING_INVALID:" + key);
        if (modelManifest.Count != lookupManifest.Count || modelManifest.Any(item => !lookupManifest.TryGetValue(item.Key, out var value) || !string.Equals(item.Value, value, StringComparison.Ordinal))) throw new InvalidDataException("WORKBOOK_PAIR_MANIFEST_MISMATCH");
        if (!Guid.TryParse(modelManifest["PairId"], out var pairId) || pairId == Guid.Empty || !Guid.TryParse(modelManifest["PullRunId"], out var runId) || runId == Guid.Empty) throw new InvalidDataException("WORKBOOK_PAIR_IDENTITY_INVALID");
        return new WorkbookPairReadBack(runId, pairId, model, lookup);
    }

    public static WorkbookProjection ReadWorkbook(string path, string kind)
    {
        using var archive = ZipFile.OpenRead(path);
        var workbook = Load(archive, "xl/workbook.xml");
        var sharedStrings = ReadSharedStrings(archive);
        var relationships = Load(archive, "xl/_rels/workbook.xml.rels").Descendants(PackageRel + "Relationship").ToDictionary(item => (string)item.Attribute("Id")!, item => (string)item.Attribute("Target")!, StringComparer.Ordinal);
        var sheets = new List<WorksheetProjection>();
        foreach (var sheet in workbook.Descendants(Main + "sheet"))
        {
            var name = (string)sheet.Attribute("name")!;
            var relationshipId = (string)sheet.Attribute(Rel + "id")!;
            if (!relationships.TryGetValue(relationshipId, out var target) || !target.StartsWith("worksheets/", StringComparison.Ordinal)) throw new InvalidDataException("WORKBOOK_SHEET_RELATION_INVALID");
            var doc = Load(archive, "xl/" + target);
            var rows = doc.Descendants(Main + "sheetData").Elements(Main + "row").Select(row => ParseRow(row, sharedStrings)).ToArray();
            if (rows.Length == 0) throw new InvalidDataException("WORKBOOK_HEADER_MISSING:" + name);
            var validations = ParseValidations(name, rows[0], doc);
            sheets.Add(new WorksheetProjection(name, rows[0].Select(value => value ?? string.Empty).ToArray(), rows.Skip(1).ToArray(), doc.Descendants(Main + "sheetProtection").Any(), string.Equals((string?)sheet.Attribute("state"), "hidden", StringComparison.Ordinal), validations));
        }
        return new WorkbookProjection(kind, sheets);
    }

    private static IReadOnlyDictionary<string, string> ParseValidations(string sheetName, IReadOnlyList<string?> headers, XDocument doc)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var validation in doc.Descendants(Main + "dataValidation"))
        {
            if (!string.Equals((string?)validation.Attribute("type"), "list", StringComparison.Ordinal)) throw new InvalidDataException("WORKBOOK_VALIDATION_TYPE_INVALID:" + sheetName);
            var sqref = (string?)validation.Attribute("sqref") ?? throw new InvalidDataException("WORKBOOK_VALIDATION_RANGE_MISSING");
            var formula = validation.Element(Main + "formula1")?.Value ?? throw new InvalidDataException("WORKBOOK_VALIDATION_FORMULA_MISSING");
            var name = formula.StartsWith('=') ? formula[1..] : formula;
            if (!WorkbookContract.ValidationHeaders.Contains(name, StringComparer.Ordinal)) throw new InvalidDataException("WORKBOOK_VALIDATION_EXTERNAL_OR_INVALID");
            foreach (var reference in sqref.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var normalizedReference = reference.TrimStart('$');
                var columnText = new string(normalizedReference.TakeWhile(char.IsLetter).ToArray());
                var column = ColumnNumber(columnText);
                if (column < 1 || column > headers.Count) throw new InvalidDataException("WORKBOOK_VALIDATION_EXTERNAL_OR_INVALID");
                result.Add(headers[column - 1]!, name);
            }
        }
        return result;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document.Descendants(Main + "si").Select(item => string.Concat(item.Descendants(Main + "t").Select(text => text.Value))).ToArray();
    }

    private static string?[] ParseRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var cells = row.Elements(Main + "c").ToArray();
        var width = cells.Select(cell => ColumnNumber(new string(((string?)cell.Attribute("r") ?? string.Empty).TakeWhile(char.IsLetter).ToArray()))).DefaultIfEmpty(0).Max();
        var result = Enumerable.Repeat<string?>(string.Empty, width).ToArray();
        foreach (var cell in cells)
        {
            var column = ColumnNumber(new string(((string?)cell.Attribute("r") ?? string.Empty).TakeWhile(char.IsLetter).ToArray()));
            if (column < 1) throw new InvalidDataException("WORKBOOK_CELL_REFERENCE_INVALID");
            var type = (string?)cell.Attribute("t");
            var value = type switch
            {
                "inlineStr" => string.Concat(cell.Descendants(Main + "t").Select(text => text.Value)),
                "s" when int.TryParse(cell.Element(Main + "v")?.Value, out var index) && index >= 0 && index < sharedStrings.Count => sharedStrings[index],
                "s" => throw new InvalidDataException("WORKBOOK_SHARED_STRING_INVALID"),
                _ => cell.Element(Main + "v")?.Value ?? string.Empty
            };
            result[column - 1] = value;
        }
        return result;
    }
    private static Dictionary<string, string> Manifest(WorkbookProjection workbook) => workbook.Sheet("Manifest").Rows.ToDictionary(row => row[0] ?? string.Empty, row => row[1] ?? string.Empty, StringComparer.Ordinal);
    private static int ColumnNumber(string name) { var result = 0; foreach (var character in name) result = checked(result * 26 + character - 'A' + 1); return result; }
    private static XDocument Load(ZipArchive archive, string name) { using var stream = archive.GetEntry(name)?.Open() ?? throw new InvalidDataException("MISSING_PART:" + name); return XDocument.Load(stream); }
}
