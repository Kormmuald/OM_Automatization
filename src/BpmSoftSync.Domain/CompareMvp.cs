using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BpmSoftSync.Domain;

/// <summary>In-memory only comparison contract. Raw desired values leave this model only through the local plan writer.</summary>
public sealed record CompareMvpBlocker(string Code, string StableKeyHash, string Action);
public sealed record CompareMvpOperation(string Kind, Guid SchemaUId, Guid RecordId, Guid ColumnUId, string DesiredState, string? ValueKind, string? DesiredValue, string DesiredValueHash, string ExpectedCurrentFingerprint);
public sealed record CompareMvpResult(string Status, IReadOnlyList<CompareMvpOperation> Operations, IReadOnlyList<CompareMvpBlocker> Blockers, IReadOnlyDictionary<string, int> Counts);

public sealed record CompareWorkbookPair(string TargetAlias, string ModelHash, string LookupHash, string BindingDigest, IReadOnlyList<IReadOnlyDictionary<string, string>> Columns, IReadOnlyList<IReadOnlyDictionary<string, string>> LookupValues);

public static class CompareMvpService
{
    private static readonly HashSet<string> ScalarKinds = new(StringComparer.Ordinal) { "Text", "Integer", "Decimal", "Boolean", "DateTime", "Date", "Time", "Guid" };

    public static CompareMvpResult Compare(CompareWorkbookPair pair, WorkspaceObjectModel workspace, LookupCatalog lookups)
    {
        var operations = new List<CompareMvpOperation>();
        var blockers = new List<CompareMvpBlocker>();
        foreach (var row in pair.Columns) CompareColumn(row, workspace, operations, blockers);
        foreach (var row in pair.LookupValues) CompareLookupValue(row, workspace, lookups, operations, blockers);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["operations"] = operations.Count,
            ["blockers"] = blockers.Count,
            ["requiredOperations"] = operations.Count(item => item.Kind == "Columns.DesiredRequired"),
            ["lookupValueOperations"] = operations.Count(item => item.Kind == "LookupValues.Value"),
            ["referenceOperations"] = operations.Count(item => item.Kind == "LookupValues.ReferenceRecordId")
        };
        return new(blockers.Count == 0 ? "completed" : "completed_with_blockers", operations, blockers, counts);
    }

    private static void CompareColumn(IReadOnlyDictionary<string, string> row, WorkspaceObjectModel workspace, List<CompareMvpOperation> operations, List<CompareMvpBlocker> blockers)
    {
        var key = Hash(Key(row, "SchemaUId", "ColumnUId"));
        if (!Guid.TryParse(Value(row, "ParentSchemaUId"), out var schemaId) || !Guid.TryParse(Value(row, "ColumnUId"), out var columnId)) { Block(blockers, "COLUMN_IDENTITY_INVALID", key, "Исправьте GUID-идентичность schema и column."); return; }
        if (!string.Equals(Value(row, "DesiredState"), "Active", StringComparison.Ordinal)) { Block(blockers, "UNSUPPORTED_DESIRED_STATE", key, "Верните DesiredState=Active; structural changes не поддерживаются."); return; }
        if (!string.IsNullOrEmpty(Value(row, "DesiredIndexed")) && !string.Equals(Value(row, "DesiredIndexed"), Value(row, "ActualIndexed"), StringComparison.Ordinal)) Block(blockers, "UNSUPPORTED_DESIRED_INDEXED", key, "Верните DesiredIndexed к ActualIndexed.");
        var schema = workspace.Schemas.SingleOrDefault(item => item.Identity.SchemaUId == schemaId);
        var column = schema?.Columns.SingleOrDefault(item => item.ColumnUId == columnId);
        if (schema is null || column is null) { Block(blockers, "CURRENT_COLUMN_NOT_READ", key, "Повторите best-effort чтение и проверьте строгие GUID."); return; }
        if (column.Ownership != ColumnOwnership.Own || !string.Equals(Value(row, "Ownership"), "Own", StringComparison.Ordinal))
        {
            // A generated inherited row has no editable DesiredRequired intent.
            // It is neither a structural change nor an operation, so it must not
            // drown independently actionable findings in blockers.
            if (string.IsNullOrEmpty(Value(row, "DesiredRequired"))) return;
            Block(blockers, "COLUMN_NOT_OWN", key, "Операции поддерживаются только для existing Own columns."); return;
        }
        if (!TryBool(Value(row, "DesiredRequired"), out var desired)) { Block(blockers, "DESIRED_REQUIRED_INVALID", key, "Укажите TRUE или FALSE."); return; }
        var current = column.RequirementType == 1;
        if (desired != current) operations.Add(new("Columns.DesiredRequired", schemaId, Guid.Empty, columnId, desired ? "Value" : "Value", "Boolean", desired ? "TRUE" : "FALSE", Hash(desired ? "TRUE" : "FALSE"), Fingerprint("required", schemaId, Guid.Empty, columnId, current ? "TRUE" : "FALSE")));
    }

    private static void CompareLookupValue(IReadOnlyDictionary<string, string> row, WorkspaceObjectModel workspace, LookupCatalog lookups, List<CompareMvpOperation> operations, List<CompareMvpBlocker> blockers)
    {
        var key = Hash(Key(row, "SysEntitySchemaUId", "RecordId", "ColumnUId", "ColumnName"));
        if (!Guid.TryParse(Value(row, "SysEntitySchemaUId"), out var schemaId) || !Guid.TryParse(Value(row, "RecordId"), out var recordId)) { Block(blockers, "LOOKUP_VALUE_IDENTITY_INVALID", key, "Исправьте GUID schema и record."); return; }
        if (!Guid.TryParse(Value(row, "ColumnUId"), out var columnId)) { Block(blockers, "LOOKUP_VALUE_COLUMN_IDENTITY_MISSING", key, "Используйте новую экспортированную пару с ColumnUId; fallback по ColumnName запрещён."); return; }
        if (!string.Equals(Value(row, "DesiredState"), "Active", StringComparison.Ordinal)) { Block(blockers, "UNSUPPORTED_LOOKUP_DESIRED_STATE", key, "Верните DesiredState=Active."); return; }
        if (!string.IsNullOrEmpty(Value(row, "DraftRowToken")) || !string.IsNullOrEmpty(Value(row, "ReferenceDraftRowToken"))) { Block(blockers, "REFERENCE_DRAFT_ROW_TOKEN_UNSUPPORTED", key, "Очистите DraftRowToken и ReferenceDraftRowToken."); return; }
        if (!string.IsNullOrEmpty(Value(row, "Comment"))) { Block(blockers, "LOOKUP_COMMENT_UNSUPPORTED", key, "Очистите Comment; изменение comments не поддерживается."); return; }
        var schema = workspace.Schemas.SingleOrDefault(item => item.Identity.SchemaUId == schemaId);
        var column = schema?.Columns.SingleOrDefault(item => item.ColumnUId == columnId);
        var collection = lookups.Collections.SingleOrDefault(item => item.RegistryRecord.SysEntitySchemaUId == schemaId);
        var actual = collection?.Rows.SingleOrDefault(item => item.RecordId == recordId)?.Values.SingleOrDefault(item => item.ColumnUId == columnId);
        if (schema is null || column is null || collection is null || actual is null) { Block(blockers, "CURRENT_LOOKUP_VALUE_NOT_READ", key, "Повторите best-effort чтение; затронутое значение должно быть уверенно прочитано."); return; }
        if (!string.Equals(column.ColumnName, Value(row, "ColumnName"), StringComparison.Ordinal)) { Block(blockers, "LOOKUP_VALUE_COLUMN_MISMATCH", key, "Исправьте пару; имя не соответствует column GUID."); return; }
        var kind = Value(row, "ValueKind");
        var state = Value(row, "ValueState");
        if (kind == "LookupReference") { CompareReference(row, schemaId, recordId, columnId, actual, key, operations, blockers); return; }
        if (!ScalarKinds.Contains(kind) || actual.ValueKind.ToString() != kind) { Block(blockers, "VALUE_KIND_MISMATCH_OR_UNSUPPORTED", key, "ValueKind должен быть неизменным поддерживаемым scalar типом."); return; }
        if (!TryNormalize(kind, state, Value(row, "Value"), out var normalized, out var error)) { Block(blockers, error!, key, "Согласуйте ValueState, Value и ValueKind с каноническим форматом."); return; }
        var current = Canonical(actual);
        if (state != actual.State.ToString() || !string.Equals(normalized, current, StringComparison.Ordinal)) operations.Add(new("LookupValues.Value", schemaId, recordId, columnId, state, kind, normalized, Hash(normalized ?? "<null>"), actual.ValueFingerprint));
    }

    private static void CompareReference(IReadOnlyDictionary<string, string> row, Guid schemaId, Guid recordId, Guid columnId, NormalizedLookupValue actual, string key, List<CompareMvpOperation> operations, List<CompareMvpBlocker> blockers)
    {
        if (actual.ValueKind != LookupValueKind.Reference) { Block(blockers, "VALUE_KIND_MISMATCH_OR_UNSUPPORTED", key, "ValueKind нельзя изменять."); return; }
        var reference = Value(row, "ReferenceRecordId").Normalize(NormalizationForm.FormC);
        if (string.IsNullOrEmpty(reference)) { Block(blockers, "REFERENCE_RECORD_ID_REQUIRED", key, "Укажите opaque ReferenceRecordId."); return; }
        var current = actual.ReferenceRecordId?.ToString("D");
        if (!string.Equals(reference, current, StringComparison.Ordinal)) operations.Add(new("LookupValues.ReferenceRecordId", schemaId, recordId, columnId, "Value", "LookupReference", reference, Hash(reference), actual.ValueFingerprint));
    }

    private static bool TryNormalize(string kind, string state, string raw, out string? value, out string? error)
    {
        value = null; error = null;
        if (state == "Null") { if (raw.Length != 0) { error = "VALUE_STATE_NULL_REQUIRES_EMPTY_VALUE"; return false; } return true; }
        if (state == "EmptyString") { if (raw.Length != 0) { error = "VALUE_STATE_EMPTY_STRING_REQUIRES_EMPTY_VALUE"; return false; } value = string.Empty; return true; }
        if (state != "Value") { error = "VALUE_STATE_INVALID"; return false; }
        try
        {
            value = kind switch
            {
                "Text" => raw.Normalize(NormalizationForm.FormC),
                "Integer" when long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var i) => i.ToString(CultureInfo.InvariantCulture),
                "Decimal" when decimal.TryParse(raw, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d) => d.ToString("0.#############################", CultureInfo.InvariantCulture),
                "Boolean" when raw is "TRUE" or "FALSE" => raw,
                "DateTime" when DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) => dt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                "Date" when DateOnly.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "Time" when TimeOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                "Guid" when Guid.TryParseExact(raw, "D", out var id) && id != Guid.Empty => id.ToString("D"),
                _ => null
            };
            if (value is not null) return true;
        }
        catch (FormatException) { }
        error = "VALUE_FORMAT_INVALID"; return false;
    }

    private static string? Canonical(NormalizedLookupValue value) => value.State switch { LookupValueState.Null => null, LookupValueState.EmptyString => string.Empty, _ => value.TypedValue switch { LookupTextValue item => item.Value.Normalize(NormalizationForm.FormC), LookupIntegerValue item => item.Value.ToString(CultureInfo.InvariantCulture), LookupDecimalValue item => item.Value.ToString("0.#############################", CultureInfo.InvariantCulture), LookupBooleanValue item => item.Value ? "TRUE" : "FALSE", LookupDateTimeValue item => item.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture), LookupDateValue item => item.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), LookupTimeValue item => item.Value.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture), LookupGuidValue item => item.Value.ToString("D"), _ => value.CanonicalValue } };
    // Existing best-effort workbooks used CLR-style True/False for this column;
    // accept that closed legacy spelling while canonical plans always use TRUE/FALSE.
    private static bool TryBool(string value, out bool result)
    {
        if (string.Equals(value, "TRUE", StringComparison.OrdinalIgnoreCase)) { result = true; return true; }
        if (string.Equals(value, "FALSE", StringComparison.OrdinalIgnoreCase)) { result = false; return true; }
        result = false; return false;
    }
    private static string Value(IReadOnlyDictionary<string, string> row, string name) => row.TryGetValue(name, out var value) ? value : string.Empty;
    private static string Key(IReadOnlyDictionary<string, string> row, params string[] names) => string.Join("|", names.Select(name => Value(row, name)));
    private static string Fingerprint(string kind, Guid schema, Guid record, Guid column, string value) => Hash($"{kind}|{schema:D}|{record:D}|{column:D}|{value}");
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static void Block(List<CompareMvpBlocker> blockers, string code, string key, string action) => blockers.Add(new(code, key, action));
}
