using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.Excel.Tests;

internal static class SyntheticSnapshot
{
    public const string RawLookupValue = "S05_LOCAL_LOOKUP_VALUE_8f71";
    public static readonly DateTimeOffset PullStartedUtc = new(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset PullCompletedUtc = new(2026, 9, 14, 10, 0, 2, TimeSpan.Zero);
    public static readonly Guid PrimarySchemaUId = Id("30000000-0000-0000-0000-000000000001");
    public static readonly Guid SecondarySchemaUId = Id("30000000-0000-0000-0000-000000000002");
    public static readonly Guid CollisionSchemaOneUId = Id("30000000-0000-0000-0000-000000000003");
    public static readonly Guid CollisionSchemaTwoUId = Id("30000000-0000-0000-0000-000000000004");
    public static readonly Guid IdColumnUId = Id("40000000-0000-0000-0000-000000000001");
    public static readonly Guid NameColumnUId = Id("40000000-0000-0000-0000-000000000002");
    public static readonly Guid InheritedColumnUId = Id("40000000-0000-0000-0000-000000000012");
    public static readonly Guid CompositeIndexUId = Id("60000000-0000-0000-0000-000000000001");
    public static readonly Guid PrimaryRecordId = Id("80000000-0000-0000-0000-000000000001");
    public static readonly Guid SecondaryRecordId = Id("80000000-0000-0000-0000-000000000002");

    public static CatalogQualification CreateQualification() => CreateService(null).QualifyAsync().AsTask().GetAwaiter().GetResult();
    public static ValueTask<CatalogQualification> CreateAcceptedQualificationAsync(AppendOnlyRunStore store) => CreateService(store).QualifyAsync();

    private static CatalogQualificationService CreateService(AppendOnlyRunStore? store)
    {
        var ids = new Queue<Guid>([Id("10000000-0000-0000-0000-000000000001"), Id("10000000-0000-0000-0000-000000000003"), Id("10000000-0000-0000-0000-000000000004"), Id("10000000-0000-0000-0000-000000000002")]);
        var times = new Queue<DateTimeOffset>([PullStartedUtc, PullCompletedUtc]);
        return new CatalogQualificationService(new SyntheticFullCatalogSource(), Policy(), store, () => ids.Dequeue(), () => times.Dequeue());
    }

    private static CatalogScopePolicy Policy() => CatalogScopePolicy.Create(
        "fixture", "sha256:fixture-origin-policy", "ReadEndpointAllowlist/v1",
        new("workspace", "workspaceItemUId", "GetWorkspaceItems/v1", new(64, 10_000, 2L * 1024 * 1024 * 1024)),
        new("schemas", "schemaUId/packageLayer", "GetSchema/v1", new(1, 10_000, 2L * 1024 * 1024 * 1024)),
        new("lookup-registry", "Id", "SelectQuery/lookup-registry/v1", new(500, 5_000_000, 2L * 1024 * 1024 * 1024)),
        new("Id", "SelectQuery/lookup-values/v1", new(500, 5_000_000, 2L * 1024 * 1024 * 1024)),
        QualifiedCatalogSnapshot.SchemaVersion, "WorkbookProjection/v1");

    private sealed class SyntheticFullCatalogSource : IFullCatalogSource
    {
        public ValueTask<FullCatalogRead> ReadFullAsync(CatalogReadRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Read(request));
        }
    }

    private static FullCatalogRead Read(CatalogReadRequest request)
    {
        var packageOne = new PackageLayerIdentity("opaque-package-one", Id("21000000-0000-0000-0000-000000000001"), "Current", "PackageOne");
        var packageTwo = new PackageLayerIdentity("opaque-package-two", Id("21000000-0000-0000-0000-000000000002"), "Current", "PackageTwo");
        var baseReference = new SchemaReference("BaseLookup", Id("30000000-0000-0000-0000-000000000099"));
        var primaryIdentity = new SchemaIdentity("PrimaryLookup", PrimarySchemaUId, Id("31000000-0000-0000-0000-000000000001"), baseReference.SchemaName, baseReference.SchemaUId, packageOne);
        var secondaryIdentity = new SchemaIdentity("SecondaryLookup", SecondarySchemaUId, Id("31000000-0000-0000-0000-000000000002"), baseReference.SchemaName, baseReference.SchemaUId, packageTwo);
        var collisionOne = new SchemaIdentity("CollisionEntity", CollisionSchemaOneUId, Id("31000000-0000-0000-0000-000000000003"), null, null, packageOne);
        var collisionTwo = new SchemaIdentity("CollisionEntity", CollisionSchemaTwoUId, Id("31000000-0000-0000-0000-000000000004"), null, null, packageTwo);

        var primaryColumns = new[]
        {
            Column("Id", IdColumnUId, 0, 0, true), Column("Name", NameColumnUId, 1, 10, false),
            Column("NullableText", Id("40000000-0000-0000-0000-000000000003"), 2, 10),
            Column("EmptyText", Id("40000000-0000-0000-0000-000000000004"), 3, 10),
            Column("IntegerValue", Id("40000000-0000-0000-0000-000000000005"), 4, 4),
            Column("DecimalValue", Id("40000000-0000-0000-0000-000000000006"), 5, 7),
            Column("BooleanValue", Id("40000000-0000-0000-0000-000000000007"), 6, 12),
            Column("DateValue", Id("40000000-0000-0000-0000-000000000008"), 7, 14),
            Column("DateTimeValue", Id("40000000-0000-0000-0000-000000000009"), 8, 16),
            Column("TimeValue", Id("40000000-0000-0000-0000-000000000010"), 9, 28),
            Column("ReferenceValue", Id("40000000-0000-0000-0000-000000000011"), 10, 10, false, new("SecondaryLookup", SecondarySchemaUId)),
            Column("InheritedContext", InheritedColumnUId, 11, 10, true, null, ColumnOwnership.Inherited)
        };
        var primary = new EntitySchemaModel(primaryIdentity, primaryColumns,
        [
            new(CompositeIndexUId, "IX_Primary_Composite", true, false, [new(IdColumnUId, 0, []), new(NameColumnUId, 1, [])], []),
            new(Id("60000000-0000-0000-0000-000000000002"), "IX_Primary_Name", false, true, [new(NameColumnUId, 0, [])], [])
        ], []);
        var secondaryIdColumn = Column("Id", Id("41000000-0000-0000-0000-000000000001"), 0, 0, true);
        var secondaryNameColumn = Column("Name", Id("41000000-0000-0000-0000-000000000002"), 1, 10);
        var secondary = new EntitySchemaModel(secondaryIdentity, [secondaryIdColumn, secondaryNameColumn], [], []);
        var collisionSchemaOne = new EntitySchemaModel(collisionOne, [Column("Id", Id("42000000-0000-0000-0000-000000000001"), 0, 0)], [], []);
        var collisionSchemaTwo = new EntitySchemaModel(collisionTwo, [Column("Id", Id("42000000-0000-0000-0000-000000000002"), 0, 0)], [], []);
        var schemas = new[] { primary, secondary, collisionSchemaOne, collisionSchemaTwo };
        var inventoryItems = schemas.Select((schema, index) => new WorkspaceInventoryItem(new WorkspaceItemIdentity(Id($"50000000-0000-0000-0000-{index + 1:000000000000}"), schema.Identity.PackageLayer, "EntitySchema", schema.Identity.SchemaUId), SupportStatus.Structured, "STRUCTURED", null, schema.Identity.SchemaName, [])).ToList();
        inventoryItems.Add(new(new WorkspaceItemIdentity(Id("50000000-0000-0000-0000-000000000099"), packageTwo, "ClientUnitSchema"), SupportStatus.Unsupported, "UNSUPPORTED", new("object", "{}", Hash('a'), 0, Hash('b')), "UnsupportedClient", []));
        var workspace = new WorkspaceObjectModel(true, WorkspaceInventory.Create(inventoryItems), schemas, null);

        var registryPrimary = new LookupRegistryRecord(Id("70000000-0000-0000-0000-000000000001"), PrimarySchemaUId, primaryIdentity, baseReference, Hash('c'));
        var registrySecondary = new LookupRegistryRecord(Id("70000000-0000-0000-0000-000000000002"), SecondarySchemaUId, secondaryIdentity, baseReference, Hash('d'));
        var primaryValues = new[]
        {
            Value(IdColumnUId, "Id", LookupValueKind.Guid, new LookupGuidValue(PrimaryRecordId), PrimaryRecordId.ToString("D")),
            Value(NameColumnUId, "Name", LookupValueKind.Text, new LookupTextValue(RawLookupValue), RawLookupValue),
            Value(primaryColumns[2].ColumnUId, "NullableText", LookupValueKind.Text, null, null, LookupValueState.Null),
            Value(primaryColumns[3].ColumnUId, "EmptyText", LookupValueKind.Text, new LookupTextValue(string.Empty), string.Empty, LookupValueState.EmptyString),
            Value(primaryColumns[4].ColumnUId, "IntegerValue", LookupValueKind.Integer, new LookupIntegerValue(42), "42"),
            Value(primaryColumns[5].ColumnUId, "DecimalValue", LookupValueKind.Decimal, new LookupDecimalValue(123.4500m), "123.45"),
            Value(primaryColumns[6].ColumnUId, "BooleanValue", LookupValueKind.Boolean, new LookupBooleanValue(true), "true"),
            Value(primaryColumns[7].ColumnUId, "DateValue", LookupValueKind.Date, new LookupDateValue(new DateOnly(2026, 9, 14)), "2026-09-14"),
            Value(primaryColumns[8].ColumnUId, "DateTimeValue", LookupValueKind.DateTime, new LookupDateTimeValue(new DateTimeOffset(2026, 9, 14, 12, 34, 56, TimeSpan.Zero)), "2026-09-14T12:34:56.0000000+00:00"),
            Value(primaryColumns[9].ColumnUId, "TimeValue", LookupValueKind.Time, new LookupTimeValue(new TimeOnly(12, 34, 56)), "12:34:56.0000000"),
            Value(primaryColumns[10].ColumnUId, "ReferenceValue", LookupValueKind.Reference, new LookupReferenceValue(SecondaryRecordId, "Secondary"), SecondaryRecordId.ToString("D"), LookupValueState.Value, SecondaryRecordId)
        };
        var secondaryValues = new[]
        {
            new NormalizedLookupValue(SecondaryRecordId, secondaryIdColumn.ColumnUId, "Id", LookupValueState.Value, LookupValueKind.Guid, new LookupGuidValue(SecondaryRecordId), SecondaryRecordId.ToString("D"), null, Hash('e'), Hash('f')),
            new NormalizedLookupValue(SecondaryRecordId, secondaryNameColumn.ColumnUId, "Name", LookupValueState.Value, LookupValueKind.Text, new LookupTextValue("Secondary"), "Secondary", null, Hash('e'), Hash('f'))
        };
        var primaryCollection = Collection(registryPrimary, primary, [new(PrimaryRecordId, primaryValues, Hash('1'), Hash('2'))], "lookup:PrimaryLookup", PrimaryRecordId, '5');
        var secondaryCollection = Collection(registrySecondary, secondary, [new(SecondaryRecordId, secondaryValues, Hash('3'), Hash('4'))], "lookup:SecondaryLookup", SecondaryRecordId, '6');
        var lookups = new LookupCatalog(true, [registryPrimary, registrySecondary], Manifest("lookup-registry", [registryPrimary.LookupRecordId, registrySecondary.LookupRecordId], '7'), [primaryCollection, secondaryCollection], null);
        var applied = request.SealedScope?.Collections.ToArray() ?? request.Policy.CreatePassAContracts([primaryCollection.Manifest.CollectionId, secondaryCollection.Manifest.CollectionId]);
        var buckets = applied.ToDictionary(item => item.CollectionId, _ => "0-16KiB", StringComparer.Ordinal);
        var offset = request.Pass == CatalogPassOrdinal.A ? 0 : 20;
        var attestation = new CatalogReadAttestation(Id($"90000000-0000-0000-0000-{offset + 1:000000000000}"), schemas.Select((_, i) => Id($"90000000-0000-0000-0000-{offset + i + 2:000000000000}")).ToArray(), Id($"90000000-0000-0000-0000-{offset + 6:000000000000}"), new Dictionary<string, Guid>
        {
            [primaryCollection.Manifest.CollectionId] = Id($"90000000-0000-0000-0000-{offset + 7:000000000000}"), [secondaryCollection.Manifest.CollectionId] = Id($"90000000-0000-0000-0000-{offset + 8:000000000000}")
        });
        return new FullCatalogRead(request.IndependentReadId, "fake:s05-full-catalog-v2", workspace, lookups, ["fixture-v2"], applied, attestation, buckets, TimeSpan.FromSeconds(1), 1_048_576);
    }

    private static EntityColumnModel Column(string name, Guid id, int ordinal, int typeCode, bool indexed = false, SchemaReference? reference = null, ColumnOwnership ownership = ColumnOwnership.Own) => new(name, id, ordinal, ownership, typeCode, 1, indexed, reference, []);
    private static NormalizedLookupValue Value(Guid columnId, string name, LookupValueKind kind, LookupTypedValue? typed, string? canonical, LookupValueState state = LookupValueState.Value, Guid? referenceId = null) => new(PrimaryRecordId, columnId, name, state, kind, typed, canonical, referenceId, Hash('8'), Hash('9'));
    private static LookupCollection Collection(LookupRegistryRecord registry, EntitySchemaModel schema, IReadOnlyList<LookupRow> rows, string id, Guid recordId, char hash) => new(registry, schema, rows, Manifest(id, [recordId], hash), Hash(hash));
    private static OrderedCollectionManifest Manifest(string id, IReadOnlyList<Guid> identities, char hash) => new(id, "Id", [PageManifest.Create(0, "0", identities.Select(value => value.ToString("D")).ToArray(), 256)], new(1, identities.Count, 256, "0-16KiB", TimeSpan.FromMilliseconds(10), PageManifest.Digest(string.Join("\n", identities))), Hash(hash));
    private static Guid Id(string value) => Guid.Parse(value);
    private static string Hash(char value) => new(value, 64);
}
