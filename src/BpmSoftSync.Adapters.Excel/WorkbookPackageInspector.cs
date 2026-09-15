using System.IO.Compression;
using System.Xml.Linq;

namespace BpmSoftSync.Adapters.Excel;

public sealed record WorkbookPackageValidation(bool IsValid, int FormulaCellCount, int ExternalRelationshipCount, int ForbiddenPartCount, IReadOnlyList<string> Errors);

public static class WorkbookPackageInspector
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace OfficeRel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static WorkbookPackageValidation Validate(string path)
    {
        var errors = new List<string>();
        var formulaCells = 0;
        var external = 0;
        var forbidden = 0;
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var names = archive.Entries.Select(entry => entry.FullName).ToArray();
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length) errors.Add("DUPLICATE_PART");
            string[] required = ["[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "xl/styles.xml"];
            foreach (var item in required) if (!names.Contains(item, StringComparer.Ordinal)) errors.Add("MISSING_PART:" + item);
            forbidden = names.Count(IsForbiddenPart);
            if (forbidden != 0) errors.Add("FORBIDDEN_PART");
            foreach (var entry in archive.Entries.Where(entry => entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = entry.Open();
                var doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                if (entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)) formulaCells += doc.Descendants(Main + "c").Count(cell => cell.Element(Main + "f") is not null);
                if (entry.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var relationship in doc.Descendants(PackageRel + "Relationship"))
                    {
                        if (string.Equals((string?)relationship.Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase)) { external++; continue; }
                        var target = (string?)relationship.Attribute("Target");
                        if (string.IsNullOrWhiteSpace(target)) { errors.Add("EMPTY_RELATIONSHIP_TARGET"); continue; }
                        var resolved = Resolve(entry.FullName, target);
                        if (!names.Contains(resolved, StringComparer.Ordinal)) errors.Add("BROKEN_RELATIONSHIP:" + resolved);
                    }
                }
            }
            if (formulaCells != 0) errors.Add("FORMULA_CELL_FORBIDDEN");
            if (external != 0) errors.Add("EXTERNAL_RELATIONSHIP_FORBIDDEN");
            ValidateWorkbookClosure(archive, names, errors);
        }
        catch (Exception error) when (error is InvalidDataException or IOException or System.Xml.XmlException)
        {
            errors.Add("PACKAGE_READ_FAILED:" + error.GetType().Name);
        }
        return new WorkbookPackageValidation(errors.Count == 0, formulaCells, external, forbidden, errors);
    }

    public static IReadOnlyList<string> ValidateProjectionStyles(string path, WorkbookProjection expected)
    {
        var errors = new List<string>();
        try
        {
            using var archive = ZipFile.OpenRead(path);
            for (var sheetIndex = 0; sheetIndex < expected.Sheets.Count; sheetIndex++)
            {
                var projection = expected.Sheets[sheetIndex];
                var document = Load(archive, $"xl/worksheets/sheet{sheetIndex + 1}.xml");
                var rows = document.Descendants(Main + "sheetData").Elements(Main + "row").ToArray();
                if (rows.Length != projection.Rows.Count + 1) { errors.Add("STYLE_ROW_COUNT_INVALID:" + projection.Name); continue; }
                CheckRowStyle(rows[0], _ => 1, projection.Name, 1, errors);
                for (var rowIndex = 0; rowIndex < projection.Rows.Count; rowIndex++)
                {
                    var editable = WorkbookContract.EditableColumns(projection.Name, projection.Rows[rowIndex]);
                    CheckRowStyle(rows[rowIndex + 1], column => editable.Contains(column) ? 3 : 2, projection.Name, rowIndex + 2, errors);
                }
            }
        }
        catch (Exception error) when (error is InvalidDataException or IOException or System.Xml.XmlException) { errors.Add("STYLE_READ_FAILED:" + error.GetType().Name); }
        return errors;
    }

    private static void ValidateWorkbookClosure(ZipArchive archive, IReadOnlyList<string> names, List<string> errors)
    {
        var workbook = Load(archive, "xl/workbook.xml");
        var sheets = workbook.Descendants(Main + "sheet").ToArray();
        var expectedParts = new HashSet<string>(StringComparer.Ordinal) { "[Content_Types].xml", "_rels/.rels", "xl/workbook.xml", "xl/_rels/workbook.xml.rels", "xl/styles.xml" };
        for (var index = 1; index <= sheets.Length; index++) expectedParts.Add($"xl/worksheets/sheet{index}.xml");
        foreach (var name in names) if (!expectedParts.Contains(name)) errors.Add("UNDECLARED_OR_FORBIDDEN_PART:" + name);
        foreach (var expected in expectedParts) if (!names.Contains(expected, StringComparer.Ordinal)) errors.Add("MISSING_EXPECTED_PART:" + expected);
        ValidateRelationships(archive, sheets, errors);
        ValidateDefinedNames(workbook, errors);
        ValidateSheetContracts(archive, sheets, errors);
        XNamespace content = "http://schemas.openxmlformats.org/package/2006/content-types";
        var types = Load(archive, "[Content_Types].xml");
        var overrides = types.Descendants(content + "Override").Select(item => ((string?)item.Attribute("PartName"), (string?)item.Attribute("ContentType"))).ToArray();
        var expectedOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["/xl/workbook.xml"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml",
            ["/xl/styles.xml"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"
        };
        for (var index = 1; index <= sheets.Length; index++) expectedOverrides[$"/xl/worksheets/sheet{index}.xml"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml";
        if (overrides.Length != expectedOverrides.Count || overrides.Any(item => item.Item1 is null || item.Item2 is null || !expectedOverrides.TryGetValue(item.Item1, out var value) || value != item.Item2) || overrides.Select(item => item.Item1).Distinct(StringComparer.Ordinal).Count() != overrides.Length) errors.Add("CONTENT_TYPE_CLOSURE_INVALID");
        var defaults = types.Descendants(content + "Default").Select(item => ((string?)item.Attribute("Extension"), (string?)item.Attribute("ContentType"))).ToArray();
        if (defaults.Length != 2 || !defaults.Contains(("xml", "application/xml")) || !defaults.Contains(("rels", "application/vnd.openxmlformats-package.relationships+xml"))) errors.Add("CONTENT_TYPE_DEFAULTS_INVALID");
        var styles = Load(archive, "xl/styles.xml");
        var cellXfs = styles.Descendants(Main + "cellXfs").SingleOrDefault();
        var styleCount = cellXfs?.Elements(Main + "xf").Count() ?? 0;
        if (styleCount != 4 || (int?)cellXfs?.Attribute("count") != styleCount || cellXfs?.Attributes().Count() != 1) errors.Add("STYLE_COUNT_INVALID");
        else ValidateCellXfs(cellXfs!.Elements(Main + "xf").ToArray(), errors);
        foreach (var sheet in Enumerable.Range(1, sheets.Length).Select(index => Load(archive, $"xl/worksheets/sheet{index}.xml")))
            if (sheet.Descendants(Main + "c").Any(cell => !int.TryParse((string?)cell.Attribute("s"), out var style) || style < 0 || style >= styleCount)) errors.Add("STYLE_REFERENCE_INVALID");
    }

    private static void ValidateRelationships(ZipArchive archive, IReadOnlyList<XElement> sheets, List<string> errors)
    {
        var root = Load(archive, "_rels/.rels").Descendants(PackageRel + "Relationship").ToArray();
        if (root.Length != 1 || (string?)root[0].Attribute("Id") != "rId1" || (string?)root[0].Attribute("Type") != "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" || (string?)root[0].Attribute("Target") != "xl/workbook.xml" || root[0].Attribute("TargetMode") is not null)
            errors.Add("ROOT_RELATIONSHIP_CONTRACT_INVALID");
        var workbookRels = Load(archive, "xl/_rels/workbook.xml.rels").Descendants(PackageRel + "Relationship").ToArray();
        if (workbookRels.Length != sheets.Count + 1 || workbookRels.Select(item => (string?)item.Attribute("Id")).Distinct(StringComparer.Ordinal).Count() != workbookRels.Length) errors.Add("WORKBOOK_RELATIONSHIP_COUNT_INVALID");
        for (var index = 0; index < sheets.Count; index++)
        {
            var expectedId = $"rId{index + 1}";
            var relation = workbookRels.SingleOrDefault(item => (string?)item.Attribute("Id") == expectedId);
            if (relation is null || (string?)relation.Attribute("Type") != "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" || (string?)relation.Attribute("Target") != $"worksheets/sheet{index + 1}.xml" || relation.Attribute("TargetMode") is not null || (string?)sheets[index].Attribute(OfficeRel + "id") != expectedId || (int?)sheets[index].Attribute("sheetId") != index + 1)
                errors.Add("WORKBOOK_SHEET_RELATIONSHIP_INVALID:" + expectedId);
        }
        var styleId = $"rId{sheets.Count + 1}";
        var style = workbookRels.SingleOrDefault(item => (string?)item.Attribute("Id") == styleId);
        if (style is null || (string?)style.Attribute("Type") != "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" || (string?)style.Attribute("Target") != "styles.xml" || style.Attribute("TargetMode") is not null) errors.Add("WORKBOOK_STYLE_RELATIONSHIP_INVALID");
    }

    private static void ValidateDefinedNames(XDocument workbook, List<string> errors)
    {
        var names = workbook.Descendants(Main + "definedName").ToArray();
        if (names.Length != WorkbookContract.ValidationHeaders.Length || names.Select(item => (string?)item.Attribute("name")).Distinct(StringComparer.Ordinal).Count() != names.Length)
        {
            errors.Add("DEFINED_NAMES_COUNT_INVALID");
            return;
        }
        for (var index = 0; index < WorkbookContract.ValidationHeaders.Length; index++)
        {
            var name = WorkbookContract.ValidationHeaders[index];
            var item = names.SingleOrDefault(candidate => (string?)candidate.Attribute("name") == name);
            var column = WorkbookPackageWriter.ColumnName(index + 1);
            var expected = $"'ValidationLists'!${column}$2:${column}${WorkbookContract.ValidationValueCount(name) + 1}";
            if (item is null || item.HasAttributes && item.Attributes().Any(attribute => attribute.Name.LocalName != "name") || item?.Value != expected) errors.Add("DEFINED_NAME_INVALID:" + name);
        }
    }

    private static void ValidateSheetContracts(ZipArchive archive, IReadOnlyList<XElement> sheets, List<string> errors)
    {
        var sheetNames = sheets.Select(item => (string?)item.Attribute("name") ?? string.Empty).ToArray();
        if (sheetNames.Distinct(StringComparer.Ordinal).Count() != sheetNames.Length || sheetNames.Count(name => name == "ValidationLists") != 1) errors.Add("SHEET_NAME_CONTRACT_INVALID");
        for (var index = 0; index < sheetNames.Length; index++)
        {
            var name = sheetNames[index];
            var document = Load(archive, $"xl/worksheets/sheet{index + 1}.xml");
            var protections = document.Descendants(Main + "sheetProtection").ToArray();
            if (WorkbookContract.IsReadOnly(name))
            {
                string[] expectedNames = ["sheet", "objects", "scenarios", "formatColumns", "autoFilter", "sort"];
                if (protections.Length != 1 || protections[0].Attributes().Select(item => item.Name.LocalName).OrderBy(value => value).SequenceEqual(expectedNames.OrderBy(value => value), StringComparer.Ordinal) is false ||
                    (string?)protections[0].Attribute("sheet") != "1" || (string?)protections[0].Attribute("objects") != "1" || (string?)protections[0].Attribute("scenarios") != "1" || (string?)protections[0].Attribute("formatColumns") != "0" || (string?)protections[0].Attribute("autoFilter") != "0" || (string?)protections[0].Attribute("sort") != "0") errors.Add("SHEET_PROTECTION_INVALID:" + name);
            }
            else if (protections.Length != 0) errors.Add("MIXED_SHEET_PROTECTION_FORBIDDEN:" + name);

            var headers = WorkbookContract.ModelHeaders.TryGetValue(name, out var modelHeaders) ? modelHeaders : WorkbookContract.LookupHeaders[name];
            var lastRow = document.Descendants(Main + "sheetData").Elements(Main + "row").Select(row => (int?)row.Attribute("r") ?? 0).DefaultIfEmpty(1).Max();
            var filters = document.Descendants(Main + "autoFilter").ToArray();
            var expectedFilter = $"A1:{WorkbookPackageWriter.ColumnName(headers.Length)}{Math.Max(1, lastRow)}";
            if (filters.Length != 1 || filters[0].Attributes().Count() != 1 || (string?)filters[0].Attribute("ref") != expectedFilter || filters[0].HasElements || !string.IsNullOrEmpty(filters[0].Value))
                errors.Add("AUTO_FILTER_INVALID:" + name);

            var bindings = WorkbookContract.ValidationBindings(name);
            var container = document.Descendants(Main + "dataValidations").ToArray();
            var validations = document.Descendants(Main + "dataValidation").ToArray();
            if (bindings.Count == 0)
            {
                if (container.Length != 0 || validations.Length != 0) errors.Add("DATA_VALIDATION_UNEXPECTED:" + name);
                continue;
            }
            if (container.Length != 1 || (int?)container[0].Attribute("count") != bindings.Count || validations.Length != bindings.Count) { errors.Add("DATA_VALIDATION_COUNT_INVALID:" + name); continue; }
            foreach (var binding in bindings)
            {
                var column = WorkbookPackageWriter.ColumnName(Array.IndexOf(headers, binding.Key) + 1);
                var expectedSqref = $"{column}2:{column}{Math.Max(lastRow, 500)}";
                var matches = validations.Where(item => (string?)item.Attribute("sqref") == expectedSqref && item.Element(Main + "formula1")?.Value == binding.Value).ToArray();
                if (matches.Length != 1 || matches[0].Attributes().Select(item => item.Name.LocalName).OrderBy(value => value).SequenceEqual(new[] { "allowBlank", "showErrorMessage", "sqref", "type" }.OrderBy(value => value), StringComparer.Ordinal) is false || (string?)matches[0].Attribute("type") != "list" || (string?)matches[0].Attribute("allowBlank") != "1" || (string?)matches[0].Attribute("showErrorMessage") != "1" || matches[0].Element(Main + "formula2") is not null)
                    errors.Add("DATA_VALIDATION_INVALID:" + name + ":" + binding.Key);
            }
        }
    }

    private static void ValidateCellXfs(IReadOnlyList<XElement> xfs, List<string> errors)
    {
        var expected = new[] { (Font: "0", Fill: "0", Locked: "1", Aligned: false), (Font: "1", Fill: "2", Locked: "1", Aligned: true), (Font: "0", Fill: "0", Locked: "1", Aligned: false), (Font: "0", Fill: "0", Locked: "0", Aligned: false) };
        string[] baseAttributes = ["numFmtId", "fontId", "fillId", "borderId", "xfId", "applyFont", "applyFill", "applyProtection"];
        for (var index = 0; index < xfs.Count; index++)
        {
            var xf = xfs[index];
            var item = expected[index];
            var expectedAttributes = item.Aligned ? baseAttributes.Append("applyAlignment").OrderBy(value => value, StringComparer.Ordinal) : baseAttributes.OrderBy(value => value, StringComparer.Ordinal);
            var protection = xf.Elements(Main + "protection").ToArray();
            var alignment = xf.Elements(Main + "alignment").ToArray();
            var expectedChildren = item.Aligned ? new[] { "alignment", "protection" } : new[] { "protection" };
            var valid = xf.Attributes().Select(attribute => attribute.Name.LocalName).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(expectedAttributes, StringComparer.Ordinal) &&
                        xf.Elements().Select(element => element.Name.LocalName).SequenceEqual(expectedChildren, StringComparer.Ordinal) &&
                        (string?)xf.Attribute("numFmtId") == "0" && (string?)xf.Attribute("fontId") == item.Font && (string?)xf.Attribute("fillId") == item.Fill && (string?)xf.Attribute("borderId") == "0" && (string?)xf.Attribute("xfId") == "0" &&
                        (string?)xf.Attribute("applyFont") == "1" && (string?)xf.Attribute("applyFill") == "1" && (string?)xf.Attribute("applyProtection") == "1" && protection.Length == 1 && protection[0].Attributes().Count() == 1 && (string?)protection[0].Attribute("locked") == item.Locked && !protection[0].HasElements;
            if (item.Aligned)
                valid = valid && (string?)xf.Attribute("applyAlignment") == "1" && alignment.Length == 1 && alignment[0].Attributes().Count() == 3 && (string?)alignment[0].Attribute("horizontal") == "center" && (string?)alignment[0].Attribute("vertical") == "center" && (string?)alignment[0].Attribute("wrapText") == "1" && !alignment[0].HasElements;
            else valid = valid && alignment.Length == 0;
            if (!valid) errors.Add("CELL_XF_PROTECTION_INVALID:" + index);
        }
    }

    private static string Resolve(string relationshipPart, string target)
    {
        var basePart = relationshipPart == "_rels/.rels" ? string.Empty : relationshipPart.Replace("/_rels/", "/", StringComparison.Ordinal)[..^5];
        var baseDirectory = basePart.Length == 0 ? string.Empty : Path.GetDirectoryName(basePart)?.Replace('\\', '/') ?? string.Empty;
        var combined = string.IsNullOrEmpty(baseDirectory) ? target : baseDirectory + "/" + target;
        var stack = new Stack<string>();
        foreach (var part in combined.Split('/', StringSplitOptions.RemoveEmptyEntries)) { if (part == "..") { if (stack.Count != 0) stack.Pop(); } else if (part != ".") stack.Push(part); }
        return string.Join('/', stack.Reverse());
    }
    private static void CheckRowStyle(XElement row, Func<int, int> expectedStyle, string sheetName, int rowNumber, List<string> errors)
    {
        var cells = row.Elements(Main + "c").ToArray();
        for (var column = 0; column < cells.Length; column++)
            if (!int.TryParse((string?)cells[column].Attribute("s"), out var actual) || actual != expectedStyle(column)) errors.Add($"CELL_STYLE_INVALID:{sheetName}!{WorkbookPackageWriter.ColumnName(column + 1)}{rowNumber}");
    }
    private static bool IsForbiddenPart(string name) => name.Contains("externalLinks", StringComparison.OrdinalIgnoreCase) || name.Contains("connections", StringComparison.OrdinalIgnoreCase) || name.Contains("vbaProject", StringComparison.OrdinalIgnoreCase) || name.Contains("embeddings", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase);
    private static XDocument Load(ZipArchive archive, string name) { using var stream = archive.GetEntry(name)?.Open() ?? throw new InvalidDataException("MISSING_PART:" + name); return XDocument.Load(stream); }
}
