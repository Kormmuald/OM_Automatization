using System.Security.Cryptography;
using System.Text;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem;

public enum WorkbookPublicationCheckpoint { BeforeStaging, AfterValidation, BeforePublication, AfterPublication, AfterPairEvidence, AfterSeal }

public interface IWorkbookPublicationFaultInjector
{
    void Check(WorkbookPublicationCheckpoint checkpoint);
}

public sealed record WorkbookPairPublicationResult(bool IsSuccess, SafeResult Result, StagedWorkbookPair? Pair, string? OutputDirectory);

public sealed class AtomicWorkbookPairPublisher
{
    private readonly AppendOnlyRunStore _runStore;
    private readonly WorkbookPairMaterializer _materializer;
    private readonly IWorkbookPublicationFaultInjector? _faults;

    public AtomicWorkbookPairPublisher(AppendOnlyRunStore runStore, WorkbookPairMaterializer materializer, IWorkbookPublicationFaultInjector? faults = null)
    {
        _runStore = runStore;
        _materializer = materializer;
        _faults = faults;
    }

    public async ValueTask<WorkbookPairPublicationResult> PublishAsync(CatalogQualification qualification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(qualification);
        RunRoot root;
        try { root = _runStore.GetRunRoot(qualification.RunId); }
        catch (DirectoryNotFoundException) { return Failure("RUN_NOT_FOUND"); }
        SafeResult recovered;
        try { recovered = await _runStore.RecoverIncompleteWorkbookPublicationAsync(qualification.RunId, cancellationToken); }
        catch (Exception) { return Failure("WORKBOOK_PAIR_PUBLICATION_FAILED"); }
        if (!recovered.IsSuccess) return Failure(recovered.Reason);
        var accepted = _runStore.ValidateAcceptedQualification(qualification);
        if (!accepted.IsSuccess) return Failure("ACCEPTED_S04_EVIDENCE_REQUIRED");
        var pairId = qualification.Snapshot?.PairId ?? Guid.Empty;
        var staging = Path.Combine(root.RootPath, ".pair-staging-" + pairId.ToString("N") + "-" + Guid.NewGuid().ToString("N"));
        try
        {
            _faults?.Check(WorkbookPublicationCheckpoint.BeforeStaging);
            var staged = await _materializer.StageValidatedPairAsync(qualification, staging, cancellationToken);
            _faults?.Check(WorkbookPublicationCheckpoint.AfterValidation);
            var pair = staged with { ModelPath = Path.Combine(root.OutputPath, WorkbookContract.ModelFileName), LookupPath = Path.Combine(root.OutputPath, WorkbookContract.LookupFileName) };
            var committed = await _runStore.CommitWorkbookPairAsync(qualification, staged, staging, PairEvidence(qualification.Snapshot!, pair), _faults, cancellationToken);
            return committed.IsSuccess ? new WorkbookPairPublicationResult(true, committed, pair, root.OutputPath) : Failure(committed.Reason);
        }
        catch (Exception)
        {
            return Failure("WORKBOOK_PAIR_PUBLICATION_FAILED");
        }
        finally
        {
            try { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
            catch (Exception) { /* Recovery removes bounded staging directories on the next invocation. */ }
        }
    }

    private static QualificationEvidenceEnvelope PairEvidence(QualifiedCatalogSnapshot snapshot, StagedWorkbookPair pair)
    {
        var components = Digest(string.Join("\n", snapshot.ComponentDigests.OrderBy(item => item.StableIdentity, StringComparer.Ordinal).ThenBy(item => item.ComponentKind, StringComparer.Ordinal).Select(item => $"{item.StableIdentity}|{item.ComponentKind}|{item.Digest}")));
        var digests = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["passA"] = snapshot.PassADigest,
            ["passB"] = snapshot.PassBDigest,
            ["targetFingerprint"] = snapshot.TargetFingerprint.Digest,
            ["scope"] = snapshot.Scope.Digest,
            ["components"] = components,
            ["sourceIdentity"] = Digest(snapshot.SourceIdentity),
            ["modelWorkbook"] = pair.ModelSha256,
            ["lookupWorkbook"] = pair.LookupSha256,
            ["pair"] = pair.PairDigest
        };
        var counts = new Dictionary<string, int>(snapshot.Counts, StringComparer.Ordinal);
        var scope = snapshot.Scope.Collections.Select(item => new EvidenceScopeContract(SafeCollectionId(item.CollectionId), item.OrderKeyId, item.QueryContractId, item.Limits.PageSize, item.Limits.MaxPages, item.Limits.MaxRows, item.Limits.MaxResponseBytes)).ToArray();
        digests["pullWindow"] = Digest(snapshot.PullStartedUtc.ToUniversalTime().ToString("O") + "\n" + snapshot.PullCompletedUtc.ToUniversalTime().ToString("O"));
        var metadata = new QualificationEvidenceMetadata(snapshot.RunId, snapshot.Scope.TargetAlias, "workbook-pair", "HUMAN_REVIEW_REQUIRED", 0, counts, digests, "not-recorded", snapshot.Scale.RowBucket, [], scope, ["PAIR_VALIDATED", "PAIR_PUBLISHED", "HUMAN_REVIEW_REQUIRED"]);
        return new QualificationEvidenceEnvelope(QualificationEvidenceEnvelope.SchemaVersion, "workbook-pair", pair.PairDigest, metadata);
    }

    private static string SafeCollectionId(string collectionId) => collectionId is "workspace" or "schemas" or "lookup-registry" ? collectionId : "lookup-sha256:" + Digest(collectionId);
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static WorkbookPairPublicationResult Failure(string reason) => new(false, SafeResult.Blocked(new Blocker(BlockerCode.EvidenceSchemaInvalid, "workbook-pair", reason, "Create a new manually authorized run after inspecting safe evidence.", "Do not expose or accept a partial workbook pair.")), null, null);
}
