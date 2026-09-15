using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;

public static class CatalogQualificationTests
{
    public static void ExactlyTwoPassesReconcileOrSealTerminalChange()
    {
        var a = Pass("a"); var same = Pass("a"); var changed = Pass("b");
        var ok = CatalogQualification.Reconcile(a, same);
        Assert(ok.IsQualified && ok.RetryCount == 0 && ok.Result.Reason == "HUMAN_REVIEW_REQUIRED", "Matching passes must be review-only.");
        var blocked = CatalogQualification.Reconcile(a, changed);
        Assert(!blocked.IsQualified && blocked.RetryCount == 0 && blocked.Result.Reason == "TARGET_STATE_CHANGED_DURING_QUALIFICATION", "Changed target must be terminal without Pass C.");
    }

    public static void FullReconciliationCoversEverySafeComponentAndSnapshotGate()
    {
        var scope = Scope();
        var a = FullPass(CatalogPassOrdinal.A, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), scope);
        var b = FullPass(CatalogPassOrdinal.B, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), scope);
        var ok = CatalogQualification.Reconcile(a, b, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), Started, Completed);
        Assert(ok.IsQualified && ok.Snapshot?.Schema == QualifiedCatalogSnapshot.SchemaVersion && ok.Snapshot.PassADigest == ok.Snapshot.PassBDigest, "Equal full passes did not cross the snapshot gate.");

        var mutations = new CatalogPass[]
        {
            b with { ObservedTargetVersionEvidence = ["v2"], ReconciliationDigest = "changed-version" },
            b with { OrderedIdentities = ["identity-2"], ReconciliationDigest = "changed-identities" },
            b with { Counts = new Dictionary<string, int> { ["workspaceItems"] = 2 }, ReconciliationDigest = "changed-counts" },
            b with { PageManifests = [PageManifest.Create(0, "1", ["identity-1"])], ReconciliationDigest = "changed-manifests" },
            b with { ComponentDigests = [new CatalogComponentDigest("component", "schema", "digest-2")], ReconciliationDigest = "changed-components" },
            b with { ResponseSizeBuckets = new Dictionary<string, string> { ["workspace"] = "16KiB-1MiB" }, ReconciliationDigest = "changed-response-size" },
            b with { Unsupported = [new ScopedUnsupportedDiagnostic("unsupported", SupportStatus.Unsupported, "shape", "UNSUPPORTED")], ReconciliationDigest = "changed-unsupported" },
            b with { Fingerprint = new TargetFingerprint(TargetFingerprint.SchemaVersion, "fingerprint-2", ["v1"]), ReconciliationDigest = "changed-fingerprint" }
        };
        foreach (var mutation in mutations)
        {
            var result = CatalogQualification.Reconcile(a, mutation, Guid.NewGuid(), Guid.NewGuid(), Started, Completed);
            Assert(!result.IsQualified && result.Snapshot is null && result.RetryCount == 0 && result.Result.Reason == "TARGET_STATE_CHANGED_DURING_QUALIFICATION", "A reconciliation component changed without a terminal target-change blocker.");
        }

        var reused = CatalogQualification.Reconcile(a, b with { IndependentReadId = a.IndependentReadId }, Guid.NewGuid(), Guid.NewGuid(), Started, Completed);
        Assert(!reused.IsQualified && reused.Result.Reason == "PASS_B_INDEPENDENCE_UNQUALIFIED" && reused.RetryCount == 0, "Same-read cache reuse crossed the snapshot gate.");

        var paging = CatalogQualification.Terminal(new Blocker(BlockerCode.CatalogOrderOrPagingUnqualified, "lookup", "LOOKUP_PAGE_OFFSET_UNQUALIFIED", "Inspect fixture.", "Stop."), a, null, Guid.NewGuid());
        Assert(!paging.IsQualified && paging.PassB is null && paging.Snapshot is null && paging.RetryCount == 0 && paging.Result.Reason == "LOOKUP_PAGE_OFFSET_UNQUALIFIED", "A pass blocker did not remain terminal.");
    }

    public static void ScopeDigestIsCanonicalAndSensitiveToOrderedContract()
    {
        var first = Scope();
        var copy = Scope();
        var changed = ScopeDescriptor.Create("fixture", "origin-policy", "ReadEndpointAllowlist/v1", [new CatalogCollectionScope("lookup", "Name", "query-v1", new CatalogReadLimits(500, 1000, 1_000_000))], QualifiedCatalogSnapshot.SchemaVersion, "WorkbookProjection/v1");
        Assert(first.Digest == copy.Digest && first.IsSealed, "Equivalent scope contracts do not have a canonical seal.");
        Assert(first.Digest != changed.Digest, "Scope contract mutation did not change its digest.");
    }

    public static void WorkbookScaleLimitIncludesHeaderRow()
    {
        var scope = Scope();
        var a = FullPass(CatalogPassOrdinal.A, Guid.NewGuid(), scope) with { DeclaredWorkbookRowLimit = 1 };
        var b = FullPass(CatalogPassOrdinal.B, Guid.NewGuid(), scope) with { DeclaredWorkbookRowLimit = 1 };
        var result = CatalogQualification.Reconcile(a, b, Guid.NewGuid(), Guid.NewGuid(), Started, Completed);
        Assert(!result.IsQualified && result.Result.Reason == "WORKBOOK_SCALE_DECISION_REQUIRED", "Workbook row forecast omitted the header row at the declared worksheet limit.");
    }

    private static QualificationPass Pass(string value) => new("test", new TargetFingerprint(TargetFingerprint.SchemaVersion, value, []), [], [], TimeSpan.Zero);
    private static readonly DateTimeOffset Started = new(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Completed = Started.AddSeconds(1);
    private static ScopeDescriptor Scope() => ScopeDescriptor.Create("fixture", "origin-policy", "ReadEndpointAllowlist/v1", [new CatalogCollectionScope("lookup", "Id", "query-v1", new CatalogReadLimits(500, 1000, 1_000_000))], QualifiedCatalogSnapshot.SchemaVersion, "WorkbookProjection/v1");
    private static CatalogPass FullPass(CatalogPassOrdinal ordinal, Guid readId, ScopeDescriptor scope)
    {
        var inventory = WorkspaceInventory.Create([]);
        var content = new CatalogPassContent(new WorkspaceObjectModel(true, inventory, [], null), new LookupCatalog(true, [], null, [], null));
        return new CatalogPass(ordinal, readId, "fake:s04", scope, content, ["v1"], ["identity-1"], new Dictionary<string, int> { ["workspaceItems"] = 1 }, [PageManifest.Create(0, "0", ["identity-1"])], [new CatalogComponentDigest("component", "schema", "digest-1")], [], new TargetFingerprint(TargetFingerprint.SchemaVersion, "fingerprint-1", ["v1"]), "ordered", "manifests", "components", "unsupported", "reconciled", scope.Collections, "scope-contracts", "read-attestation", new Dictionary<string, string> { ["workspace"] = "0-16KiB" }, TimeSpan.FromMilliseconds(10), 1000);
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
