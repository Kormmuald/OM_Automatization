using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Application;
using BpmSoftSync.Cli.Commands;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Tests;

public static class ProductionWorkflowE2ETests
{
    private const string LookupCanary = "S06_WORKBOOK_ONLY_CANARY_2bc1";

    public static async Task FullCompositionPublishesExactAcceptedPairAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "bpmsoft-s06-e2e-" + Guid.NewGuid().ToString("N"));
        try
        {
            var source = new FullFixtureCatalogSource();
            var store = new AppendOnlyRunStore(root);
            var qualification = new CatalogQualificationService(source, Policy(), store);
            var publisher = new WorkbookPairSnapshotPublisher(new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()));
            var result = await new CatalogQualificationWorkflow(qualification, publisher).ExecuteAsync();
            if (!result.IsSuccess || source.ReadCount != 2) throw new InvalidOperationException($"The production composition did not complete exactly Pass A and Pass B: {result.Reason}; reads={source.ReadCount}.");
            var runPath = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).Single(path => Guid.TryParse(Path.GetFileName(path), out _));
            var outputPath = Path.Combine(runPath, "output");
            var evidencePath = Path.Combine(runPath, "evidence");
            var output = Directory.EnumerateFiles(outputPath).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!output.SequenceEqual([WorkbookContract.LookupFileName, WorkbookContract.ModelFileName], StringComparer.Ordinal) || !File.Exists(Path.Combine(evidencePath, "workbook-pair.json")) || !File.Exists(Path.Combine(evidencePath, "review-only-seal.json"))) throw new InvalidOperationException("The accepted S04 snapshot was not atomically published through S05.");
            var safeText = string.Join("\n", Directory.EnumerateFiles(runPath, "*.json", SearchOption.AllDirectories).Where(path => !path.Contains(Path.DirectorySeparatorChar + "output" + Path.DirectorySeparatorChar, StringComparison.Ordinal)).Select(File.ReadAllText));
            if (safeText.Contains(LookupCanary, StringComparison.Ordinal) || safeText.Contains("fixture-secret", StringComparison.Ordinal)) throw new InvalidOperationException("Safe E2E evidence leaked a lookup value or secret canary.");
            Console.WriteLine($"S06_E2E outputTreeSha256={OutputTreeHash(outputPath)} modelSha256={FileHash(Path.Combine(outputPath, WorkbookContract.ModelFileName))} lookupSha256={FileHash(Path.Combine(outputPath, WorkbookContract.LookupFileName))}");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    public static async Task LiveAdmissionRequiresBothFlagsAndSafeRootAsync()
    {
        var runner = new CaptureLiveRunner();
        var command = new CatalogQualifyCommand(new ManualInvocationPolicy(), runner);
        var noLive = await command.ExecuteAsync(["--target", "fixture", "--scope", "full", "--manual"], new StringWriter());
        var unsafeRoot = await command.ExecuteAsync(["--target", "fixture", "--scope", "full", "--manual", "--live", "--output-root", Path.GetTempPath()], new StringWriter());
        var safeRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync-S06-test-" + Guid.NewGuid().ToString("N"));
        var admitted = await command.ExecuteAsync(["--target", "fixture", "--scope", "full", "--manual", "--live", "--output-root", safeRoot], new StringWriter());
        if (noLive.Reason != "LIVE_ADMISSION_REQUIRED" || unsafeRoot.Reason != "OUTPUT_ROOT_NOT_ALLOWED" || !admitted.IsSuccess || runner.Count != 1 || runner.Target != "fixture") throw new InvalidOperationException("Live CLI admission is not explicit and fail-closed.");
    }

    private sealed class CaptureLiveRunner : ILiveCatalogQualificationRunner
    {
        public int Count { get; private set; }
        public string? Target { get; private set; }
        public ValueTask<SafeResult> ExecuteAsync(string targetAlias, string outputRoot, CancellationToken cancellationToken = default) { Count++; Target = targetAlias; return ValueTask.FromResult(SafeResult.SuccessForHumanReview("fake-live-admission")); }
    }

    private sealed class FullFixtureCatalogSource : IFullCatalogSource
    {
        public int ReadCount { get; private set; }
        public ValueTask<FullCatalogRead> ReadFullAsync(CatalogReadRequest request, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            var layer = new PackageLayerIdentity("opaque-fixture-package", Id("10000000-0000-0000-0000-000000000002"), "Current", "FixturePackage");
            var schemaIdentity = new SchemaIdentity("FixtureLookup", Id("30000000-0000-0000-0000-000000000001"), null, null, null, layer);
            var record = Id("70000000-0000-0000-0000-000000000001");
            var idColumn = Id("40000000-0000-0000-0000-000000000001"); var nameColumn = Id("40000000-0000-0000-0000-000000000002");
            var workspace = new WorkspaceObjectModel(true, WorkspaceInventory.Create([new(new(Id("20000000-0000-0000-0000-000000000001"), layer, "EntitySchema", schemaIdentity.SchemaUId), SupportStatus.Structured, "STRUCTURED", null, "FixtureLookup", [])]), [new(schemaIdentity, [new("Id", idColumn, 0, ColumnOwnership.Own, 0, 1, true, null, []), new("Name", nameColumn, 1, ColumnOwnership.Own, 1, 1, false, null, [])], [], [])], null);
            var registry = new LookupRegistryRecord(Id("60000000-0000-0000-0000-000000000001"), schemaIdentity.SchemaUId, schemaIdentity, null, Hash("registry"));
            var row = new LookupRow(record, [new(record, idColumn, "Id", LookupValueState.Value, LookupValueKind.Guid, new LookupGuidValue(record), record.ToString("D"), null, Hash("id"), Hash("id-source")), new(record, nameColumn, "Name", LookupValueState.Value, LookupValueKind.Text, new LookupTextValue(LookupCanary), LookupCanary, null, Hash("name"), Hash("name-source"))], Hash("row"), Hash("row-source"));
            var manifest = new OrderedCollectionManifest("lookup:FixtureLookup", "Id", [PageManifest.Create(0, "0", [record.ToString("D")], 128)], new(1, 1, 128, "0-16KiB", TimeSpan.Zero, Hash("ids")), Hash("collection"));
            var collection = new LookupCollection(registry, workspace.Schemas[0], [row], manifest, Hash("collection-source"));
            var registryManifest = new OrderedCollectionManifest("lookup-registry", "Id", [PageManifest.Create(0, "0", [registry.LookupRecordId.ToString("D")], 128)], new(1, 1, 128, "0-16KiB", TimeSpan.Zero, Hash("registry-ids")), Hash("registry-manifest"));
            var lookups = new LookupCatalog(true, [registry], registryManifest, [collection], null);
            var contracts = request.SealedScope?.Collections ?? request.Policy.CreatePassAContracts([manifest.CollectionId]);
            var attestation = new CatalogReadAttestation(Guid.NewGuid(), [Guid.NewGuid()], Guid.NewGuid(), new Dictionary<string, Guid> { [manifest.CollectionId] = Guid.NewGuid() });
            return ValueTask.FromResult(new FullCatalogRead(request.IndependentReadId, "fake:s06-full-catalog-v1", workspace, lookups, ["fixture-v1"], contracts, attestation, contracts.ToDictionary(contract => contract.CollectionId, _ => "0-16KiB", StringComparer.Ordinal), TimeSpan.Zero, 1_048_576));
        }
    }

    private static CatalogScopePolicy Policy() => CatalogScopePolicy.Create("fixture", "sha256:fixture", "ReadEndpointAllowlist/v1", new("workspace", "workspaceItemUId", "GetWorkspaceItems/v1", new(64, 10_000, 2L * 1024 * 1024 * 1024)), new("schemas", "schemaUId/packageLayer", "GetSchema/v1", new(1, 10_000, 2L * 1024 * 1024 * 1024)), new("lookup-registry", "Id", "SelectQuery/lookup-registry/v1", new(500, 5_000_000, 2L * 1024 * 1024 * 1024)), new("Id", "SelectQuery/lookup-values/v1", new(500, 5_000_000, 2L * 1024 * 1024 * 1024)), QualifiedCatalogSnapshot.SchemaVersion, "WorkbookProjection/v1");
    private static Guid Id(string value) => Guid.Parse(value);
    private static string Hash(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string FileHash(string path) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static string OutputTreeHash(string root) => Hash(string.Join("\n", Directory.EnumerateFiles(root).OrderBy(path => path, StringComparer.Ordinal).Select(path => Path.GetFileName(path) + "|" + FileHash(path))));
}
