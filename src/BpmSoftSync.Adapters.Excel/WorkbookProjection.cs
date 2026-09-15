using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.Excel;

public sealed record WorksheetProjection(string Name, string[] Headers, IReadOnlyList<string?[]> Rows, bool ReadOnly, bool Hidden, IReadOnlyDictionary<string, string> Validations);

public sealed record WorkbookProjection(string Kind, IReadOnlyList<WorksheetProjection> Sheets)
{
    public WorksheetProjection Sheet(string name) => Sheets.Single(sheet => string.Equals(sheet.Name, name, StringComparison.Ordinal));
    public string CanonicalDigest() => WorkbookHash.Json(new { Kind, sheets = Sheets.Select(sheet => new { sheet.Name, sheet.Headers, sheet.Rows, sheet.ReadOnly, sheet.Hidden, validations = sheet.Validations.OrderBy(item => item.Key, StringComparer.Ordinal) }) });
}

public sealed record WorkbookPairProjection(Guid RunId, Guid PairId, string PairBaselineHash, WorkbookProjection Model, WorkbookProjection Lookup)
{
    public static WorkbookPairProjection Create(QualifiedCatalogSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var modelBusiness = ModelBusiness(snapshot);
        var lookupBusiness = LookupBusiness(snapshot);
        var pairBaseline = WorkbookHash.Json(new
        {
            contractVersion = WorkbookContract.ContractVersion,
            scopeMode = "AllReadableCatalog",
            model = modelBusiness.Where(sheet => sheet.Name is not "ValidationLists" and not "PullConflicts").Select(CanonicalSheet),
            lookup = lookupBusiness.Where(sheet => sheet.Name is not "ValidationLists" and not "PullConflicts").Select(CanonicalSheet)
        });
        var manifest = Manifest(snapshot, pairBaseline);
        var model = new WorkbookProjection("Model", [Readme("Model"), manifest, .. modelBusiness]);
        var lookup = new WorkbookProjection("Lookup", [Readme("Lookup"), manifest with { Rows = manifest.Rows.Select(row => row.ToArray()).ToArray() }, .. lookupBusiness]);
        EnsureOrder(model, WorkbookContract.ModelSheetOrder);
        EnsureOrder(lookup, WorkbookContract.LookupSheetOrder);
        return new WorkbookPairProjection(snapshot.RunId, snapshot.PairId, pairBaseline, model, lookup);
    }

    private static IReadOnlyList<WorksheetProjection> ModelBusiness(QualifiedCatalogSnapshot snapshot)
    {
        var inventory = snapshot.Workspace.Inventory.Items.OrderBy(item => item.Identity.WorkspaceItemUId).ThenBy(item => item.Identity.PackageLayer.PackageUId).Select(item => Row(
            GuidText(item.Identity.WorkspaceItemUId), item.DisplayName, item.Identity.ItemType,
            item.Identity.PackageLayer.PackageName, GuidText(item.Identity.PackageLayer.PackageUId), item.SupportStatus.ToString(), item.SafeReason)).ToArray();
        var lookupSchemaIds = snapshot.Lookups.Registry.Select(item => item.SysEntitySchemaUId).ToHashSet();
        var schemas = snapshot.Workspace.Schemas.OrderBy(schema => schema.Identity.SchemaUId).ThenBy(schema => schema.Identity.PackageLayer.PackageUId).Select(schema => Row(
            schema.Identity.SchemaName, GuidText(schema.Identity.SchemaUId), GuidText(schema.Identity.ServerSchemaIdCandidate),
            lookupSchemaIds.Contains(schema.Identity.SchemaUId) ? "lookup" : "entity", schema.Identity.ParentSchemaName,
            GuidText(schema.Identity.ParentSchemaUId), null, schema.Identity.PackageLayer.PackageName,
            GuidText(schema.Identity.PackageLayer.PackageUId), "Active", "Present", SchemaFingerprint(snapshot, schema))).ToArray();
        var columns = snapshot.Workspace.Schemas.OrderBy(schema => schema.Identity.SchemaUId).SelectMany(schema => schema.Columns.OrderBy(column => column.Ownership).ThenBy(column => column.ColumnUId).Select(column =>
        {
            var actualRequired = Requirement(column.RequirementType);
            return Row(schema.Identity.SchemaName, GuidText(schema.Identity.SchemaUId), column.ColumnName, GuidText(column.ColumnUId), column.Ownership.ToString(),
                "BPMSoftTypeCode:" + column.TypeCode.ToString(CultureInfo.InvariantCulture), column.Reference?.SchemaName, GuidText(column.Reference?.SchemaUId),
                column.Ownership == ColumnOwnership.Own ? actualRequired : null, actualRequired,
                column.Ownership == ColumnOwnership.Own ? Bool(column.ActualIndexed) : null, Bool(column.ActualIndexed), "Active", "Present",
                WorkbookHash.Json(new { schema = schema.Identity, column.ColumnName, column.ColumnUId, column.Ordinal, column.Ownership, column.TypeCode, column.RequirementType, column.ActualIndexed, column.Reference, column.UnknownProperties }));
        })).ToArray();
        var indexes = snapshot.Workspace.Schemas.OrderBy(schema => schema.Identity.SchemaUId).SelectMany(schema => schema.Indexes.OrderBy(index => index.IndexUId).SelectMany(index => index.Members.OrderBy(member => member.Ordinal).Select(member =>
        {
            var column = schema.Columns.SingleOrDefault(candidate => candidate.ColumnUId == member.ColumnUId) ?? throw new InvalidDataException("INDEX_COLUMN_RELATION_UNQUALIFIED");
            return Row(schema.Identity.SchemaName, GuidText(schema.Identity.SchemaUId), GuidText(index.IndexUId), index.Name, Bool(index.IsUnique), column.ColumnName,
                GuidText(member.ColumnUId), member.Ordinal.ToString(CultureInfo.InvariantCulture), WorkbookHash.Json(new { schema = schema.Identity, index.IndexUId, index.Name, index.IsUnique, index.AutoName, members = index.Members.OrderBy(item => item.Ordinal).Select(item => new { item.ColumnUId, item.Ordinal, item.UnknownProperties }).ToArray(), index.UnknownProperties }));
        }))).ToArray();
        return
        [
            Sheet("WorkspaceInventory", inventory),
            Sheet("Schemas", schemas, WorkbookContract.ValidationBindings("Schemas")),
            Sheet("Columns", columns, WorkbookContract.ValidationBindings("Columns")),
            Sheet("Indexes", indexes),
            ValidationLists(),
            PullConflicts()
        ];
    }

    private static IReadOnlyList<WorksheetProjection> LookupBusiness(QualifiedCatalogSnapshot snapshot)
    {
        var registry = snapshot.Lookups.Registry.OrderBy(item => item.SchemaIdentity.SchemaName, StringComparer.Ordinal).ThenBy(item => item.LookupRecordId).Select(item => Row(item.SchemaIdentity.SchemaName, GuidText(item.SysEntitySchemaUId), GuidText(item.LookupRecordId),
            item.BaseSchemaIdentity?.SchemaName, GuidText(item.BaseSchemaIdentity?.SchemaUId), "Active", "Present", ExactComponentFingerprint(snapshot, $"lookup-registry:{GuidText(item.LookupRecordId)}", "lookup-registry"))).ToArray();
        var registryByKey = snapshot.Lookups.Registry.ToDictionary(item => (item.LookupRecordId, item.SysEntitySchemaUId));
        var values = new List<string?[]>();
        foreach (var collection in snapshot.Lookups.Collections.OrderBy(item => item.Schema.Identity.SchemaName, StringComparer.Ordinal).ThenBy(item => item.RegistryRecord.LookupRecordId))
        {
            if (!registryByKey.ContainsKey((collection.RegistryRecord.LookupRecordId, collection.RegistryRecord.SysEntitySchemaUId))) throw new InvalidDataException("LOOKUP_REGISTRY_RELATION_UNQUALIFIED");
            foreach (var row in collection.Rows.OrderBy(item => item.RecordId))
            foreach (var value in row.Values.OrderBy(item => item.ColumnName, StringComparer.Ordinal).ThenBy(item => item.ColumnUId))
            {
                if (value.RecordId != row.RecordId) throw new InvalidDataException("LOOKUP_VALUE_ROW_RELATION_UNQUALIFIED");
                values.Add(Row(collection.Schema.Identity.SchemaName, GuidText(collection.RegistryRecord.SysEntitySchemaUId), GuidText(row.RecordId), null,
                    "Active", "Present", row.SourceFingerprint, null, value.ColumnName, value.State.ToString(), DisplayValue(value), ValueKind(value.ValueKind),
                    GuidText(value.ReferenceRecordId), null, value.CanonicalValue));
            }
        }
        return
        [
            Sheet("LookupRegistry", registry, WorkbookContract.ValidationBindings("LookupRegistry")),
            Sheet("LookupValues", values.ToArray(), WorkbookContract.ValidationBindings("LookupValues")),
            ValidationLists(),
            PullConflicts()
        ];
    }

    private static WorksheetProjection Manifest(QualifiedCatalogSnapshot snapshot, string pairBaseline)
    {
        var components = WorkbookHash.Json(snapshot.ComponentDigests.OrderBy(item => item.StableIdentity, StringComparer.Ordinal).ThenBy(item => item.ComponentKind, StringComparer.Ordinal));
        var counts = WorkbookHash.Json(snapshot.Counts.OrderBy(item => item.Key, StringComparer.Ordinal));
        var rows = new List<string?[]>
        {
            Row("ContractVersion", WorkbookContract.ContractVersion), Row("PairId", GuidText(snapshot.PairId)), Row("PullRunId", GuidText(snapshot.RunId)),
            Row("TargetAlias", snapshot.Scope.TargetAlias), Row("ScopeMode", "AllReadableCatalog"), Row("PullStartedUtc", snapshot.PullStartedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)), Row("PullCompletedUtc", snapshot.PullCompletedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
            Row("PairBaselineHash", pairBaseline), Row("BaselineTargetFingerprint", snapshot.TargetFingerprint.Digest), Row("TemplateVersion", snapshot.Scope.WorkbookProjectionVersion),
            Row("SnapshotSchema", snapshot.Schema), Row("SourceIdentity", WorkbookContract.ClosedSourceIdentity(snapshot.SourceIdentity)), Row("ScopeDigest", snapshot.Scope.Digest),
            Row("PassADigest", snapshot.PassADigest), Row("PassBDigest", snapshot.PassBDigest), Row("ComponentDigest", components), Row("CountsDigest", counts)
        };
        rows.AddRange(snapshot.Counts.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => Row("Count." + item.Key, item.Value.ToString(CultureInfo.InvariantCulture))));
        rows.AddRange(snapshot.ComponentDigests.OrderBy(item => item.StableIdentity, StringComparer.Ordinal).ThenBy(item => item.ComponentKind, StringComparer.Ordinal).Select((item, index) => Row("ComponentDigest." + index.ToString("D6", CultureInfo.InvariantCulture), item.Digest)));
        return Sheet("Manifest", rows);
    }

    private static WorksheetProjection Readme(string kind) => Sheet("Readme", [
        Row("Workbook", kind + " Catalog"), Row("ContractVersion", WorkbookContract.ContractVersion), Row("Projection", "QualifiedCatalogSnapshot/v1 -> WorkbookProjection/v1"),
        Row("Identity", "Existing metadata uses BPMSoft UId; lookup records use server Id."), Row("ActualIndexed", "Separate source flag; never inferred from Indexes membership."),
        Row("Safety", "No external links, VBA, connections, cross-workbook formulas, Compare or Apply.")
    ]);

    private static WorksheetProjection ValidationLists()
    {
        string?[][] rows =
        [
            Row("Active", "Present", "Own", "entity", "Null", "Text", "TRUE", "Structured", "AllReadableCatalog", "BPMSoftTypeCode:0"),
            Row("Proposed", "PotentiallyDeleted", "Inherited", "lookup", "EmptyString", "Integer", "FALSE", "InventoryOnly", null, "BPMSoftTypeCode:4"),
            Row("Removed", "NotRead", "System", "other", "Value", "Decimal", null, "Unreadable", null, "BPMSoftTypeCode:7"),
            Row(null, null, null, null, null, "Boolean", null, "Unsupported", null, "BPMSoftTypeCode:10"),
            Row(null, null, null, null, null, "Date", null, null, null, "BPMSoftTypeCode:12"),
            Row(null, null, null, null, null, "DateTime", null, null, null, "BPMSoftTypeCode:14"),
            Row(null, null, null, null, null, "Guid", null, null, null, "BPMSoftTypeCode:16"),
            Row(null, null, null, null, null, "LookupReference", null, null, null, "BPMSoftTypeCode:27"),
            Row(null, null, null, null, null, "Time", null, null, null, "BPMSoftTypeCode:28"),
            Row(null, null, null, null, null, null, null, null, null, "BPMSoftTypeCode:29")
        ];
        return new WorksheetProjection("ValidationLists", WorkbookContract.ValidationHeaders, rows, true, true, EmptyValidations());
    }

    private static WorksheetProjection PullConflicts() => new("PullConflicts", WorkbookContract.ConflictHeaders, [], true, false, EmptyValidations());
    private static WorksheetProjection Sheet(string name, IReadOnlyList<string?[]> rows, IReadOnlyDictionary<string, string>? validations = null) => new(name, Headers(name), rows, WorkbookContract.IsReadOnly(name), name == "ValidationLists", validations ?? EmptyValidations());
    private static string[] Headers(string name) => WorkbookContract.ModelHeaders.TryGetValue(name, out var model) ? model : WorkbookContract.LookupHeaders[name];
    private static IReadOnlyDictionary<string, string> EmptyValidations() => new Dictionary<string, string>(StringComparer.Ordinal);
    private static object CanonicalSheet(WorksheetProjection sheet) => new { sheet.Name, sheet.Headers, sheet.Rows };
    private static void EnsureOrder(WorkbookProjection workbook, IReadOnlyList<string> order)
    {
        if (!workbook.Sheets.Select(sheet => sheet.Name).SequenceEqual(order, StringComparer.Ordinal)) throw new InvalidDataException("WORKBOOK_SHEET_ORDER_INVALID");
    }
    private static string SchemaFingerprint(QualifiedCatalogSnapshot snapshot, EntitySchemaModel schema) => ExactComponentFingerprint(snapshot, $"schema:{GuidText(schema.Identity.SchemaUId)}:layer:{schema.Identity.PackageLayer.PrimaryIdentityKey}", "schema");
    private static string ExactComponentFingerprint(QualifiedCatalogSnapshot snapshot, string stableIdentity, string kind)
    {
        var matches = snapshot.ComponentDigests.Where(item => string.Equals(item.StableIdentity, stableIdentity, StringComparison.Ordinal) && string.Equals(item.ComponentKind, kind, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1 || matches[0].Digest.Length != 64) throw new InvalidDataException("ACTUAL_FINGERPRINT_UNQUALIFIED:" + stableIdentity);
        return matches[0].Digest;
    }
    private static string? Requirement(int value) => value switch { 0 => "False", 1 => "True", _ => null };
    private static string Bool(bool value) => value ? "TRUE" : "FALSE";
    private static string ValueKind(LookupValueKind kind) => kind == LookupValueKind.Reference ? "LookupReference" : kind.ToString();
    private static string? DisplayValue(NormalizedLookupValue value) => value.State switch
    {
        LookupValueState.Null => null,
        LookupValueState.EmptyString => string.Empty,
        _ => value.TypedValue switch
        {
            LookupTextValue item => item.Value,
            LookupGuidValue item => GuidText(item.Value),
            LookupIntegerValue item => item.Value.ToString(CultureInfo.InvariantCulture),
            LookupDecimalValue item => item.Value.ToString(CultureInfo.InvariantCulture),
            LookupBooleanValue item => Bool(item.Value),
            LookupDateTimeValue item => item.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            LookupDateValue item => item.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            LookupTimeValue item => item.Value.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            LookupReferenceValue item => item.DisplayValue ?? GuidText(item.RecordId),
            _ => value.CanonicalValue
        }
    };
    private static string? GuidText(Guid? value) => value?.ToString("D").ToLowerInvariant();
    private static string?[] Row(params string?[] values) => values.Select(value => value ?? string.Empty).ToArray();
}

internal static class WorkbookHash
{
    public static string Json(object value) => Bytes(JsonSerializer.SerializeToUtf8Bytes(value));
    public static string File(string path) => Bytes(System.IO.File.ReadAllBytes(path));
    public static string Text(string value) => Bytes(Encoding.UTF8.GetBytes(value));
    private static string Bytes(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
