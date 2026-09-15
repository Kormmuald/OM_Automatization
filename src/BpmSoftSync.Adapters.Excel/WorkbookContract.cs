using System.Security.Cryptography;
using System.Text;

namespace BpmSoftSync.Adapters.Excel;

public static class WorkbookContract
{
    public const string ContractVersion = "1";
    public const string ModelFileName = "BPMSoft.ModelCatalog.xlsx";
    public const string LookupFileName = "BPMSoft.LookupCatalog.xlsx";

    public static string ClosedSourceIdentity(string sourceIdentity)
    {
        if (string.IsNullOrWhiteSpace(sourceIdentity)) throw new InvalidDataException("SOURCE_IDENTITY_UNQUALIFIED");
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceIdentity.Normalize(NormalizationForm.FormC)))).ToLowerInvariant();
    }

    public static readonly string[] ModelSheetOrder = ["Readme", "Manifest", "WorkspaceInventory", "Schemas", "Columns", "Indexes", "ValidationLists", "PullConflicts"];
    public static readonly string[] LookupSheetOrder = ["Readme", "Manifest", "LookupRegistry", "LookupValues", "ValidationLists", "PullConflicts"];
    public static readonly string[] ValidationHeaders = ["DesiredState", "ServerPresence", "Ownership", "SchemaKind", "ValueState", "ValueKind", "BooleanChoice", "SupportStatus", "ScopeMode", "DataType"];
    public static readonly string[] ConflictHeaders = ["ConflictCode", "Workbook", "Sheet", "StableKey", "ColumnName", "BeforeValue", "ServerValue", "ResolutionStatus", "Message"];

    public static readonly IReadOnlyDictionary<string, string[]> ModelHeaders = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Readme"] = ["Topic", "Value"],
        ["Manifest"] = ["Key", "Value"],
        ["WorkspaceInventory"] = ["WorkspaceItemUId", "Name", "ItemType", "PackageName", "PackageUId", "SupportStatus", "SupportReason"],
        ["Schemas"] = ["SchemaName", "SchemaUId", "SysSchemaId", "SchemaKind", "ParentSchemaName", "ParentSchemaUId", "DesiredPackageName", "ActualPackageName", "ActualPackageUId", "DesiredState", "ServerPresence", "ActualFingerprint"],
        ["Columns"] = ["SchemaName", "ParentSchemaUId", "ColumnName", "ColumnUId", "Ownership", "DataType", "ReferenceSchemaName", "ReferenceSchemaUId", "DesiredRequired", "ActualRequired", "DesiredIndexed", "ActualIndexed", "DesiredState", "ServerPresence", "ActualFingerprint"],
        ["Indexes"] = ["SchemaName", "SchemaUId", "IndexUId", "IndexName", "IsUnique", "ColumnName", "ColumnUId", "Ordinal", "ActualFingerprint"],
        ["ValidationLists"] = ValidationHeaders,
        ["PullConflicts"] = ConflictHeaders
    };

    public static readonly IReadOnlyDictionary<string, string[]> LookupHeaders = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Readme"] = ["Topic", "Value"],
        ["Manifest"] = ["Key", "Value"],
        ["LookupRegistry"] = ["SchemaName", "SysEntitySchemaUId", "LookupRecordId", "BaseSchemaName", "BaseSchemaUId", "DesiredState", "ServerPresence", "ActualFingerprint"],
        ["LookupValues"] = ["SchemaName", "SysEntitySchemaUId", "RecordId", "DraftRowToken", "DesiredState", "ServerPresence", "SourceFingerprint", "Comment", "ColumnName", "ValueState", "Value", "ValueKind", "ReferenceRecordId", "ReferenceDraftRowToken", "CanonicalValue"],
        ["ValidationLists"] = ValidationHeaders,
        ["PullConflicts"] = ConflictHeaders
    };

    public static bool IsReadOnly(string sheetName) => sheetName is "Readme" or "Manifest" or "WorkspaceInventory" or "Indexes" or "ValidationLists" or "PullConflicts";

    public static IReadOnlySet<int> EditableColumns(string sheetName, IReadOnlyList<string?> row) => sheetName switch
    {
        "Schemas" => Set(9),
        "Columns" when string.Equals(row[4], "Own", StringComparison.Ordinal) => Set(8, 10, 12),
        "LookupRegistry" => Set(5),
        "LookupValues" => Set(4, 7, 9, 10, 12, 13),
        _ => Set()
    };

    public static int ValidationValueCount(string header) => header switch
    {
        "DesiredState" => 3, "ServerPresence" => 3, "Ownership" => 3, "SchemaKind" => 3, "ValueState" => 3,
        "ValueKind" => 9, "BooleanChoice" => 2, "SupportStatus" => 4, "ScopeMode" => 1, "DataType" => 10,
        _ => throw new InvalidDataException("WORKBOOK_VALIDATION_LIST_UNKNOWN")
    };

    public static IReadOnlyDictionary<string, string> ValidationBindings(string sheetName) => sheetName switch
    {
        "Schemas" => Bind(("SchemaKind", "SchemaKind"), ("DesiredState", "DesiredState"), ("ServerPresence", "ServerPresence")),
        "Columns" => Bind(("Ownership", "Ownership"), ("DataType", "DataType"), ("DesiredRequired", "BooleanChoice"), ("DesiredIndexed", "BooleanChoice"), ("DesiredState", "DesiredState"), ("ServerPresence", "ServerPresence")),
        "LookupRegistry" => Bind(("DesiredState", "DesiredState"), ("ServerPresence", "ServerPresence")),
        "LookupValues" => Bind(("DesiredState", "DesiredState"), ("ServerPresence", "ServerPresence"), ("ValueState", "ValueState"), ("ValueKind", "ValueKind")),
        _ => new Dictionary<string, string>(StringComparer.Ordinal)
    };

    private static IReadOnlySet<int> Set(params int[] values) => new HashSet<int>(values);
    private static IReadOnlyDictionary<string, string> Bind(params (string Column, string Name)[] values) => values.ToDictionary(item => item.Column, item => item.Name, StringComparer.Ordinal);
}
