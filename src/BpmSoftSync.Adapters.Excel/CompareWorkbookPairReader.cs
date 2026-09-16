using System.Security.Cryptography;
using System.Text;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.Excel;

/// <summary>Reads only closed, locally generated workbook pairs. It never evaluates formulas or follows relationships.</summary>
public static class CompareWorkbookPairReader
{
    public static CompareWorkbookPair Read(string modelPath, string lookupPath, string expectedTargetAlias)
    {
        RequireSafePackage(modelPath, allowLegacyLookupLayout: false); RequireSafePackage(lookupPath, allowLegacyLookupLayout: true);
        var pair = WorkbookPairReader.Read(modelPath, lookupPath);
        ValidateWorkbook(pair.Model, WorkbookContract.ModelSheetOrder, WorkbookContract.ModelHeaders, "Model");
        ValidateWorkbook(pair.Lookup, WorkbookContract.LookupSheetOrder, WorkbookContract.LookupHeaders, "Lookup");
        var manifest = Manifest(pair.Model);
        if (!manifest.TryGetValue("TargetAlias", out var target) || !string.Equals(target, expectedTargetAlias, StringComparison.Ordinal) ||
            !string.Equals(manifest.GetValueOrDefault("ScopeMode"), "AllReadableCatalog", StringComparison.Ordinal)) throw new InvalidDataException("COMPARE_PAIR_BINDING_INVALID");
        return new CompareWorkbookPair(target, HashFile(modelPath), HashFile(lookupPath), BindingDigest(manifest), Rows(pair.Model.Sheet("Columns")), Rows(pair.Lookup.Sheet("LookupValues")));
    }

    private static void RequireSafePackage(string path, bool allowLegacyLookupLayout)
    {
        if (!Path.IsPathFullyQualified(path) || !File.Exists(path)) throw new InvalidDataException("COMPARE_WORKBOOK_PATH_INVALID");
        var inspection = WorkbookPackageInspector.Validate(path);
        // Compare accepts an Excel-saved user workbook, not only the byte-exact
        // writer projection. Canonical style/relationship closure errors are
        // therefore not safety findings here. Formulae, external relations and
        // forbidden parts remain terminal, as do unreadable/missing packages.
        if (inspection.FormulaCellCount != 0 || inspection.ExternalRelationshipCount != 0 || inspection.ForbiddenPartCount != 0 || inspection.Errors.Any(error => error.StartsWith("PACKAGE_READ_FAILED", StringComparison.Ordinal) || error.StartsWith("MISSING_PART", StringComparison.Ordinal)))
            throw new InvalidDataException("COMPARE_WORKBOOK_UNSAFE:" + inspection.Errors.FirstOrDefault());
        if (allowLegacyLookupLayout && IsLegacyLookupLayout(path)) return;
    }

    private static bool IsLegacyLookupLayout(string path)
    {
        try
        {
            var projection = WorkbookPairReader.ReadWorkbook(path, "Lookup");
            var expected = WorkbookContract.LookupHeaders["LookupValues"].Where(header => header != "ColumnUId");
            return projection.Sheets.Select(sheet => sheet.Name).SequenceEqual(WorkbookContract.LookupSheetOrder, StringComparer.Ordinal) &&
                   projection.Sheet("LookupValues").Headers.SequenceEqual(expected, StringComparer.Ordinal);
        }
        catch (InvalidDataException) { return false; }
    }

    private static void ValidateWorkbook(WorkbookProjection projection, IReadOnlyList<string> order, IReadOnlyDictionary<string, string[]> headers, string kind)
    {
        if (!projection.Sheets.Select(sheet => sheet.Name).SequenceEqual(order, StringComparer.Ordinal)) throw new InvalidDataException("COMPARE_WORKBOOK_SHEET_SET_INVALID:" + kind);
        foreach (var sheet in projection.Sheets)
        {
            if (!headers.TryGetValue(sheet.Name, out var expected) || !KnownHeaders(sheet.Name, sheet.Headers, expected)) throw new InvalidDataException("COMPARE_WORKBOOK_HEADERS_INVALID:" + sheet.Name);
            var expectedBindings = WorkbookContract.ValidationBindings(sheet.Name);
            if (sheet.Validations.Count != expectedBindings.Count || expectedBindings.Any(item => !sheet.Validations.TryGetValue(item.Key, out var actual) || actual != item.Value)) throw new InvalidDataException("COMPARE_WORKBOOK_VALIDATION_INVALID:" + sheet.Name);
        }
    }

    // Pre-MVP best-effort pairs lack ColumnUId. They remain safely readable so
    // independent column intents can be compared, while each lookup value becomes
    // a strict-identity blocker in the domain; no name fallback is introduced.
    private static bool KnownHeaders(string sheetName, IReadOnlyList<string> actual, IReadOnlyList<string> current) =>
        actual.SequenceEqual(current, StringComparer.Ordinal) ||
        sheetName == "LookupValues" && actual.SequenceEqual(current.Where(header => header != "ColumnUId"), StringComparer.Ordinal);

    private static Dictionary<string, string> Manifest(WorkbookProjection projection) => projection.Sheet("Manifest").Rows.ToDictionary(row => row[0] ?? string.Empty, row => row[1] ?? string.Empty, StringComparer.Ordinal);
    private static IReadOnlyList<IReadOnlyDictionary<string, string>> Rows(WorksheetProjection sheet) => sheet.Rows.Select(row =>
    {
        var result = sheet.Headers.Select((header, index) => new KeyValuePair<string, string>(header, index < row.Length ? row[index] ?? string.Empty : string.Empty)).ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        if (sheet.Name == "LookupValues" && !result.ContainsKey("ColumnUId")) result["ColumnUId"] = string.Empty;
        return (IReadOnlyDictionary<string, string>)result;
    }).ToArray();
    private static string BindingDigest(IReadOnlyDictionary<string, string> manifest) => Hash(string.Join("\n", manifest.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => item.Key + "=" + item.Value)));
    private static string HashFile(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
