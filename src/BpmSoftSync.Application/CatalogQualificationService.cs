using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

public sealed class CatalogQualificationService : ICatalogQualificationService
{
    private readonly ICatalogSource? _legacySource;
    private readonly IFullCatalogSource? _fullSource;
    private readonly CatalogScopePolicy? _scopePolicy;
    private readonly IRunEvidenceStore? _evidenceStore;
    private readonly Func<Guid> _idFactory;
    private readonly Func<DateTimeOffset> _clock;

    public CatalogQualificationService(ICatalogSource source, IRunEvidenceStore? evidenceStore = null)
    {
        _legacySource = source ?? throw new ArgumentNullException(nameof(source));
        _evidenceStore = evidenceStore;
        _idFactory = Guid.NewGuid;
        _clock = () => DateTimeOffset.UtcNow;
    }

    public CatalogQualificationService(IFullCatalogSource source, CatalogScopePolicy scopePolicy, IRunEvidenceStore? evidenceStore = null, Func<Guid>? idFactory = null, Func<DateTimeOffset>? clock = null)
    {
        _fullSource = source ?? throw new ArgumentNullException(nameof(source));
        _scopePolicy = scopePolicy ?? throw new ArgumentNullException(nameof(scopePolicy));
        _evidenceStore = evidenceStore;
        _idFactory = idFactory ?? Guid.NewGuid;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public ValueTask<CatalogQualification> QualifyAsync(CancellationToken cancellationToken = default) =>
        _fullSource is null ? QualifyLegacyAsync(cancellationToken) : QualifyFullAsync(cancellationToken);

    private async ValueTask<CatalogQualification> QualifyFullAsync(CancellationToken cancellationToken)
    {
        var pullStartedUtc = _clock();
        var policy = _scopePolicy!;
        var runId = _evidenceStore is null ? NonEmptyId() : await _evidenceStore.BeginRunAsync(policy.TargetAlias, cancellationToken);
        var requestA = new CatalogReadRequest(CatalogPassOrdinal.A, policy, null, NonEmptyId());
        var readA = await _fullSource!.ReadFullAsync(requestA, cancellationToken);
        if (!policy.TrySealFromPassA(readA, out var sealedScope, out var scopeBlocker))
            return await PersistFullAsync(CatalogQualification.Terminal(scopeBlocker!, null, null, runId), cancellationToken);
        var sealedRequestA = requestA with { SealedScope = sealedScope };
        if (!CatalogPassBuilder.TryBuild(sealedRequestA, readA, out var passA, out var blockerA))
            return await PersistFullAsync(CatalogQualification.Terminal(blockerA!, null, null, runId), cancellationToken);

        var requestB = new CatalogReadRequest(CatalogPassOrdinal.B, policy, sealedScope, NonEmptyId(requestA.IndependentReadId));
        var readB = await _fullSource.ReadFullAsync(requestB, cancellationToken);
        if (SharesMaterializedData(readA, readB) || readB.IndependentReadId != requestB.IndependentReadId)
        {
            var independence = new Blocker(BlockerCode.FullCatalogNotQualified, "catalog-qualification", "PASS_B_INDEPENDENCE_UNQUALIFIED", "Start a new manually authorized run with fresh independent readers.", "Seal this run; do not retry or run Pass C.");
            return await PersistFullAsync(CatalogQualification.Terminal(independence, passA, null, runId), cancellationToken);
        }
        if (!CatalogPassBuilder.TryBuild(requestB, readB, out var passB, out var blockerB))
            return await PersistFullAsync(CatalogQualification.Terminal(blockerB!, passA, null, runId), cancellationToken);

        var qualification = CatalogQualification.Reconcile(passA!, passB!, runId, NonEmptyId(runId, requestA.IndependentReadId, requestB.IndependentReadId), pullStartedUtc, _clock());
        return await PersistFullAsync(qualification, cancellationToken);
    }

    private async ValueTask<CatalogQualification> PersistFullAsync(CatalogQualification qualification, CancellationToken cancellationToken)
    {
        if (_evidenceStore is null) return qualification;
        foreach (var envelope in EvidenceFor(qualification, _scopePolicy!.TargetAlias))
        {
            var appended = await _evidenceStore.AppendAsync(qualification.RunId, envelope, cancellationToken);
            if (!appended.IsSuccess)
            {
                qualification = qualification with { IsQualified = false, Result = appended, Snapshot = null };
                break;
            }
        }
        // A qualified snapshot is an intermediate gate for S05, not the terminal run outcome.
        // Keep the same RunId open so the workbook pair can append its evidence and seal once.
        if (qualification.IsQualified) return qualification;
        var sealedResult = await _evidenceStore.SealAsync(qualification.RunId, qualification.Result, cancellationToken);
        return sealedResult.IsSuccess ? qualification : qualification with { IsQualified = false, Result = sealedResult, Snapshot = null };
    }

    private static IEnumerable<QualificationEvidenceEnvelope> EvidenceFor(CatalogQualification qualification, string targetAlias)
    {
        if (qualification.PassA is not null) yield return PassEvidence(qualification.RunId, qualification.PassA, "pass-a");
        if (qualification.PassB is not null) yield return PassEvidence(qualification.RunId, qualification.PassB, "pass-b");
        var terminalCounts = qualification.PassB?.Counts ?? qualification.PassA?.Counts ?? new Dictionary<string, int>();
        var evidencePass = qualification.PassB ?? qualification.PassA;
        var terminalDigests = evidencePass is null ? new Dictionary<string, string>() : new Dictionary<string, string>
        {
            ["scope"] = evidencePass.Scope.Digest,
            ["scopeContracts"] = evidencePass.AppliedCollectionContractsDigest,
            ["targetFingerprint"] = evidencePass.Fingerprint.Digest
        };
        if (qualification.PassA is not null) terminalDigests["passA"] = qualification.PassA.ReconciliationDigest;
        if (qualification.PassB is not null) terminalDigests["passB"] = qualification.PassB.ReconciliationDigest;
        var safeOutcome = EvidenceOutcome(qualification.Result);
        var scopeContracts = ScopeEvidence(qualification.PassB?.Scope ?? qualification.PassA?.Scope);
        if (qualification.Snapshot is not null) { terminalDigests["pullWindow"] = PullWindowDigest(qualification.Snapshot); terminalDigests["snapshot"] = SnapshotBindingDigest(qualification.Snapshot, qualification.PassB!); }
        yield return Envelope(qualification.RunId, qualification.PassA?.Scope.TargetAlias ?? qualification.PassB?.Scope.TargetAlias ?? targetAlias, "reconciliation", Digest(qualification.Result.Reason), safeOutcome, qualification.RetryCount, terminalCounts, terminalDigests, "not-recorded", "not-recorded", [], scopeContracts, [safeOutcome], qualification.Result.Blocker?.FailedShape);
        if (qualification.Snapshot is not null)
            yield return Envelope(qualification.RunId, qualification.Snapshot.Scope.TargetAlias, "qualified-snapshot", qualification.Snapshot.PassBDigest, "HUMAN_REVIEW_REQUIRED", qualification.RetryCount, qualification.Snapshot.Counts, new Dictionary<string, string> { ["passA"] = qualification.Snapshot.PassADigest, ["passB"] = qualification.Snapshot.PassBDigest, ["targetFingerprint"] = qualification.Snapshot.TargetFingerprint.Digest, ["scope"] = qualification.Snapshot.Scope.Digest, ["scopeContracts"] = qualification.PassB!.AppliedCollectionContractsDigest, ["pullWindow"] = PullWindowDigest(qualification.Snapshot), ["snapshot"] = SnapshotBindingDigest(qualification.Snapshot, qualification.PassB) }, DurationBucket(qualification.PassB.Duration), qualification.Snapshot.Scale.RowBucket, [], ScopeEvidence(qualification.Snapshot.Scope), ["HUMAN_REVIEW_REQUIRED", qualification.Snapshot.Scale.Reason]);
    }

    private static QualificationEvidenceEnvelope PassEvidence(Guid runId, CatalogPass pass, string stableKey) => Envelope(
        runId,
        pass.Scope.TargetAlias,
        stableKey,
        pass.ReconciliationDigest,
        "PASS_CAPTURED",
        0,
        pass.Counts,
        new Dictionary<string, string> { ["components"] = pass.ComponentDigest, ["manifests"] = pass.PageManifestDigest, ["orderedIdentities"] = pass.OrderedIdentityDigest, ["readAttestation"] = pass.ReadAttestationDigest, ["scope"] = pass.Scope.Digest, ["scopeContracts"] = pass.AppliedCollectionContractsDigest, ["targetFingerprint"] = pass.Fingerprint.Digest, ["unsupported"] = pass.UnsupportedDigest },
        DurationBucket(pass.Duration),
        "not-recorded",
        pass.ResponseSizeBuckets.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => item.Value).ToArray(),
        ScopeEvidence(pass.Scope),
        ["FULL_READ_COMPLETE"]);

    private static QualificationEvidenceEnvelope Envelope(Guid runId, string targetAlias, string stableKey, string payloadDigest, string outcome, int retryCount, IReadOnlyDictionary<string, int> counts, IReadOnlyDictionary<string, string> digests, string durationBucket, string scaleBucket, IReadOnlyList<string> responseSizeBuckets, IReadOnlyList<EvidenceScopeContract> scopeContracts, IReadOnlyList<string> gateOutcomes, FailedShapeDiagnostic? failedShape = null) =>
        new(QualificationEvidenceEnvelope.SchemaVersion, stableKey, payloadDigest, new QualificationEvidenceMetadata(runId, targetAlias, stableKey, outcome, retryCount, counts, digests, durationBucket, scaleBucket, responseSizeBuckets, scopeContracts, gateOutcomes, WithoutBoundedDiagnosticCompanion(failedShape)));

    // The H-005 discriminator is safe only in SchemaDiagnosticTerminalEvidence.
    // Full qualification evidence must remain independent of this bounded route.
    private static FailedShapeDiagnostic? WithoutBoundedDiagnosticCompanion(FailedShapeDiagnostic? failedShape) =>
        failedShape is null ? null : failedShape with { CompanionGuidStringStatus = null };

    private static IReadOnlyList<EvidenceScopeContract> ScopeEvidence(ScopeDescriptor? scope) => scope?.Collections.Select(item => new EvidenceScopeContract(SafeCollectionId(item.CollectionId), item.OrderKeyId, item.QueryContractId, item.Limits.PageSize, item.Limits.MaxPages, item.Limits.MaxRows, item.Limits.MaxResponseBytes)).ToArray() ?? [];

    private static string SafeCollectionId(string collectionId) => collectionId is "workspace" or "schemas" or "lookup-registry" ? collectionId : "lookup-sha256:" + Digest(collectionId);

    private static string EvidenceOutcome(SafeResult result) => result.IsSuccess ? "HUMAN_REVIEW_REQUIRED" : result.Blocker?.Code switch
    {
        BlockerCode.EndpointNotAllowlisted => "ENDPOINT_NOT_ALLOWLISTED",
        BlockerCode.CatalogOrderOrPagingUnqualified => "CATALOG_ORDER_OR_PAGING_UNQUALIFIED",
        BlockerCode.UnknownShapeUnqualified => "UNKNOWN_SHAPE_UNQUALIFIED",
        BlockerCode.LegacyDispositionInvalid => "LEGACY_DISPOSITION_INVALID",
        BlockerCode.FullCatalogNotQualified => "FULL_CATALOG_NOT_QUALIFIED",
        BlockerCode.TargetStateChangedDuringQualification => "TARGET_STATE_CHANGED_DURING_QUALIFICATION",
        BlockerCode.WorkbookScaleDecisionRequired => "WORKBOOK_SCALE_DECISION_REQUIRED",
        BlockerCode.EvidenceSchemaInvalid => "EVIDENCE_SCHEMA_INVALID",
        BlockerCode.EvidenceScanFailed => "EVIDENCE_SCAN_FAILED",
        BlockerCode.IndexSyncUnresolved => "INDEX_SYNC_UNRESOLVED",
        BlockerCode.OfflineFixtureInvalid => "OFFLINE_FIXTURE_INVALID",
        _ => "FULL_CATALOG_NOT_QUALIFIED"
    };

    private Guid NonEmptyId(params Guid[] distinctFrom)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var candidate = _idFactory();
            if (candidate != Guid.Empty && !distinctFrom.Contains(candidate)) return candidate;
        }
        throw new InvalidOperationException("RUN_ID_GENERATION_FAILED");
    }

    private static string DurationBucket(TimeSpan duration) => duration.TotalSeconds switch { < 0 => "invalid", <= 1 => "0-1s", <= 10 => "1-10s", <= 60 => "10-60s", _ => "60s+" };
    private static string PullWindowDigest(QualifiedCatalogSnapshot snapshot) => Digest(snapshot.PullStartedUtc.ToUniversalTime().ToString("O") + "\n" + snapshot.PullCompletedUtc.ToUniversalTime().ToString("O"));
    private static string SnapshotBindingDigest(QualifiedCatalogSnapshot snapshot, CatalogPass passB) => Digest(System.Text.Json.JsonSerializer.Serialize(new { snapshot.Schema, runId = snapshot.RunId.ToString("D"), pairId = snapshot.PairId.ToString("D"), scope = snapshot.Scope.Digest, sourceIdentity = Digest(snapshot.SourceIdentity), snapshot.PassADigest, snapshot.PassBDigest, targetFingerprint = snapshot.TargetFingerprint.Digest, currentContent = CatalogPassBuilder.CurrentContentBindingDigest(passB), counts = Digest(System.Text.Json.JsonSerializer.Serialize(snapshot.Counts.OrderBy(item => item.Key, StringComparer.Ordinal))), pullWindow = PullWindowDigest(snapshot) }));
    private static string Digest(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool SharesMaterializedData(FullCatalogRead first, FullCatalogRead second)
    {
        var firstReferences = MaterializedReferences(first).ToHashSet(ReferenceEqualityComparer.Instance);
        if (MaterializedReferences(second).Any(firstReferences.Contains)) return true;
        var firstReadIds = ReadIds(first.ReadAttestation).ToHashSet();
        return ReadIds(second.ReadAttestation).Any(firstReadIds.Contains);
    }

    private static IEnumerable<Guid> ReadIds(CatalogReadAttestation attestation) =>
        new[] { attestation.WorkspaceReadId, attestation.LookupRegistryReadId }.Concat(attestation.SchemaReadIds).Concat(attestation.LookupCollectionReadIds.Values);

    private static IEnumerable<object> MaterializedReferences(FullCatalogRead read)
    {
        yield return read;
        yield return read.Workspace;
        yield return read.Workspace.Inventory;
        foreach (var item in read.Workspace.Inventory.Items) { yield return item; if (item.Envelope is not null) yield return item.Envelope; foreach (var unknown in item.UnknownProperties ?? []) yield return unknown; }
        foreach (var schema in read.Workspace.Schemas)
        {
            yield return schema;
            foreach (var column in schema.Columns) { yield return column; if (column.Reference is not null) yield return column.Reference; foreach (var unknown in column.UnknownProperties) yield return unknown; }
            foreach (var index in schema.Indexes) { yield return index; foreach (var member in index.Members) { yield return member; foreach (var unknown in member.UnknownProperties) yield return unknown; } foreach (var unknown in index.UnknownProperties) yield return unknown; }
            foreach (var unknown in schema.UnknownProperties) yield return unknown;
        }
        yield return read.Lookups;
        foreach (var registry in read.Lookups.Registry) yield return registry;
        if (read.Lookups.RegistryManifest is not null) { yield return read.Lookups.RegistryManifest; foreach (var page in read.Lookups.RegistryManifest.Pages) yield return page; }
        foreach (var collection in read.Lookups.Collections)
        {
            yield return collection;
            yield return collection.Manifest;
            foreach (var page in collection.Manifest.Pages) yield return page;
            foreach (var row in collection.Rows) { yield return row; foreach (var value in row.Values) { yield return value; if (value.TypedValue is not null) yield return value.TypedValue; } }
        }
    }

    private async ValueTask<CatalogQualification> QualifyLegacyAsync(CancellationToken cancellationToken)
    {
        var runId = _evidenceStore is null ? NonEmptyId() : await _evidenceStore.BeginRunAsync("fixture", cancellationToken);
        // Compatibility path remains exactly two bounded fixture reads. S06 owns replacement by the full composition root.
        var first = await _legacySource!.ReadPassAsync("fixture", "fixture-scope", 1, cancellationToken);
        var second = await _legacySource.ReadPassAsync(first.TargetAlias, first.ScopeDescriptorHash, 2, cancellationToken);
        var builtA = BuildLegacyPass("Pass A", first);
        var builtB = BuildLegacyPass("Pass B", second);
        var qualification = builtA.Blocker is not null || builtB.Blocker is not null
            ? new CatalogQualification(false, SafeResult.Blocked(builtA.Blocker ?? builtB.Blocker!), null, null, 0, null, runId)
            : CatalogQualification.Reconcile(builtA.Pass, builtB.Pass) with { RunId = runId };
        if (_evidenceStore is not null)
        {
            var safeOutcome = EvidenceOutcome(qualification.Result);
            var envelope = Envelope(runId, first.TargetAlias, "qualification-summary", builtA.Pass.Fingerprint.Digest, safeOutcome, qualification.RetryCount, new Dictionary<string, int> { ["inventory"] = builtA.Pass.InventoryCount }, new Dictionary<string, string> { ["targetFingerprint"] = builtA.Pass.Fingerprint.Digest }, "not-recorded", "not-recorded", [], [], [safeOutcome]);
            var persisted = await _evidenceStore.AppendAsync(runId, envelope, cancellationToken);
            qualification = persisted.IsSuccess ? qualification : qualification with { IsQualified = false, Result = persisted };
            var sealedResult = await _evidenceStore.SealAsync(runId, qualification.Result, cancellationToken);
            if (!sealedResult.IsSuccess) qualification = qualification with { IsQualified = false, Result = sealedResult };
        }
        return qualification;
    }

    private static BuiltLegacyPass BuildLegacyPass(string name, CatalogPassInput input)
    {
        var read = OrderedCatalogReader.Read(input.Collection, input.Pages);
        if (!read.IsQualified)
        {
            var blockedFingerprint = new TargetFingerprint(TargetFingerprint.SchemaVersion, "reader-blocked", []);
            return new BuiltLegacyPass(new QualificationPass(name, blockedFingerprint, read.Manifests, [], TimeSpan.Zero, input.ScopeDescriptorHash), read.Blocker);
        }
        var orderedDigest = PageManifest.Digest(string.Join("\n", input.Pages.SelectMany(page => page.Identities)));
        var manifestDigest = PageManifest.Digest(System.Text.Json.JsonSerializer.Serialize(read.Manifests));
        var fingerprint = TargetFingerprint.Create(new TargetFingerprintInput(
            EndpointClassification.ExactAllowlistVersion, input.ScopeDescriptorHash, input.ObservedTargetVersionEvidence,
            input.Workspace, input.StructuredSchemas,
            [new FingerprintCollectionEntry(input.Collection.CollectionId, input.Collection.OrderKeyId, input.Pages.Sum(page => page.Identities.Count), orderedDigest, manifestDigest)],
            input.Unsupported));
        return new BuiltLegacyPass(new QualificationPass(name, fingerprint, read.Manifests, input.Unsupported.Select(item => item.StableIdentity).OrderBy(item => item, StringComparer.Ordinal).ToArray(), TimeSpan.Zero, input.ScopeDescriptorHash, input.Workspace.Count, orderedDigest, manifestDigest), null);
    }

    private sealed record BuiltLegacyPass(QualificationPass Pass, Blocker? Blocker);
}
