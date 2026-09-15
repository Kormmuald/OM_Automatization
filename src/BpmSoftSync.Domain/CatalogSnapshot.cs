using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BpmSoftSync.Domain;

public enum CatalogPassOrdinal { A = 1, B = 2 }

public sealed record CatalogReadLimits(int PageSize, int MaxRows, long MaxResponseBytes, int MaxPages = 10_000)
{
    public bool IsValid => PageSize > 0 && MaxRows > 0 && MaxResponseBytes > 0 && MaxPages > 0;
}

public sealed record CatalogCollectionScope(string CollectionId, string OrderKeyId, string QueryContractId, CatalogReadLimits Limits);
public sealed record CatalogCollectionTemplate(string OrderKeyId, string QueryContractId, CatalogReadLimits Limits);

public sealed class CatalogScopePolicy
{
    private CatalogScopePolicy(string targetAlias, string targetOriginPolicyDigest, string allowlistVersion, CatalogCollectionScope workspace, CatalogCollectionScope schemas, CatalogCollectionScope lookupRegistry, CatalogCollectionTemplate lookupTemplate, string snapshotVersion, string workbookProjectionVersion)
    {
        TargetAlias = targetAlias;
        TargetOriginPolicyDigest = targetOriginPolicyDigest;
        AllowlistVersion = allowlistVersion;
        Workspace = workspace;
        Schemas = schemas;
        LookupRegistry = lookupRegistry;
        LookupTemplate = lookupTemplate;
        SnapshotVersion = snapshotVersion;
        WorkbookProjectionVersion = workbookProjectionVersion;
        _ = ScopeDescriptor.Create(targetAlias, targetOriginPolicyDigest, allowlistVersion, [workspace, schemas, lookupRegistry], snapshotVersion, workbookProjectionVersion);
        if (!lookupTemplate.Limits.IsValid || string.IsNullOrWhiteSpace(lookupTemplate.OrderKeyId) || string.IsNullOrWhiteSpace(lookupTemplate.QueryContractId)) throw new ArgumentException("Lookup collection template is invalid.", nameof(lookupTemplate));
    }

    public string TargetAlias { get; }
    public string TargetOriginPolicyDigest { get; }
    public string AllowlistVersion { get; }
    public CatalogCollectionScope Workspace { get; }
    public CatalogCollectionScope Schemas { get; }
    public CatalogCollectionScope LookupRegistry { get; }
    public CatalogCollectionTemplate LookupTemplate { get; }
    public string SnapshotVersion { get; }
    public string WorkbookProjectionVersion { get; }

    public static CatalogScopePolicy Create(string targetAlias, string targetOriginPolicyDigest, string allowlistVersion, CatalogCollectionScope workspace, CatalogCollectionScope schemas, CatalogCollectionScope lookupRegistry, CatalogCollectionTemplate lookupTemplate, string snapshotVersion, string workbookProjectionVersion) =>
        new(targetAlias, targetOriginPolicyDigest, allowlistVersion, workspace, schemas, lookupRegistry, lookupTemplate, snapshotVersion, workbookProjectionVersion);

    public IReadOnlyList<CatalogCollectionScope> CreatePassAContracts(IReadOnlyList<string> discoveredLookupCollectionIds) =>
        new[] { Workspace, Schemas, LookupRegistry }.Concat(discoveredLookupCollectionIds.Select(id => new CatalogCollectionScope(id, LookupTemplate.OrderKeyId, LookupTemplate.QueryContractId, LookupTemplate.Limits))).ToArray();

    public bool TrySealFromPassA(FullCatalogRead read, out ScopeDescriptor? scope, out Blocker? blocker)
    {
        var discovered = new List<string>();
        foreach (var registryRecord in read.Lookups.Registry)
        {
            var matches = read.Lookups.Collections.Where(collection =>
                collection.RegistryRecord.LookupRecordId == registryRecord.LookupRecordId &&
                collection.RegistryRecord.SysEntitySchemaUId == registryRecord.SysEntitySchemaUId).ToArray();
            if (matches.Length != 1)
            {
                scope = null;
                blocker = ContractBlocker("PASS_A_SCOPE_DISCOVERY_UNQUALIFIED");
                return false;
            }
            discovered.Add(matches[0].Manifest.CollectionId);
        }
        var expected = CreatePassAContracts(discovered);
        if (read.Lookups.Registry.Count != read.Lookups.Collections.Count || discovered.Any(string.IsNullOrWhiteSpace) || discovered.Distinct(StringComparer.Ordinal).Count() != discovered.Count || !expected.SequenceEqual(read.AppliedCollectionContracts))
        {
            scope = null;
            blocker = ContractBlocker("PASS_A_SCOPE_DISCOVERY_UNQUALIFIED");
            return false;
        }
        scope = ScopeDescriptor.Create(TargetAlias, TargetOriginPolicyDigest, AllowlistVersion, expected, SnapshotVersion, WorkbookProjectionVersion);
        blocker = null;
        return true;
    }

    private static Blocker ContractBlocker(string reason) => new(BlockerCode.FullCatalogNotQualified, "catalog-scope", reason, "Read the complete registry again in a new manually authorized run.", "Seal this run; do not retry or run Pass C.");
}

public sealed class ScopeDescriptor
{
    public const string SchemaVersion = "ScopeDescriptor/v1";
    private readonly ReadOnlyCollection<CatalogCollectionScope> _collections;

    private ScopeDescriptor(string targetAlias, string targetOriginPolicyDigest, string allowlistVersion, IReadOnlyList<CatalogCollectionScope> collections, string snapshotVersion, string workbookProjectionVersion)
    {
        Schema = SchemaVersion;
        TargetAlias = Require(targetAlias, nameof(targetAlias));
        TargetOriginPolicyDigest = Require(targetOriginPolicyDigest, nameof(targetOriginPolicyDigest));
        AllowlistVersion = Require(allowlistVersion, nameof(allowlistVersion));
        SnapshotVersion = Require(snapshotVersion, nameof(snapshotVersion));
        WorkbookProjectionVersion = Require(workbookProjectionVersion, nameof(workbookProjectionVersion));
        if (collections.Count == 0 || collections.Any(item => string.IsNullOrWhiteSpace(item.CollectionId) || string.IsNullOrWhiteSpace(item.OrderKeyId) || string.IsNullOrWhiteSpace(item.QueryContractId) || !item.Limits.IsValid) || collections.Select(item => item.CollectionId).Distinct(StringComparer.Ordinal).Count() != collections.Count)
            throw new ArgumentException("A sealed scope requires unique, valid collection contracts.", nameof(collections));
        if (!string.Equals(SnapshotVersion, QualifiedCatalogSnapshot.SchemaVersion, StringComparison.Ordinal))
            throw new ArgumentException("The sealed scope must request QualifiedCatalogSnapshot/v1.", nameof(snapshotVersion));
        _collections = Array.AsReadOnly(collections.ToArray());
        Digest = CatalogCanonical.Digest(new
        {
            schema = Schema,
            targetAlias = TargetAlias,
            targetOriginPolicyDigest = TargetOriginPolicyDigest,
            allowlistVersion = AllowlistVersion,
            collections = _collections.Select(item => new { item.CollectionId, item.OrderKeyId, item.QueryContractId, item.Limits.PageSize, item.Limits.MaxPages, item.Limits.MaxRows, item.Limits.MaxResponseBytes }).ToArray(),
            snapshotVersion = SnapshotVersion,
            workbookProjectionVersion = WorkbookProjectionVersion
        });
    }

    public string Schema { get; }
    public string TargetAlias { get; }
    public string TargetOriginPolicyDigest { get; }
    public string AllowlistVersion { get; }
    public IReadOnlyList<CatalogCollectionScope> Collections => _collections;
    public string SnapshotVersion { get; }
    public string WorkbookProjectionVersion { get; }
    public string Digest { get; }
    public bool IsSealed => true;

    public static ScopeDescriptor Create(string targetAlias, string targetOriginPolicyDigest, string allowlistVersion, IReadOnlyList<CatalogCollectionScope> collections, string snapshotVersion, string workbookProjectionVersion) =>
        new(targetAlias, targetOriginPolicyDigest, allowlistVersion, collections, snapshotVersion, workbookProjectionVersion);

    private static string Require(string value, string parameterName) => !string.IsNullOrWhiteSpace(value) ? value.Normalize(NormalizationForm.FormC) : throw new ArgumentException("Value is required.", parameterName);
}

public sealed record CatalogReadRequest(CatalogPassOrdinal Pass, CatalogScopePolicy Policy, ScopeDescriptor? SealedScope, Guid IndependentReadId);
public sealed record CatalogReadAttestation(Guid WorkspaceReadId, IReadOnlyList<Guid> SchemaReadIds, Guid LookupRegistryReadId, IReadOnlyDictionary<string, Guid> LookupCollectionReadIds);
public sealed record FullCatalogRead(Guid IndependentReadId, string SourceIdentity, WorkspaceObjectModel Workspace, LookupCatalog Lookups, IReadOnlyList<string> ObservedTargetVersionEvidence, IReadOnlyList<CatalogCollectionScope> AppliedCollectionContracts, CatalogReadAttestation ReadAttestation, IReadOnlyDictionary<string, string> ResponseSizeBuckets, TimeSpan Duration, int? DeclaredWorkbookRowLimit, Blocker? Blocker = null);
public sealed record CatalogPassContent(WorkspaceObjectModel Workspace, LookupCatalog Lookups);
public sealed record CatalogComponentDigest(string StableIdentity, string ComponentKind, string Digest);
public sealed record ScopedUnsupportedDiagnostic(string StableIdentity, SupportStatus SupportStatus, string ShapeDigest, string SafeReason);

public sealed record CatalogPass(
    CatalogPassOrdinal Ordinal,
    Guid IndependentReadId,
    string SourceIdentity,
    ScopeDescriptor Scope,
    CatalogPassContent Content,
    IReadOnlyList<string> ObservedTargetVersionEvidence,
    IReadOnlyList<string> OrderedIdentities,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<PageManifest> PageManifests,
    IReadOnlyList<CatalogComponentDigest> ComponentDigests,
    IReadOnlyList<ScopedUnsupportedDiagnostic> Unsupported,
    TargetFingerprint Fingerprint,
    string OrderedIdentityDigest,
    string PageManifestDigest,
    string ComponentDigest,
    string UnsupportedDigest,
    string ReconciliationDigest,
    IReadOnlyList<CatalogCollectionScope> AppliedCollectionContracts,
    string AppliedCollectionContractsDigest,
    string ReadAttestationDigest,
    IReadOnlyDictionary<string, string> ResponseSizeBuckets,
    TimeSpan Duration,
    int? DeclaredWorkbookRowLimit);

public sealed record QualifiedCatalogSnapshot(
    string Schema,
    Guid RunId,
    Guid PairId,
    ScopeDescriptor Scope,
    string SourceIdentity,
    WorkspaceObjectModel Workspace,
    LookupCatalog Lookups,
    string PassADigest,
    string PassBDigest,
    TargetFingerprint TargetFingerprint,
    IReadOnlyList<string> OrderedIdentities,
    IReadOnlyList<CatalogComponentDigest> ComponentDigests,
    IReadOnlyList<ScopedUnsupportedDiagnostic> Unsupported,
    IReadOnlyDictionary<string, int> Counts,
    WorkbookScaleForecast Scale,
    DateTimeOffset PullStartedUtc,
    DateTimeOffset PullCompletedUtc)
{
    public const string SchemaVersion = "QualifiedCatalogSnapshot/v1";
}

public static class CatalogPassBuilder
{
    public static bool IsCurrentContentBindingValid(CatalogPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        try
        {
            var binding = CurrentBinding(pass);
            return pass.OrderedIdentities.SequenceEqual(binding.OrderedIdentities, StringComparer.Ordinal) &&
                   DictionaryEqual(pass.Counts, binding.Counts) &&
                   pass.PageManifests.SequenceEqual(binding.PageManifests) &&
                   pass.ComponentDigests.SequenceEqual(binding.ComponentDigests) &&
                   pass.Unsupported.SequenceEqual(binding.Unsupported) &&
                   string.Equals(pass.OrderedIdentityDigest, binding.OrderedIdentityDigest, StringComparison.Ordinal) &&
                   string.Equals(pass.PageManifestDigest, binding.PageManifestDigest, StringComparison.Ordinal) &&
                   string.Equals(pass.ComponentDigest, binding.ComponentDigest, StringComparison.Ordinal) &&
                   string.Equals(pass.UnsupportedDigest, binding.UnsupportedDigest, StringComparison.Ordinal) &&
                   string.Equals(pass.Fingerprint.Digest, binding.Fingerprint.Digest, StringComparison.Ordinal) &&
                   string.Equals(pass.ReconciliationDigest, binding.ReconciliationDigest, StringComparison.Ordinal);
        }
        catch (Exception error) when (error is InvalidDataException or InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }

    public static string CurrentContentBindingDigest(CatalogPass pass)
    {
        ArgumentNullException.ThrowIfNull(pass);
        var binding = CurrentBinding(pass);
        return CatalogCanonical.Digest(new
        {
            orderedIdentities = binding.OrderedIdentityDigest,
            pageManifests = binding.PageManifestDigest,
            components = binding.ComponentDigest,
            unsupported = binding.UnsupportedDigest,
            counts = CatalogCanonical.Digest(binding.Counts.OrderBy(item => item.Key, StringComparer.Ordinal).ToArray()),
            targetFingerprint = binding.Fingerprint.Digest,
            reconciliation = binding.ReconciliationDigest
        });
    }

    public static bool TryBuild(CatalogReadRequest request, FullCatalogRead read, out CatalogPass? pass, out Blocker? blocker)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(read);
        pass = null;
        if (request.SealedScope is null || request.IndependentReadId == Guid.Empty || read.IndependentReadId != request.IndependentReadId || string.IsNullOrWhiteSpace(read.SourceIdentity) || read.Duration < TimeSpan.Zero || read.ObservedTargetVersionEvidence.Count == 0 || read.ObservedTargetVersionEvidence.Any(string.IsNullOrWhiteSpace) || !ValidAttestation(read))
        {
            blocker = Blocked("PASS_READ_IDENTITY_UNQUALIFIED");
            return false;
        }
        if (read.Blocker is not null || !read.Workspace.IsQualified || read.Workspace.Blocker is not null || !read.Lookups.IsQualified || read.Lookups.Blocker is not null || read.Lookups.RegistryManifest is null)
        {
            blocker = read.Blocker ?? read.Workspace.Blocker ?? read.Lookups.Blocker ?? Blocked("FULL_CATALOG_READ_UNQUALIFIED");
            return false;
        }
        var expectedCollections = request.SealedScope.Collections.Select(item => item.CollectionId).ToArray();
        var actualCollections = new[] { "workspace", "schemas", read.Lookups.RegistryManifest.CollectionId }.Concat(read.Lookups.Collections.Select(item => item.Manifest.CollectionId)).ToArray();
        if (!expectedCollections.SequenceEqual(actualCollections, StringComparer.Ordinal) || !request.SealedScope.Collections.SequenceEqual(read.AppliedCollectionContracts) || read.ResponseSizeBuckets.Count != expectedCollections.Length || expectedCollections.Any(collection => !read.ResponseSizeBuckets.TryGetValue(collection, out var bucket) || string.IsNullOrWhiteSpace(bucket)))
        {
            blocker = Blocked("SEALED_SCOPE_CONTRACT_UNQUALIFIED");
            return false;
        }

        if (!PrimaryPackageIdentitiesAreUnambiguous(read.Workspace))
        {
            blocker = Blocked("PACKAGE_PRIMARY_IDENTITY_UNQUALIFIED");
            return false;
        }

        var orderedIdentities = OrderedIdentities(read.Workspace, read.Lookups);
        var counts = Counts(read.Workspace, read.Lookups);
        var manifests = PageManifests(read.Lookups);
        var components = Components(read.Workspace, read.Lookups);
        var unsupported = Unsupported(read.Workspace);
        var orderedDigest = CatalogCanonical.Digest(orderedIdentities);
        var manifestDigest = CatalogCanonical.Digest(manifests);
        var componentDigest = CatalogCanonical.Digest(components);
        var unsupportedDigest = CatalogCanonical.Digest(unsupported);
        var schemaEntries = read.Workspace.Schemas.Select(schema => new FingerprintSchemaEntry(schema.Identity.SchemaUId, schema.Identity.PackageLayer.PackageUId!.Value, components.Single(item => item.StableIdentity == $"schema:{Id(schema.Identity.SchemaUId)}:layer:{Layer(schema.Identity.PackageLayer)}").Digest)).ToArray();
        var collectionEntries = CollectionFingerprints(read.Lookups);
        var unsupportedEntries = unsupported.Select(item => new FingerprintUnsupportedEntry(item.StableIdentity, item.SupportStatus.ToString(), item.ShapeDigest, item.SupportStatus)).ToArray();
        var workspaceEntries = read.Workspace.Inventory.Items.Select(item => new FingerprintWorkspaceEntry(item.Identity.WorkspaceItemUId, item.Identity.PackageLayer.PackageUId ?? Guid.Empty, item.Identity.ItemType, item.SupportStatus)).ToArray();
        var versions = read.ObservedTargetVersionEvidence.Select(Normalize).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var sortedResponseSizeBuckets = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in read.ResponseSizeBuckets) sortedResponseSizeBuckets.Add(item.Key, Normalize(item.Value));
        var responseSizeBuckets = new ReadOnlyDictionary<string, string>(sortedResponseSizeBuckets);
        var appliedContracts = read.AppliedCollectionContracts.ToArray();
        var appliedContractsDigest = CatalogCanonical.Digest(appliedContracts);
        var readAttestationDigest = CatalogCanonical.Digest(read.ReadAttestation);
        var fingerprint = TargetFingerprint.Create(new TargetFingerprintInput(request.SealedScope.AllowlistVersion, request.SealedScope.Digest, versions, workspaceEntries, schemaEntries, collectionEntries, unsupportedEntries));
        var sourceIdentity = Normalize(read.SourceIdentity);
        var reconciliationDigest = CatalogCanonical.Digest(new { sourceIdentity, scope = request.SealedScope.Digest, appliedContractsDigest, versions, counts = counts.OrderBy(item => item.Key, StringComparer.Ordinal).ToArray(), responseSizeBuckets, orderedDigest, manifestDigest, componentDigest, unsupportedDigest, fingerprint = fingerprint.Digest });
        pass = new CatalogPass(request.Pass, read.IndependentReadId, sourceIdentity, request.SealedScope, new CatalogPassContent(read.Workspace, read.Lookups), versions, orderedIdentities, counts, manifests, components, unsupported, fingerprint, orderedDigest, manifestDigest, componentDigest, unsupportedDigest, reconciliationDigest, appliedContracts, appliedContractsDigest, readAttestationDigest, responseSizeBuckets, read.Duration, read.DeclaredWorkbookRowLimit);
        blocker = null;
        return true;
    }

    private static IReadOnlyList<string> OrderedIdentities(WorkspaceObjectModel workspace, LookupCatalog lookups)
    {
        var result = new List<string>();
        foreach (var item in workspace.Inventory.Items) result.Add($"workspace:{Id(item.Identity.WorkspaceItemUId)}:layer:{Layer(item.Identity.PackageLayer)}:{Normalize(item.Identity.ItemType)}");
        foreach (var schema in workspace.Schemas)
        {
            result.Add($"schema:{Id(schema.Identity.SchemaUId)}:layer:{Layer(schema.Identity.PackageLayer)}");
            foreach (var column in schema.Columns.OrderBy(item => item.Ordinal)) result.Add($"column:{Id(schema.Identity.SchemaUId)}:{column.Ordinal}:{Id(column.ColumnUId)}");
            foreach (var index in schema.Indexes) { result.Add($"index:{Id(schema.Identity.SchemaUId)}:{Id(index.IndexUId)}"); foreach (var member in index.Members.OrderBy(item => item.Ordinal)) result.Add($"index-member:{Id(index.IndexUId)}:{member.Ordinal}:{Id(member.ColumnUId)}"); }
        }
        foreach (var registry in lookups.Registry) result.Add($"lookup-registry:{Id(registry.LookupRecordId)}:{Id(registry.SysEntitySchemaUId)}:layer:{Layer(registry.SchemaIdentity.PackageLayer)}");
        foreach (var collection in lookups.Collections)
        {
            result.Add($"lookup-collection:{Normalize(collection.Manifest.CollectionId)}:{Id(collection.RegistryRecord.SysEntitySchemaUId)}");
            foreach (var row in collection.Rows) { result.Add($"lookup-row:{Normalize(collection.Manifest.CollectionId)}:{Id(row.RecordId)}"); foreach (var value in row.Values) result.Add($"lookup-value:{Id(row.RecordId)}:{Id(value.ColumnUId)}"); }
        }
        return result;
    }

    private static IReadOnlyDictionary<string, int> Counts(WorkspaceObjectModel workspace, LookupCatalog lookups) => new ReadOnlyDictionary<string, int>(new SortedDictionary<string, int>(StringComparer.Ordinal)
    {
        ["columns"] = workspace.Schemas.Sum(schema => schema.Columns.Count),
        ["indexMembers"] = workspace.Schemas.Sum(schema => schema.Indexes.Sum(index => index.Members.Count)),
        ["indexes"] = workspace.Schemas.Sum(schema => schema.Indexes.Count),
        ["lookupCollections"] = lookups.Collections.Count,
        ["lookupRegistry"] = lookups.Registry.Count,
        ["lookupRows"] = lookups.Collections.Sum(collection => collection.Rows.Count),
        ["lookupValues"] = lookups.Collections.Sum(collection => collection.Values.Count),
        ["pages"] = (lookups.RegistryManifest?.Pages.Count ?? 0) + lookups.Collections.Sum(collection => collection.Manifest.Pages.Count),
        ["schemas"] = workspace.Schemas.Count,
        ["workspaceItems"] = workspace.Inventory.Items.Count
    });

    private static IReadOnlyList<PageManifest> PageManifests(LookupCatalog lookups) => (lookups.RegistryManifest is null ? Enumerable.Empty<PageManifest>() : lookups.RegistryManifest.Pages).Concat(lookups.Collections.SelectMany(collection => collection.Manifest.Pages)).ToArray();

    private static IReadOnlyList<CatalogComponentDigest> Components(WorkspaceObjectModel workspace, LookupCatalog lookups)
    {
        var result = new List<CatalogComponentDigest>();
        foreach (var item in workspace.Inventory.Items) result.Add(Component($"workspace:{Id(item.Identity.WorkspaceItemUId)}:layer:{Layer(item.Identity.PackageLayer)}", "workspace", WorkspaceComponent(item)));
        foreach (var schema in workspace.Schemas) result.Add(Component($"schema:{Id(schema.Identity.SchemaUId)}:layer:{Layer(schema.Identity.PackageLayer)}", "schema", SchemaComponent(schema)));
        foreach (var registry in lookups.Registry) result.Add(Component($"lookup-registry:{Id(registry.LookupRecordId)}", "lookup-registry", registry));
        if (lookups.RegistryManifest is not null) result.Add(Component("lookup-registry-manifest", "lookup-registry-manifest", ManifestContent(lookups.RegistryManifest)));
        foreach (var collection in lookups.Collections)
        {
            result.Add(Component($"lookup-collection:{Normalize(collection.Manifest.CollectionId)}", "lookup-collection", new { manifest = ManifestContent(collection.Manifest), collectionSourceFingerprint = collection.SourceFingerprint }));
            foreach (var row in collection.Rows) result.Add(Component($"lookup-row:{Normalize(collection.Manifest.CollectionId)}:{Id(row.RecordId)}", "lookup-row", new { row.RecordId, values = row.Values.Select(value => new { value.RecordId, value.ColumnUId, value.ColumnName, value.State, value.ValueKind, typed = Typed(value.TypedValue), value.CanonicalValue, value.ReferenceRecordId, value.ValueFingerprint, value.SourceFingerprint }).ToArray(), row.RowFingerprint, row.SourceFingerprint }));
        }
        return result.OrderBy(item => item.StableIdentity, StringComparer.Ordinal).ToArray();
    }

    private static IReadOnlyList<ScopedUnsupportedDiagnostic> Unsupported(WorkspaceObjectModel workspace) => workspace.Inventory.Items.Where(item => item.SupportStatus != SupportStatus.Structured || item.Envelope is not null).Select(item => new ScopedUnsupportedDiagnostic($"workspace:{Id(item.Identity.WorkspaceItemUId)}:layer:{Layer(item.Identity.PackageLayer)}", item.SupportStatus, item.Envelope?.SourceShapeDigest ?? CatalogCanonical.Digest(item.UnknownProperties ?? []), item.SafeReason)).OrderBy(item => item.StableIdentity, StringComparer.Ordinal).ToArray();

    private static IReadOnlyList<FingerprintCollectionEntry> CollectionFingerprints(LookupCatalog lookups)
    {
        var result = new List<FingerprintCollectionEntry>();
        if (lookups.RegistryManifest is not null) result.Add(ToFingerprint(lookups.RegistryManifest));
        result.AddRange(lookups.Collections.Select(collection => ToFingerprint(collection.Manifest)));
        return result;
    }

    private static FingerprintCollectionEntry ToFingerprint(OrderedCollectionManifest manifest) => new(manifest.CollectionId, manifest.OrderKeyId, manifest.Telemetry.RowCount, manifest.Telemetry.OrderedIdentityDigest, CatalogCanonical.Digest(manifest.Pages));
    private static CurrentCatalogBinding CurrentBinding(CatalogPass pass)
    {
        var orderedIdentities = OrderedIdentities(pass.Content.Workspace, pass.Content.Lookups);
        var counts = Counts(pass.Content.Workspace, pass.Content.Lookups);
        var pageManifests = PageManifests(pass.Content.Lookups);
        var components = Components(pass.Content.Workspace, pass.Content.Lookups);
        var unsupported = Unsupported(pass.Content.Workspace);
        var orderedIdentityDigest = CatalogCanonical.Digest(orderedIdentities);
        var pageManifestDigest = CatalogCanonical.Digest(pageManifests);
        var componentDigest = CatalogCanonical.Digest(components);
        var unsupportedDigest = CatalogCanonical.Digest(unsupported);
        var schemaEntries = pass.Content.Workspace.Schemas.Select(schema => new FingerprintSchemaEntry(
            schema.Identity.SchemaUId,
            schema.Identity.PackageLayer.PackageUId ?? Guid.Empty,
            components.Single(item => item.StableIdentity == $"schema:{Id(schema.Identity.SchemaUId)}:layer:{Layer(schema.Identity.PackageLayer)}").Digest)).ToArray();
        var workspaceEntries = pass.Content.Workspace.Inventory.Items.Select(item => new FingerprintWorkspaceEntry(
            item.Identity.WorkspaceItemUId,
            item.Identity.PackageLayer.PackageUId ?? Guid.Empty,
            item.Identity.ItemType,
            item.SupportStatus)).ToArray();
        var unsupportedEntries = unsupported.Select(item => new FingerprintUnsupportedEntry(item.StableIdentity, item.SupportStatus.ToString(), item.ShapeDigest, item.SupportStatus)).ToArray();
        var versions = pass.ObservedTargetVersionEvidence.Select(Normalize).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var fingerprint = TargetFingerprint.Create(new TargetFingerprintInput(pass.Scope.AllowlistVersion, pass.Scope.Digest, versions, workspaceEntries, schemaEntries, CollectionFingerprints(pass.Content.Lookups), unsupportedEntries));
        var sortedResponseSizeBuckets = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in pass.ResponseSizeBuckets) sortedResponseSizeBuckets.Add(item.Key, Normalize(item.Value));
        var responseSizeBuckets = new ReadOnlyDictionary<string, string>(sortedResponseSizeBuckets);
        var appliedContractsDigest = CatalogCanonical.Digest(pass.AppliedCollectionContracts);
        var sourceIdentity = Normalize(pass.SourceIdentity);
        var reconciliationDigest = CatalogCanonical.Digest(new { sourceIdentity, scope = pass.Scope.Digest, appliedContractsDigest, versions, counts = counts.OrderBy(item => item.Key, StringComparer.Ordinal).ToArray(), responseSizeBuckets, orderedDigest = orderedIdentityDigest, manifestDigest = pageManifestDigest, componentDigest, unsupportedDigest, fingerprint = fingerprint.Digest });
        return new CurrentCatalogBinding(orderedIdentities, counts, pageManifests, components, unsupported, fingerprint, orderedIdentityDigest, pageManifestDigest, componentDigest, unsupportedDigest, reconciliationDigest);
    }

    private static bool DictionaryEqual(IReadOnlyDictionary<string, int> first, IReadOnlyDictionary<string, int> second) =>
        first.Count == second.Count && first.All(item => second.TryGetValue(item.Key, out var value) && value == item.Value);

    private sealed record CurrentCatalogBinding(
        IReadOnlyList<string> OrderedIdentities,
        IReadOnlyDictionary<string, int> Counts,
        IReadOnlyList<PageManifest> PageManifests,
        IReadOnlyList<CatalogComponentDigest> ComponentDigests,
        IReadOnlyList<ScopedUnsupportedDiagnostic> Unsupported,
        TargetFingerprint Fingerprint,
        string OrderedIdentityDigest,
        string PageManifestDigest,
        string ComponentDigest,
        string UnsupportedDigest,
        string ReconciliationDigest);
    private static bool ValidAttestation(FullCatalogRead read)
    {
        var attestation = read.ReadAttestation;
        var lookupIds = read.Lookups.Collections.Select(collection => collection.Manifest.CollectionId).ToArray();
        var allIds = new[] { attestation.WorkspaceReadId, attestation.LookupRegistryReadId }.Concat(attestation.SchemaReadIds).Concat(attestation.LookupCollectionReadIds.Values).ToArray();
        return allIds.All(id => id != Guid.Empty) && allIds.Distinct().Count() == allIds.Length && attestation.SchemaReadIds.Count == read.Workspace.Schemas.Count && attestation.LookupCollectionReadIds.Count == lookupIds.Length && lookupIds.All(attestation.LookupCollectionReadIds.ContainsKey);
    }
    private static object ManifestContent(OrderedCollectionManifest manifest) => new { manifest.CollectionId, manifest.OrderKeyId, manifest.Pages, manifest.Telemetry.PageCount, manifest.Telemetry.RowCount, manifest.Telemetry.ResponseBytes, manifest.Telemetry.ResponseSizeBucket, manifest.Telemetry.OrderedIdentityDigest, manifest.SourceFingerprint };
    private static CatalogComponentDigest Component(string identity, string kind, object value) => new(identity, kind, CatalogCanonical.Digest(value));
    private static object? Typed(LookupTypedValue? value) => value switch
    {
        null => null,
        LookupTextValue typed => new { kind = "text", typed.Value },
        LookupGuidValue typed => new { kind = "guid", value = Id(typed.Value) },
        LookupIntegerValue typed => new { kind = "integer", typed.Value },
        LookupDecimalValue typed => new { kind = "decimal", value = typed.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) },
        LookupBooleanValue typed => new { kind = "boolean", typed.Value },
        LookupDateTimeValue typed => new { kind = "dateTime", value = typed.Value.ToUniversalTime().ToString("O") },
        LookupDateValue typed => new { kind = "date", value = typed.Value.ToString("O") },
        LookupTimeValue typed => new { kind = "time", value = typed.Value.ToString("O") },
        LookupReferenceValue typed => new { kind = "reference", recordId = Id(typed.RecordId), typed.DisplayValue },
        _ => throw new InvalidDataException("LOOKUP_TYPED_VALUE_UNQUALIFIED")
    };

    private static bool PrimaryPackageIdentitiesAreUnambiguous(WorkspaceObjectModel workspace) =>
        workspace.Schemas.All(schema => !string.IsNullOrWhiteSpace(schema.Identity.PackageLayer.OpaquePackageId) && schema.Identity.PackageLayer.PackageUId is not null) &&
        workspace.Schemas.Select(schema => (schema.Identity.SchemaUId, schema.Identity.PackageLayer.PackageUId!.Value)).Distinct().Count() == workspace.Schemas.Count;

    private static object WorkspaceComponent(WorkspaceInventoryItem item) => new
    {
        identity = new
        {
            item.Identity.WorkspaceItemUId,
            item.Identity.ItemType,
            item.Identity.SchemaUId,
            package = PackageComponent(item.Identity.PackageLayer)
        },
        item.SupportStatus,
        item.SafeReason,
        item.Envelope,
        item.DisplayName,
        item.UnknownProperties
    };

    private static object SchemaComponent(EntitySchemaModel schema) => new
    {
        identity = new
        {
            schema.Identity.SchemaName,
            schema.Identity.SchemaUId,
            schema.Identity.ServerSchemaIdCandidate,
            schema.Identity.ParentSchemaName,
            schema.Identity.ParentSchemaUId,
            package = PackageComponent(schema.Identity.PackageLayer)
        },
        schema.Columns,
        schema.Indexes,
        schema.UnknownProperties
    };

    private static object PackageComponent(PackageLayerIdentity layer) => new
    {
        layer.PackageUId,
        layer.LayerKind,
        layer.PackageName,
        opaquePackageIdProvenanceDigest = layer.OpaquePackageIdDigest
    };

    // Stable identities and joins are GUID-based. The opaque source value is
    // intentionally absent here, while its safe digest participates only in
    // component change detection above.
    private static string Layer(PackageLayerIdentity layer) => layer.PrimaryIdentityKey;
    private static string Id(Guid value) => value.ToString("D").ToLowerInvariant();
    private static string Id(Guid? value) => value?.ToString("D").ToLowerInvariant() ?? string.Empty;
    private static string Normalize(string value) => value.Normalize(NormalizationForm.FormC);
    private static Blocker Blocked(string reason) => new(BlockerCode.FullCatalogNotQualified, "catalog-pass", reason, "Start a new manually authorized run with a fresh full reader.", "Seal this run; do not retry or run Pass C.");
}

internal static class CatalogCanonical
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static string Digest(object value)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Options).Normalize(NormalizationForm.FormC));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
