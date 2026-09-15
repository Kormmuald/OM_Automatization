using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Application.Tests;

public static class CatalogQualificationServiceTests
{
    public static async Task ExactlyTwoIndependentFullReadsProduceBOnlySnapshotAndSafeEvidenceAsync()
    {
        var source = new CountingFullCatalogSource();
        var root = NewRoot();
        try
        {
            var store = new AppendOnlyRunStore(root);
            var result = await new CatalogQualificationService(source, FixtureCatalog.Policy(), store).QualifyAsync();

            Assert(result.IsQualified && result.Snapshot is not null, "Equal full reads did not create a qualified snapshot.");
            var snapshot = result.Snapshot ?? throw new InvalidOperationException("Qualified snapshot is absent.");
            Assert(source.FullReadCount == 2 && source.WorkspaceReadCount == 2 && source.SchemaReadCount == 2 && source.LookupReadCount == 2, "The complete source was not independently read exactly twice.");
            Assert(source.Requests.Count == 2 && source.Requests[0].Pass == CatalogPassOrdinal.A && source.Requests[1].Pass == CatalogPassOrdinal.B, "Pass order is not exactly A then B.");
            Assert(source.Requests[0].IndependentReadId != source.Requests[1].IndependentReadId, "Pass B reused the Pass A read identity.");
            Assert(source.Requests[0].SealedScope is null && source.Requests[1].SealedScope is not null, "Lookup scope was sealed before Pass A discovery or was absent from Pass B.");
            Assert(source.Requests[1].SealedScope!.Collections.Select(item => item.CollectionId).SequenceEqual(new[] { "workspace", "schemas", "lookup-registry", "lookup:FixtureLookup" }, StringComparer.Ordinal), "Pass-A registry did not derive the exact Pass-B lookup scope.");
            Assert(result.PassA is not null && result.PassB is not null && !ReferenceEquals(result.PassA.Content.Workspace, result.PassB.Content.Workspace) && !ReferenceEquals(result.PassA.Content.Lookups, result.PassB.Content.Lookups), "Pass B reused Pass A materialized content.");
            var passB = result.PassB ?? throw new InvalidOperationException("Pass B is absent.");
            Assert(ReferenceEquals(snapshot.Workspace, passB.Content.Workspace) && ReferenceEquals(snapshot.Lookups, passB.Content.Lookups), "Snapshot is not materialized from the latest reconciled Pass B.");
            Assert(snapshot.Schema == QualifiedCatalogSnapshot.SchemaVersion && snapshot.RunId == result.RunId && snapshot.PairId != Guid.Empty, "Snapshot identity/version contract is incomplete.");
            Assert(snapshot.SourceIdentity == "fake:s04-full-catalog-v1" && snapshot.ComponentDigests.Count >= 4 && snapshot.OrderedIdentities.Count >= 8, "Snapshot omitted source/component/identity evidence required by S05.");
            Assert(snapshot.PullStartedUtc != default && snapshot.PullCompletedUtc >= snapshot.PullStartedUtc, "Qualified snapshot omitted the real S04 pull interval required by the workbook manifest.");
            Assert(snapshot.Lookups.Collections.Single().Values.Single(value => value.ColumnName == "Name").CanonicalValue == FixtureCatalog.RawLookupCanary, "In-memory snapshot lost an allowed lookup value.");
            Assert(snapshot.Scale.Status == WorkbookScaleForecastStatus.DiagnosticOnly, "Workbook-scale forecast was not attached to the snapshot.");

            var durable = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Select(File.ReadAllText).ToArray();
            Assert(durable.Length >= 5, "Typed pass/reconciliation/snapshot/seal evidence is incomplete.");
            Assert(durable.All(text => !text.Contains(FixtureCatalog.RawLookupCanary, StringComparison.Ordinal)), "Raw lookup value escaped into audit/evidence/journal.");
            Assert(durable.Any(text => text.Contains("\"stableKey\":\"pass-a\"", StringComparison.Ordinal)) && durable.Any(text => text.Contains("\"stableKey\":\"pass-b\"", StringComparison.Ordinal)) && durable.Any(text => text.Contains("\"stableKey\":\"qualified-snapshot\"", StringComparison.Ordinal)), "Typed qualification evidence is missing.");
            Assert(durable.Any(text => text.Contains("\"pullWindow\"", StringComparison.Ordinal) && text.Contains("\"snapshot\"", StringComparison.Ordinal)), "Persisted S04 evidence did not bind the pull interval and snapshot/pair identity.");
            var passAEvidence = durable.Single(text => text.Contains("\"stableKey\":\"pass-a\"", StringComparison.Ordinal) && !text.Contains(Environment.NewLine, StringComparison.Ordinal));
            Assert(passAEvidence.Contains("\"orderKeyId\":\"Id\"", StringComparison.Ordinal) && passAEvidence.Contains("\"queryContractId\":\"SelectQuery/lookup-values/v1\"", StringComparison.Ordinal) && passAEvidence.Contains("\"pageSize\":500", StringComparison.Ordinal) && passAEvidence.Contains("\"maxPages\":10000", StringComparison.Ordinal) && passAEvidence.Contains("\"maxRows\":5000000", StringComparison.Ordinal) && passAEvidence.Contains("\"maxResponseBytes\":2147483648", StringComparison.Ordinal), "Pass evidence omitted the sealed order/query/limits contract.");
            Assert(Directory.EnumerateDirectories(Path.Combine(root, "runs"), "*", SearchOption.AllDirectories).Count(path => Guid.TryParse(Path.GetFileName(path), out _)) == 1, "Qualification created more than one RunId lifecycle.");
            var downstreamProbe = new QualificationEvidenceEnvelope(QualificationEvidenceEnvelope.SchemaVersion, "downstream-stage-probe", snapshot.PassBDigest, new QualificationEvidenceMetadata(result.RunId, "fixture", "downstream-stage-probe", "SAFE", 0, new Dictionary<string, int>(), new Dictionary<string, string> { ["passB"] = snapshot.PassBDigest }, "not-recorded", "not-recorded", [], [], ["S05_NOT_STARTED"]));
            Assert((await store.AppendAsync(result.RunId, downstreamProbe)).IsSuccess, "Qualified S04 prematurely sealed the RunId needed by S05.");
        }
        finally { DeleteRoot(root); }
    }

    public static async Task CacheReuseIsRejectedWithoutPassCAsync()
    {
        var source = new CountingFullCatalogSource(cachePassAForB: true);
        var result = await new CatalogQualificationService(source, FixtureCatalog.Policy()).QualifyAsync();
        Assert(!result.IsQualified && result.Snapshot is null && result.Result.Reason == "PASS_B_INDEPENDENCE_UNQUALIFIED", "Pass A cache reuse was not rejected.");
        Assert(source.FullReadCount == 2 && result.RetryCount == 0, "Cache trap triggered a retry or Pass C.");
        var nestedSource = new CountingFullCatalogSource(reusePassASchemasForB: true);
        var nested = await new CatalogQualificationService(nestedSource, FixtureCatalog.Policy()).QualifyAsync();
        Assert(!nested.IsQualified && nested.Result.Reason == "PASS_B_INDEPENDENCE_UNQUALIFIED" && nestedSource.FullReadCount == 2, "Nested Pass A schema cache crossed the independence boundary.");
        var deepClone = new CountingFullCatalogSource(deepClonePassACacheForB: true);
        var cloned = await new CatalogQualificationService(deepClone, FixtureCatalog.Policy()).QualifyAsync();
        Assert(!cloned.IsQualified && cloned.Result.Reason == "PASS_B_INDEPENDENCE_UNQUALIFIED" && deepClone.FullReadCount == 2 && deepClone.WorkspaceReadCount == 1 && deepClone.SchemaReadCount == 1 && deepClone.LookupReadCount == 1, "Deep-cloned Pass A cache with a fresh outer ID crossed source-read attestation.");
    }

    public static async Task TargetMutationIsTerminalAndEmitsFailureEvidenceAsync()
    {
        var source = new CountingFullCatalogSource(mutatePassB: true);
        var root = NewRoot();
        try
        {
            var result = await new CatalogQualificationService(source, FixtureCatalog.Policy(), new AppendOnlyRunStore(root)).QualifyAsync();
            Assert(!result.IsQualified && result.Snapshot is null && result.Result.Reason == "TARGET_STATE_CHANGED_DURING_QUALIFICATION" && result.RetryCount == 0, "Target mutation was not a terminal zero-retry result.");
            Assert(source.FullReadCount == 2, "Target mutation caused a hidden Pass C.");
            var durable = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Select(File.ReadAllText).ToArray();
            Assert(durable.Any(text => text.Contains("blocked-terminal", StringComparison.Ordinal)) && durable.All(text => !text.Contains(FixtureCatalog.RawLookupCanary, StringComparison.Ordinal)), "Safe terminal failure evidence is missing or leaked raw values.");
        }
        finally { DeleteRoot(root); }
    }

    public static async Task SecondPassBlockerStopsWithoutRetryAsync()
    {
        var source = new CountingFullCatalogSource(blockPassB: true);
        var result = await new CatalogQualificationService(source, FixtureCatalog.Policy()).QualifyAsync();
        Assert(!result.IsQualified && result.Result.Reason == "LOOKUP_PAGE_OFFSET_UNQUALIFIED" && result.Snapshot is null, "Pass B paging blocker was not preserved.");
        Assert(source.FullReadCount == 2 && result.RetryCount == 0, "Pass B blocker caused a retry or Pass C.");
    }

    public static async Task WorkbookScaleDecisionBlocksSnapshotAsync()
    {
        var source = new CountingFullCatalogSource(declaredWorkbookLimit: 1);
        var result = await new CatalogQualificationService(source, FixtureCatalog.Policy()).QualifyAsync();
        Assert(!result.IsQualified && result.Snapshot is null && result.Result.Reason == "WORKBOOK_SCALE_DECISION_REQUIRED" && result.RetryCount == 0, "Over-limit snapshot was not blocked before S05 materialization.");
        Assert(source.FullReadCount == 2, "Scale decision caused an automatic rerun.");
    }

    public static async Task SealedCollectionContractTamperingIsTerminalAsync()
    {
        foreach (var mutation in new[] { ContractMutation.OrderKey, ContractMutation.QueryContract, ContractMutation.Limits })
        {
            var source = new CountingFullCatalogSource(contractMutation: mutation);
            var result = await new CatalogQualificationService(source, FixtureCatalog.Policy()).QualifyAsync();
            Assert(!result.IsQualified && result.Result.Reason == "SEALED_SCOPE_CONTRACT_UNQUALIFIED" && result.RetryCount == 0 && source.FullReadCount == 2, $"{mutation} was not attested against the Pass-A seal.");
        }
    }

    private static string NewRoot() => Path.Combine(Path.GetTempPath(), "BpmSoftSync-S04-App-" + Guid.NewGuid().ToString("N"));
    private static void DeleteRoot(string root) { if (Directory.Exists(root)) Directory.Delete(root, true); }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}

internal enum ContractMutation { None, OrderKey, QueryContract, Limits }

internal sealed class CountingFullCatalogSource(bool cachePassAForB = false, bool mutatePassB = false, bool blockPassB = false, int? declaredWorkbookLimit = null, bool reusePassASchemasForB = false, bool deepClonePassACacheForB = false, ContractMutation contractMutation = ContractMutation.None) : IFullCatalogSource
{
    private FullCatalogRead? _first;
    public int FullReadCount { get; private set; }
    public int WorkspaceReadCount { get; private set; }
    public int SchemaReadCount { get; private set; }
    public int LookupReadCount { get; private set; }
    public List<CatalogReadRequest> Requests { get; } = [];

    public ValueTask<FullCatalogRead> ReadFullAsync(CatalogReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        FullReadCount++;
        if (cachePassAForB && _first is not null) return ValueTask.FromResult(_first);
        if (deepClonePassACacheForB && _first is not null)
        {
            var clone = FixtureCatalog.Read(request, false, false, declaredWorkbookLimit);
            return ValueTask.FromResult(clone with { ReadAttestation = _first.ReadAttestation });
        }
        WorkspaceReadCount++;
        SchemaReadCount++;
        LookupReadCount++;
        var result = FixtureCatalog.Read(request, mutatePassB && request.Pass == CatalogPassOrdinal.B, blockPassB && request.Pass == CatalogPassOrdinal.B, declaredWorkbookLimit);
        if (reusePassASchemasForB && _first is not null) result = result with { Workspace = result.Workspace with { Schemas = _first.Workspace.Schemas } };
        if (request.Pass == CatalogPassOrdinal.B && contractMutation != ContractMutation.None)
        {
            var contracts = result.AppliedCollectionContracts.ToArray();
            var lookup = contracts[^1];
            contracts[^1] = contractMutation switch
            {
                ContractMutation.OrderKey => lookup with { OrderKeyId = "Name" },
                ContractMutation.QueryContract => lookup with { QueryContractId = "SelectQuery/changed/v1" },
                ContractMutation.Limits => lookup with { Limits = lookup.Limits with { MaxRows = lookup.Limits.MaxRows - 1 } },
                _ => lookup
            };
            result = result with { AppliedCollectionContracts = contracts };
        }
        _first ??= result;
        return ValueTask.FromResult(result);
    }
}

internal static class FixtureCatalog
{
    public const string RawLookupCanary = "S04_LOOKUP_VALUE_CANARY_6f58a715";
    private static readonly Guid PackageId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid PackageUId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid WorkspaceId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid SchemaUId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid IdColumnUId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid NameColumnUId = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid IndexUId = Guid.Parse("50000000-0000-0000-0000-000000000001");
    private static readonly Guid RegistryId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid RecordId = Guid.Parse("70000000-0000-0000-0000-000000000001");

    public static CatalogScopePolicy Policy() => CatalogScopePolicy.Create(
        "fixture",
        "sha256:fixture-origin-policy",
        "ReadEndpointAllowlist/v1",
        new CatalogCollectionScope("workspace", "workspaceItemUId", "GetWorkspaceItems/v1", new CatalogReadLimits(64, 10_000, 2L * 1024 * 1024 * 1024)),
        new CatalogCollectionScope("schemas", "schemaUId/packageLayer", "GetSchema/v1", new CatalogReadLimits(1, 10_000, 2L * 1024 * 1024 * 1024)),
        new CatalogCollectionScope("lookup-registry", "Id", "SelectQuery/lookup-registry/v1", new CatalogReadLimits(500, 5_000_000, 2L * 1024 * 1024 * 1024)),
        new CatalogCollectionTemplate("Id", "SelectQuery/lookup-values/v1", new CatalogReadLimits(500, 5_000_000, 2L * 1024 * 1024 * 1024)),
        QualifiedCatalogSnapshot.SchemaVersion,
        "WorkbookProjection/v1");

    public static FullCatalogRead Read(CatalogReadRequest request, bool mutate, bool blocked, int? declaredWorkbookLimit)
    {
        var layer = new PackageLayerIdentity(PackageId, PackageUId, "Current", "FixturePackage");
        var schemaIdentity = new SchemaIdentity("FixtureLookup", SchemaUId, null, null, null, layer);
        var inventoryItem = new WorkspaceInventoryItem(new WorkspaceItemIdentity(WorkspaceId, layer, "EntitySchema", SchemaUId), SupportStatus.Structured, "STRUCTURED", null, "FixtureLookup", []);
        var inventory = WorkspaceInventory.Create([inventoryItem]);
        var columns = new EntityColumnModel[]
        {
            new("Id", IdColumnUId, 0, ColumnOwnership.Own, 0, 1, true, null, []),
            new("Name", NameColumnUId, 1, ColumnOwnership.Own, 1, 1, false, null, [])
        };
        var schema = new EntitySchemaModel(schemaIdentity, columns, [new EntityIndexModel(IndexUId, "PK_FixtureLookup", true, true, [new IndexMember(IdColumnUId, 0, [])], [])], []);
        var model = new WorkspaceObjectModel(true, inventory, [schema], null);
        var registry = new LookupRegistryRecord(RegistryId, SchemaUId, schemaIdentity, null, "registry-source");
        var raw = mutate ? RawLookupCanary + "-changed" : RawLookupCanary;
        var values = new NormalizedLookupValue[]
        {
            new(RecordId, IdColumnUId, "Id", LookupValueState.Value, LookupValueKind.Guid, new LookupGuidValue(RecordId), RecordId.ToString("D"), null, "id-value-fingerprint", "id-source"),
            new(RecordId, NameColumnUId, "Name", LookupValueState.Value, LookupValueKind.Text, new LookupTextValue(raw), raw, null, mutate ? "name-value-fingerprint-changed" : "name-value-fingerprint", "name-source")
        };
        var row = new LookupRow(RecordId, values, mutate ? "row-fingerprint-changed" : "row-fingerprint", "row-source");
        var page = PageManifest.Create(0, "0", [RecordId.ToString("D")], 512);
        var telemetry = new OrderedCollectionTelemetry(1, 1, 512, "0-16KiB", TimeSpan.FromMilliseconds(10), PageManifest.Digest(RecordId.ToString("D")));
        var manifest = new OrderedCollectionManifest("lookup:FixtureLookup", "Id", [page], telemetry, mutate ? "collection-fingerprint-changed" : "collection-fingerprint");
        var collection = new LookupCollection(registry, schema, [row], manifest, manifest.SourceFingerprint);
        var registryManifest = new OrderedCollectionManifest("lookup-registry", "Id", [PageManifest.Create(0, "0", [RegistryId.ToString("D")], 256)], new OrderedCollectionTelemetry(1, 1, 256, "0-16KiB", TimeSpan.FromMilliseconds(5), PageManifest.Digest(RegistryId.ToString("D"))), "registry-manifest-source");
        var lookups = new LookupCatalog(true, [registry], registryManifest, [collection], null);
        var blocker = blocked ? new Blocker(BlockerCode.CatalogOrderOrPagingUnqualified, "lookup:FixtureLookup", "LOOKUP_PAGE_OFFSET_UNQUALIFIED", "Inspect the safe fixture.", "Stop without retry.") : null;
        var appliedContracts = request.SealedScope?.Collections.ToArray() ?? request.Policy.CreatePassAContracts([manifest.CollectionId]);
        var responseSizeBuckets = new Dictionary<string, string>(StringComparer.Ordinal) { ["workspace"] = "0-16KiB", ["schemas"] = "0-16KiB", ["lookup-registry"] = "0-16KiB", ["lookup:FixtureLookup"] = "0-16KiB" };
        var attestation = new CatalogReadAttestation(Guid.NewGuid(), [Guid.NewGuid()], Guid.NewGuid(), new Dictionary<string, Guid> { ["lookup:FixtureLookup"] = Guid.NewGuid() });
        return new FullCatalogRead(request.IndependentReadId, "fake:s04-full-catalog-v1", model, lookups, ["fixture-v1"], appliedContracts, attestation, responseSizeBuckets, TimeSpan.FromMilliseconds(20), declaredWorkbookLimit ?? 1_048_576, blocker);
    }
}
