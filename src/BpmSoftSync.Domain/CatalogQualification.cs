namespace BpmSoftSync.Domain;

// Compatibility input for the pre-S04 bounded fixture path. The full S04 path uses CatalogPass.
public sealed record QualificationPass(string PassName, TargetFingerprint Fingerprint, IReadOnlyList<PageManifest> PageManifests, IReadOnlyList<string> UnsupportedStableIdentities, TimeSpan Duration, string ScopeDescriptorHash = "", int InventoryCount = 0, string OrderedIdentityDigest = "", string PageManifestDigest = "");

public sealed record CatalogQualification(bool IsQualified, SafeResult Result, CatalogPass? PassA, CatalogPass? PassB, int RetryCount, QualifiedCatalogSnapshot? Snapshot = null, Guid RunId = default)
{
    public static CatalogQualification Reconcile(CatalogPass passA, CatalogPass passB, Guid runId, Guid pairId, DateTimeOffset pullStartedUtc, DateTimeOffset pullCompletedUtc)
    {
        ArgumentNullException.ThrowIfNull(passA);
        ArgumentNullException.ThrowIfNull(passB);
        if (passA.Ordinal != CatalogPassOrdinal.A || passB.Ordinal != CatalogPassOrdinal.B || !ReferenceEquals(passA.Scope, passB.Scope) || !passA.Scope.IsSealed || passA.IndependentReadId == passB.IndependentReadId || pullStartedUtc == default || pullCompletedUtc < pullStartedUtc)
            return Blocked(passA, passB, runId, BlockerCode.FullCatalogNotQualified, "PASS_B_INDEPENDENCE_UNQUALIFIED");

        if (!string.Equals(passA.ReconciliationDigest, passB.ReconciliationDigest, StringComparison.Ordinal) ||
            !string.Equals(passA.Scope.Digest, passB.Scope.Digest, StringComparison.Ordinal) ||
            !string.Equals(passA.SourceIdentity, passB.SourceIdentity, StringComparison.Ordinal) ||
            !passA.AppliedCollectionContracts.SequenceEqual(passB.AppliedCollectionContracts) ||
            !string.Equals(passA.AppliedCollectionContractsDigest, passB.AppliedCollectionContractsDigest, StringComparison.Ordinal) ||
            !passA.ObservedTargetVersionEvidence.SequenceEqual(passB.ObservedTargetVersionEvidence, StringComparer.Ordinal) ||
            !passA.OrderedIdentities.SequenceEqual(passB.OrderedIdentities, StringComparer.Ordinal) ||
            !DictionaryEqual(passA.Counts, passB.Counts) ||
            !DictionaryEqual(passA.ResponseSizeBuckets, passB.ResponseSizeBuckets) ||
            !passA.PageManifests.SequenceEqual(passB.PageManifests) ||
            !passA.ComponentDigests.SequenceEqual(passB.ComponentDigests) ||
            !passA.Unsupported.SequenceEqual(passB.Unsupported) ||
            !string.Equals(passA.Fingerprint.Digest, passB.Fingerprint.Digest, StringComparison.Ordinal))
            return Blocked(passA, passB, runId, BlockerCode.TargetStateChangedDuringQualification, "TARGET_STATE_CHANGED_DURING_QUALIFICATION");

        var scale = WorkbookScaleForecast.CreateForCatalogPair(passB.Counts, passB.ComponentDigests.Count, passB.DeclaredWorkbookRowLimit);
        if (scale.Status == WorkbookScaleForecastStatus.DecisionRequired)
            return Blocked(passA, passB, runId, BlockerCode.WorkbookScaleDecisionRequired, "WORKBOOK_SCALE_DECISION_REQUIRED");

        var snapshot = new QualifiedCatalogSnapshot(QualifiedCatalogSnapshot.SchemaVersion, runId, pairId, passB.Scope, passB.SourceIdentity, passB.Content.Workspace, passB.Content.Lookups, passA.ReconciliationDigest, passB.ReconciliationDigest, passB.Fingerprint, passB.OrderedIdentities, passB.ComponentDigests, passB.Unsupported, passB.Counts, scale, pullStartedUtc, pullCompletedUtc);
        return new(true, SafeResult.SuccessForHumanReview("qualified-catalog-snapshot"), passA, passB, 0, snapshot, runId);
    }

    public static CatalogQualification Reconcile(QualificationPass passA, QualificationPass passB)
    {
        ArgumentNullException.ThrowIfNull(passA);
        ArgumentNullException.ThrowIfNull(passB);
        var equal = string.Equals(passA.Fingerprint.Digest, passB.Fingerprint.Digest, StringComparison.Ordinal) &&
                    string.Equals(passA.ScopeDescriptorHash, passB.ScopeDescriptorHash, StringComparison.Ordinal) &&
                    passA.InventoryCount == passB.InventoryCount &&
                    string.Equals(passA.OrderedIdentityDigest, passB.OrderedIdentityDigest, StringComparison.Ordinal) &&
                    string.Equals(passA.PageManifestDigest, passB.PageManifestDigest, StringComparison.Ordinal) &&
                    passA.UnsupportedStableIdentities.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(passB.UnsupportedStableIdentities.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
        return equal
            ? new CatalogQualification(true, SafeResult.SuccessForHumanReview("two reconciled offline passes"), null, null, 0)
            : new CatalogQualification(false, SafeResult.Blocked(TargetChanged()), null, null, 0);
    }

    public static CatalogQualification Terminal(Blocker blocker, CatalogPass? passA, CatalogPass? passB, Guid runId) => new(false, SafeResult.Blocked(blocker), passA, passB, 0, null, runId);

    private static CatalogQualification Blocked(CatalogPass passA, CatalogPass passB, Guid runId, BlockerCode code, string reason) =>
        new(false, SafeResult.Blocked(code == BlockerCode.TargetStateChangedDuringQualification ? TargetChanged() : new Blocker(code, "catalog-qualification", reason, "Start a new manually authorized run with fresh independent reads.", "Seal this run; do not retry or run Pass C.")), passA, passB, 0, null, runId);

    private static Blocker TargetChanged() => new(BlockerCode.TargetStateChangedDuringQualification, "catalog-qualification", "TARGET_STATE_CHANGED_DURING_QUALIFICATION", "Obtain a new human authorization for the exact target and declared read scope.", "Seal this safe terminal result; do not run Pass C or retry.");
    private static bool DictionaryEqual(IReadOnlyDictionary<string, int> first, IReadOnlyDictionary<string, int> second) => first.Count == second.Count && first.All(item => second.TryGetValue(item.Key, out var value) && value == item.Value);
    private static bool DictionaryEqual(IReadOnlyDictionary<string, string> first, IReadOnlyDictionary<string, string> second) => first.Count == second.Count && first.All(item => second.TryGetValue(item.Key, out var value) && string.Equals(value, item.Value, StringComparison.Ordinal));
}
