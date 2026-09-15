using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace BpmSoftSync.Adapters.Excel;

internal static class WorkbookPackageWriter
{
    private static readonly DateTimeOffset FixedZipTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Content = "http://schemas.openxmlformats.org/package/2006/content-types";

    public static void Write(string path, WorkbookProjection workbook)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false, Encoding.UTF8);
        Add(archive, "[Content_Types].xml", ContentTypes(workbook));
        Add(archive, "_rels/.rels", RootRelationships());
        Add(archive, "xl/workbook.xml", Workbook(workbook));
        Add(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(workbook));
        Add(archive, "xl/styles.xml", Styles());
        for (var index = 0; index < workbook.Sheets.Count; index++) Add(archive, $"xl/worksheets/sheet{index + 1}.xml", Worksheet(workbook.Sheets[index]));
    }

    private static XDocument ContentTypes(WorkbookProjection workbook) => new(new XElement(Content + "Types",
        new XElement(Content + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
        new XElement(Content + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
        new XElement(Content + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
        new XElement(Content + "Override", new XAttribute("PartName", "/xl/styles.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")),
        workbook.Sheets.Select((_, index) => new XElement(Content + "Override", new XAttribute("PartName", $"/xl/worksheets/sheet{index + 1}.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")))));

    private static XDocument RootRelationships() => new(new XElement(PackageRel + "Relationships",
        new XElement(PackageRel + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "xl/workbook.xml"))));

    private static XDocument Workbook(WorkbookProjection workbook) => new(new XElement(Main + "workbook", new XAttribute(XNamespace.Xmlns + "r", Rel),
        new XElement(Main + "bookViews", new XElement(Main + "workbookView", new XAttribute("activeTab", "0"))),
        new XElement(Main + "sheets", workbook.Sheets.Select((sheet, index) => new XElement(Main + "sheet", new XAttribute("name", sheet.Name), new XAttribute("sheetId", index + 1), new XAttribute(Rel + "id", $"rId{index + 1}"), sheet.Hidden ? new XAttribute("state", "hidden") : null))),
        new XElement(Main + "definedNames", WorkbookContract.ValidationHeaders.Select((header, index) => new XElement(Main + "definedName", new XAttribute("name", header), $"'ValidationLists'!${ColumnName(index + 1)}$2:${ColumnName(index + 1)}${WorkbookContract.ValidationValueCount(header) + 1}"))),
        new XElement(Main + "calcPr", new XAttribute("calcId", "0"), new XAttribute("calcMode", "manual"))));

    private static XDocument WorkbookRelationships(WorkbookProjection workbook) => new(new XElement(PackageRel + "Relationships",
        workbook.Sheets.Select((_, index) => new XElement(PackageRel + "Relationship", new XAttribute("Id", $"rId{index + 1}"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", $"worksheets/sheet{index + 1}.xml"))),
        new XElement(PackageRel + "Relationship", new XAttribute("Id", $"rId{workbook.Sheets.Count + 1}"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"), new XAttribute("Target", "styles.xml"))));

    private static XDocument Styles()
    {
        var root = new XElement(Main + "styleSheet",
            new XElement(Main + "fonts", new XAttribute("count", "2"),
                new XElement(Main + "font", new XElement(Main + "sz", new XAttribute("val", "11")), new XElement(Main + "name", new XAttribute("val", "Calibri")), new XElement(Main + "family", new XAttribute("val", "2"))),
                new XElement(Main + "font", new XElement(Main + "b"), new XElement(Main + "color", new XAttribute("rgb", "00000000")), new XElement(Main + "sz", new XAttribute("val", "11")), new XElement(Main + "name", new XAttribute("val", "Calibri")))),
            new XElement(Main + "fills", new XAttribute("count", "3"),
                new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "none"))),
                new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "gray125"))),
                new XElement(Main + "fill", new XElement(Main + "patternFill", new XAttribute("patternType", "solid"), new XElement(Main + "fgColor", new XAttribute("rgb", "00D9EAF7")), new XElement(Main + "bgColor", new XAttribute("indexed", "64"))))),
            new XElement(Main + "borders", new XAttribute("count", "1"), new XElement(Main + "border", new XElement(Main + "left"), new XElement(Main + "right"), new XElement(Main + "top"), new XElement(Main + "bottom"), new XElement(Main + "diagonal"))),
            new XElement(Main + "cellStyleXfs", new XAttribute("count", "1"), new XElement(Main + "xf", new XAttribute("numFmtId", "0"), new XAttribute("fontId", "0"), new XAttribute("fillId", "0"), new XAttribute("borderId", "0"))),
            new XElement(Main + "cellXfs", new XAttribute("count", "4"), Xf(0, 0, true), Xf(1, 2, true, "center"), Xf(0, 0, true), Xf(0, 0, false)),
            new XElement(Main + "cellStyles", new XAttribute("count", "1"), new XElement(Main + "cellStyle", new XAttribute("name", "Normal"), new XAttribute("xfId", "0"), new XAttribute("builtinId", "0"))));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static XElement Xf(int fontId, int fillId, bool locked, string? alignment = null) => new(Main + "xf",
        new XAttribute("numFmtId", "0"), new XAttribute("fontId", fontId), new XAttribute("fillId", fillId), new XAttribute("borderId", "0"), new XAttribute("xfId", "0"),
        new XAttribute("applyFont", "1"), new XAttribute("applyFill", "1"), new XAttribute("applyProtection", "1"),
        alignment is null ? null : new XAttribute("applyAlignment", "1"), alignment is null ? null : new XElement(Main + "alignment", new XAttribute("horizontal", alignment), new XAttribute("vertical", "center"), new XAttribute("wrapText", "1")),
        new XElement(Main + "protection", new XAttribute("locked", locked ? "1" : "0")));

    private static XDocument Worksheet(WorksheetProjection sheet)
    {
        var lastColumn = ColumnName(sheet.Headers.Length);
        var lastRow = sheet.Rows.Count + 1;
        var data = new XElement(Main + "sheetData", RowElement(1, sheet.Headers.Cast<string?>().ToArray(), _ => 1));
        for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
        {
            var row = sheet.Rows[rowIndex];
            var editable = WorkbookContract.EditableColumns(sheet.Name, row);
            data.Add(RowElement(rowIndex + 2, row, column => editable.Contains(column) ? 3 : 2));
        }
        var root = new XElement(Main + "worksheet",
            new XElement(Main + "sheetViews", new XElement(Main + "sheetView", new XAttribute("workbookViewId", "0"), new XElement(Main + "pane", new XAttribute("ySplit", "1"), new XAttribute("topLeftCell", "A2"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(Main + "sheetFormatPr", new XAttribute("defaultRowHeight", "15")),
            new XElement(Main + "cols", Enumerable.Range(1, sheet.Headers.Length).Select(index => new XElement(Main + "col", new XAttribute("min", index), new XAttribute("max", index), new XAttribute("width", Math.Clamp(sheet.Headers[index - 1].Length + 4, 12, 34)), new XAttribute("customWidth", "1")))),
            data,
            sheet.ReadOnly ? new XElement(Main + "sheetProtection", new XAttribute("sheet", "1"), new XAttribute("objects", "1"), new XAttribute("scenarios", "1"), new XAttribute("formatColumns", "0"), new XAttribute("autoFilter", "0"), new XAttribute("sort", "0")) : null,
            new XElement(Main + "autoFilter", new XAttribute("ref", $"A1:{lastColumn}{Math.Max(1, lastRow)}")),
            DataValidations(sheet, lastRow),
            new XElement(Main + "pageMargins", new XAttribute("left", "0.7"), new XAttribute("right", "0.7"), new XAttribute("top", "0.75"), new XAttribute("bottom", "0.75"), new XAttribute("header", "0.3"), new XAttribute("footer", "0.3")));
        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), root);
    }

    private static XElement? DataValidations(WorksheetProjection sheet, int lastRow)
    {
        if (sheet.Validations.Count == 0) return null;
        var validations = new List<XElement>();
        foreach (var item in sheet.Validations.OrderBy(item => Array.IndexOf(sheet.Headers, item.Key)))
        {
            var column = Array.IndexOf(sheet.Headers, item.Key) + 1;
            var validationColumn = Array.IndexOf(WorkbookContract.ValidationHeaders, item.Value) + 1;
            if (column < 1 || validationColumn < 1) throw new InvalidDataException("WORKBOOK_VALIDATION_CONTRACT_INVALID");
            validations.Add(new XElement(Main + "dataValidation", new XAttribute("type", "list"), new XAttribute("allowBlank", "1"), new XAttribute("showErrorMessage", "1"), new XAttribute("sqref", $"{ColumnName(column)}2:{ColumnName(column)}{Math.Max(lastRow, 500)}"),
                new XElement(Main + "formula1", item.Value)));
        }
        return new XElement(Main + "dataValidations", new XAttribute("count", validations.Count), validations);
    }

    private static XElement RowElement(int rowNumber, IReadOnlyList<string?> values, Func<int, int> style) => new(Main + "row", new XAttribute("r", rowNumber), values.Select((value, index) => Cell(rowNumber, index + 1, value, style(index))));

    private static XElement Cell(int row, int column, string? value, int style)
    {
        var cell = new XElement(Main + "c", new XAttribute("r", ColumnName(column) + row), new XAttribute("s", style), new XAttribute("t", "inlineStr"));
        var text = new XElement(Main + "t", value ?? string.Empty);
        if (value is { Length: > 0 } && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))) text.Add(new XAttribute(XNamespace.Xml + "space", "preserve"));
        cell.Add(new XElement(Main + "is", text));
        return cell;
    }

    internal static string ColumnName(int number)
    {
        var result = string.Empty;
        while (number > 0) { number--; result = (char)('A' + number % 26) + result; number /= 26; }
        return result;
    }

    private static void Add(ZipArchive archive, string name, XDocument document)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        entry.LastWriteTime = FixedZipTime;
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        document.Save(writer, SaveOptions.DisableFormatting);
    }
}
