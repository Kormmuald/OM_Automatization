namespace BpmSoftSync.Domain;

public enum WorkbookScaleForecastStatus { DiagnosticOnly, DecisionRequired }

public sealed record WorkbookScaleForecast(string Schema, WorkbookScaleForecastStatus Status, string RowBucket, string Reason)
{
    public const string SchemaVersion = "WorkbookScaleForecast/v1";
    private const int ManifestFixedDataRows = 17;
    private const int ReadmeDataRows = 6;
    private const int ValidationListDataRows = 10;

    public static WorkbookScaleForecast Create(int observedRows, int? declaredLimit)
    {
        if (observedRows < 0 || declaredLimit is null || declaredLimit < 1 || observedRows > declaredLimit)
            return new(SchemaVersion, WorkbookScaleForecastStatus.DecisionRequired, Bucket(Math.Max(0, observedRows)), "WORKBOOK_SCALE_DECISION_REQUIRED");
        return new(SchemaVersion, WorkbookScaleForecastStatus.DiagnosticOnly, Bucket(observedRows), "DIAGNOSTIC_ONLY");
    }

    public static WorkbookScaleForecast Create(IReadOnlyDictionary<string, int> projectedRowCounts, int? declaredLimit)
    {
        ArgumentNullException.ThrowIfNull(projectedRowCounts);
        if (projectedRowCounts.Count == 0 || projectedRowCounts.Any(item => item.Value < 0))
            return new(SchemaVersion, WorkbookScaleForecastStatus.DecisionRequired, "0-100", "WORKBOOK_SCALE_DECISION_REQUIRED");
        return Create(projectedRowCounts.Values.Max(), declaredLimit);
    }

    public static WorkbookScaleForecast CreateForCatalogPair(IReadOnlyDictionary<string, int> catalogCounts, int componentDigestCount, int? declaredLimit) =>
        Create(ProjectCatalogPairWorksheets(catalogCounts, componentDigestCount), declaredLimit);

    public static IReadOnlyDictionary<string, int> ProjectCatalogPairWorksheets(IReadOnlyDictionary<string, int> catalogCounts, int componentDigestCount)
    {
        ArgumentNullException.ThrowIfNull(catalogCounts);
        if (componentDigestCount < 0 || catalogCounts.Any(item => item.Value < 0)) return new Dictionary<string, int> { ["invalid"] = -1 };
        int Count(string key) => catalogCounts.TryGetValue(key, out var value) ? value : 0;
        int WithHeader(long dataRows) => dataRows >= int.MaxValue ? int.MaxValue : checked((int)dataRows + 1);
        var manifestRows = WithHeader((long)ManifestFixedDataRows + catalogCounts.Count + componentDigestCount);
        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Model.Readme"] = WithHeader(ReadmeDataRows),
            ["Model.Manifest"] = manifestRows,
            ["Model.WorkspaceInventory"] = WithHeader(Count("workspaceItems")),
            ["Model.Schemas"] = WithHeader(Count("schemas")),
            ["Model.Columns"] = WithHeader(Count("columns")),
            ["Model.Indexes"] = WithHeader(Count("indexMembers")),
            ["Model.ValidationLists"] = WithHeader(ValidationListDataRows),
            ["Model.PullConflicts"] = WithHeader(0),
            ["Lookup.Readme"] = WithHeader(ReadmeDataRows),
            ["Lookup.Manifest"] = manifestRows,
            ["Lookup.LookupRegistry"] = WithHeader(Count("lookupRegistry")),
            ["Lookup.LookupValues"] = WithHeader(Count("lookupValues")),
            ["Lookup.ValidationLists"] = WithHeader(ValidationListDataRows),
            ["Lookup.PullConflicts"] = WithHeader(0)
        };
    }

    private static string Bucket(int rows) => rows switch { <= 100 => "0-100", <= 1_000 => "101-1000", <= 10_000 => "1001-10000", _ => "10001+" };
}
