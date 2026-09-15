namespace BpmSoftSync.Domain;

public sealed record LookupReadLimits(int PageSize, int MaxPages, int MaxRows, long MaxResponseBytes)
{
    public static LookupReadLimits Default { get; } = new(500, 10_000, 5_000_000, 2L * 1024 * 1024 * 1024);

    public bool IsValid => PageSize > 0 && MaxPages > 0 && MaxRows > 0 && MaxResponseBytes > 0;
}

public enum LookupValueState { Null, EmptyString, Value }

public enum LookupValueKind { Guid, Text, Integer, Decimal, Boolean, DateTime, Date, Time, Reference }

public abstract record LookupTypedValue;
public sealed record LookupTextValue(string Value) : LookupTypedValue;
public sealed record LookupGuidValue(Guid Value) : LookupTypedValue;
public sealed record LookupIntegerValue(long Value) : LookupTypedValue;
public sealed record LookupDecimalValue(decimal Value) : LookupTypedValue;
public sealed record LookupBooleanValue(bool Value) : LookupTypedValue;
public sealed record LookupDateTimeValue(DateTimeOffset Value) : LookupTypedValue;
public sealed record LookupDateValue(DateOnly Value) : LookupTypedValue;
public sealed record LookupTimeValue(TimeOnly Value) : LookupTypedValue;
public sealed record LookupReferenceValue(Guid RecordId, string? DisplayValue) : LookupTypedValue;

public sealed record LookupRegistryRecord(
    Guid LookupRecordId,
    Guid SysEntitySchemaUId,
    SchemaIdentity SchemaIdentity,
    SchemaReference? BaseSchemaIdentity,
    string SourceFingerprint);

public sealed record NormalizedLookupValue(
    Guid RecordId,
    Guid ColumnUId,
    string ColumnName,
    LookupValueState State,
    LookupValueKind ValueKind,
    LookupTypedValue? TypedValue,
    string? CanonicalValue,
    Guid? ReferenceRecordId,
    string ValueFingerprint,
    string SourceFingerprint);

public sealed record LookupRow(
    Guid RecordId,
    IReadOnlyList<NormalizedLookupValue> Values,
    string RowFingerprint,
    string SourceFingerprint);

public sealed record OrderedCollectionTelemetry(
    int PageCount,
    int RowCount,
    long ResponseBytes,
    string ResponseSizeBucket,
    TimeSpan Duration,
    string OrderedIdentityDigest);

public sealed record OrderedCollectionManifest(
    string CollectionId,
    string OrderKeyId,
    IReadOnlyList<PageManifest> Pages,
    OrderedCollectionTelemetry Telemetry,
    string SourceFingerprint);

public sealed record LookupCollection(
    LookupRegistryRecord RegistryRecord,
    EntitySchemaModel Schema,
    IReadOnlyList<LookupRow> Rows,
    OrderedCollectionManifest Manifest,
    string SourceFingerprint)
{
    public IReadOnlyList<NormalizedLookupValue> Values => Rows.SelectMany(row => row.Values).ToArray();
}

public sealed record LookupCatalog(
    bool IsQualified,
    IReadOnlyList<LookupRegistryRecord> Registry,
    OrderedCollectionManifest? RegistryManifest,
    IReadOnlyList<LookupCollection> Collections,
    Blocker? Blocker)
{
    public static LookupCatalog Blocked(
        IReadOnlyList<LookupRegistryRecord> registry,
        OrderedCollectionManifest? registryManifest,
        IReadOnlyList<LookupCollection> collections,
        Blocker blocker) => new(false, registry, registryManifest, collections, blocker);
}
