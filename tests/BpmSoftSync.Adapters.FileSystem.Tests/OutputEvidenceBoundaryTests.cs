using System.IO.Compression;
using System.Text;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem.Tests;

public static class OutputEvidenceBoundaryTests
{
    private const string Canary = "S05_WORKBOOK_ONLY_CANARY_91c4";
    private static readonly string H1 = new('1', 64);
    private static readonly string H2 = new('2', 64);

    public static async Task RawLookupValueExistsOnlyInsidePublishedLookupWorkbookAsync(string parentRoot)
    {
        var root = Path.Combine(parentRoot, "s05-output-boundary");
        var store = new AppendOnlyRunStore(root);
        var started = new DateTimeOffset(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
        var times = new Queue<DateTimeOffset>([started, started.AddSeconds(1)]);
        var qualification = await new CatalogQualificationService(new BoundarySource(), Policy(), store, null, () => times.Dequeue()).QualifyAsync();
        var published = await new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()).PublishAsync(qualification);
        Assert(published.IsSuccess && published.Pair is not null, "Synthetic S05 pair publication failed.");
        var pair = published.Pair ?? throw new InvalidOperationException("Synthetic S05 pair is missing.");

        using (var archive = ZipFile.OpenRead(pair.LookupPath))
        {
            var contains = false;
            foreach (var entry in archive.Entries.Where(entry => entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)))
            {
                using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
                contains |= reader.ReadToEnd().Contains(Canary, StringComparison.Ordinal);
            }
            Assert(contains, "Raw lookup value was not materialized in the local Lookup workbook.");
        }

        var run = store.GetRunRoot(qualification.RunId);
        Assert(Path.GetDirectoryName(pair.ModelPath) == run.OutputPath && Path.GetDirectoryName(pair.LookupPath) == run.OutputPath, "Workbook pair is not in canonical output/*.xlsx.");
        var safeFiles = Directory.EnumerateFiles(run.RootPath, "*", SearchOption.AllDirectories).Where(path => !path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert(safeFiles.All(path => !File.ReadAllText(path).Contains(Canary, StringComparison.Ordinal)), "Raw lookup value escaped into audit/evidence/journal or control files.");
    }

    private sealed class BoundarySource : IFullCatalogSource
    {
        public ValueTask<FullCatalogRead> ReadFullAsync(CatalogReadRequest request, CancellationToken cancellationToken = default) => ValueTask.FromResult(Read(request));
    }

    private static CatalogScopePolicy Policy() => CatalogScopePolicy.Create("fixture", "sha256:fixture", "ReadEndpointAllowlist/v1",
        new("workspace", "workspaceItemUId", "GetWorkspaceItems/v1", new(64, 10_000, 2L * 1024 * 1024 * 1024)),
        new("schemas", "schemaUId/packageLayer", "GetSchema/v1", new(1, 10_000, 2L * 1024 * 1024 * 1024)),
        new("lookup-registry", "Id", "SelectQuery/lookup-registry/v1", new(500, 5_000_000, 2L * 1024 * 1024 * 1024)),
        new("Id", "SelectQuery/lookup-values/v1", new(500, 5_000_000, 2L * 1024 * 1024 * 1024)), QualifiedCatalogSnapshot.SchemaVersion, "WorkbookProjection/v1");

    private static FullCatalogRead Read(CatalogReadRequest request)
    {
        var packageUId = Guid.Parse("a0000000-0000-0000-0000-000000000002");
        var schemaUId = Guid.Parse("a0000000-0000-0000-0000-000000000003");
        var columnUId = Guid.Parse("a0000000-0000-0000-0000-000000000004");
        var recordId = Guid.Parse("a0000000-0000-0000-0000-000000000005");
        var registryId = Guid.Parse("a0000000-0000-0000-0000-000000000006");
        var layer = new PackageLayerIdentity(Guid.Parse("a0000000-0000-0000-0000-000000000001"), packageUId, "Current", "FixturePackage");
        var identity = new SchemaIdentity("FixtureLookup", schemaUId, null, null, null, layer);
        var inventory = WorkspaceInventory.Create([new WorkspaceInventoryItem(new WorkspaceItemIdentity(Guid.Parse("a0000000-0000-0000-0000-000000000007"), layer, "EntitySchema", schemaUId), SupportStatus.Structured, "STRUCTURED", null, "FixtureLookup", [])]);
        var schema = new EntitySchemaModel(identity, [new("Name", columnUId, 0, ColumnOwnership.Own, 10, 1, false, null, [])], [], []);
        var workspace = new WorkspaceObjectModel(true, inventory, [schema], null);
        var registry = new LookupRegistryRecord(registryId, schemaUId, identity, null, H1);
        var value = new NormalizedLookupValue(recordId, columnUId, "Name", LookupValueState.Value, LookupValueKind.Text, new LookupTextValue(Canary), Canary, null, H1, H2);
        var row = new LookupRow(recordId, [value], H1, H2);
        var page = PageManifest.Create(0, "0", [recordId.ToString("D")], 128);
        var manifest = new OrderedCollectionManifest("lookup:FixtureLookup", "Id", [page], new(1, 1, 128, "0-16KiB", TimeSpan.Zero, H1), H2);
        var registryPage = PageManifest.Create(0, "0", [registryId.ToString("D")], 128);
        var lookups = new LookupCatalog(true, [registry], new("lookup-registry", "Id", [registryPage], new(1, 1, 128, "0-16KiB", TimeSpan.Zero, H1), H2), [new(registry, schema, [row], manifest, H1)], null);
        var applied = request.SealedScope?.Collections.ToArray() ?? request.Policy.CreatePassAContracts([manifest.CollectionId]);
        var offset = request.Pass == CatalogPassOrdinal.A ? 0 : 10;
        var attestation = new CatalogReadAttestation(GuidFrom(offset + 1), [GuidFrom(offset + 2)], GuidFrom(offset + 3), new Dictionary<string, Guid> { [manifest.CollectionId] = GuidFrom(offset + 4) });
        return new FullCatalogRead(request.IndependentReadId, "fake:s05-boundary-v2", workspace, lookups, ["fixture"], applied, attestation, applied.ToDictionary(item => item.CollectionId, _ => "0-16KiB", StringComparer.Ordinal), TimeSpan.Zero, 1_048_576);
    }

    private static Guid GuidFrom(int suffix) => Guid.Parse($"b0000000-0000-0000-0000-{suffix:000000000000}");
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
