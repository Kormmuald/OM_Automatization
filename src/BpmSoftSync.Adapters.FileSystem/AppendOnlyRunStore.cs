using BpmSoftSync.Application;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Domain;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BpmSoftSync.Adapters.FileSystem;

public sealed record RunRoot(Guid RunId, string RootPath, string AuditPath, string EvidencePath, string OutputPath, string JournalPath);

public sealed class AppendOnlyRunStore(string basePath) : IRunEvidenceStore
{
    private readonly string _basePath = Path.GetFullPath(basePath);
    private readonly Dictionary<Guid, RunRoot> _runs = [];
    private readonly object _sync = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RunRoot CreateRun(DateTimeOffset now, Guid? runId = null)
    {
        var id = runId ?? Guid.NewGuid();
        var runsRoot = Path.Combine(_basePath, "runs");
        var parent = Path.Combine(runsRoot, now.UtcDateTime.ToString("yyyy"), now.UtcDateTime.ToString("MM"), now.UtcDateTime.ToString("dd"));
        var root = Path.Combine(parent, id.ToString("D"));
        lock (_sync)
        {
            Directory.CreateDirectory(runsRoot);
            var globalClaims = Path.Combine(runsRoot, ".run-id-claims");
            Directory.CreateDirectory(globalClaims);
            if (_runs.ContainsKey(id) || Directory.EnumerateDirectories(runsRoot, id.ToString("D"), SearchOption.AllDirectories).Any()) throw new IOException("RUN_ROOT_ALREADY_EXISTS");
            try
            {
                using var globalClaim = new FileStream(Path.Combine(globalClaims, id.ToString("D")), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                globalClaim.WriteByte(1);
                globalClaim.Flush(true);
            }
            catch (IOException) { throw new IOException("RUN_ROOT_ALREADY_EXISTS"); }
            Directory.CreateDirectory(parent);
            Directory.CreateDirectory(root);
            try
            {
                using var reservation = new FileStream(Path.Combine(root, ".run-reservation"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                reservation.WriteByte(1);
                reservation.Flush(true);
            }
            catch (IOException) { throw new IOException("RUN_ROOT_ALREADY_EXISTS"); }
            var audit = Path.Combine(root, "audit");
            var evidence = Path.Combine(root, "evidence");
            var output = Path.Combine(root, "output");
            Directory.CreateDirectory(audit);
            Directory.CreateDirectory(evidence);
            Directory.CreateDirectory(output);
            var run = new RunRoot(id, root, audit, evidence, output, Path.Combine(root, "run-journal.json"));
            _runs.Add(id, run);
            return run;
        }
    }

    public async ValueTask<Guid> BeginRunAsync(string targetAlias, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsTargetAlias(targetAlias)) throw new InvalidDataException("EVIDENCE_SCHEMA_INVALID");
        var run = CreateRun(DateTimeOffset.UtcNow);
        await using var aliasFile = new FileStream(Path.Combine(run.RootPath, ".target-alias"), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await aliasFile.WriteAsync(Encoding.UTF8.GetBytes(targetAlias), cancellationToken);
        await aliasFile.FlushAsync(cancellationToken);
        return run.RunId;
    }

    public async ValueTask<SafeResult> AppendAsync(Guid runId, QualificationEvidenceEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var root = ResolveRun(runId);
        if (root is null) return MissingRun();
        if (envelope.Metadata.RunId != runId || !string.Equals(envelope.StableKey, envelope.Metadata.Phase, StringComparison.Ordinal)) return Blocked(BlockerCode.EvidenceSchemaInvalid, "EVIDENCE_STABLE_KEY_MISMATCH");
        await using var runLock = await AcquireRunLockAsync(root, cancellationToken);
        if (File.Exists(SealPath(root))) return Blocked(BlockerCode.EvidenceSchemaInvalid, "RUN_ALREADY_SEALED");
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var validator = new EvidenceEnvelopeValidator();
        var validation = validator.Validate(json);
        if (!validation.IsSuccess) return validation;
        return await WriteValidatedEvidenceUnderLockAsync(root, envelope.StableKey, json, validator, cancellationToken);
    }

    public async ValueTask<SafeResult> SealAsync(Guid runId, SafeResult outcome, CancellationToken cancellationToken = default)
    {
        var root = ResolveRun(runId);
        if (root is null) return MissingRun();
        var envelope = SealEnvelope(runId, root, outcome);
        var stableKey = envelope.StableKey;
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        var validator = new EvidenceEnvelopeValidator();
        var validation = validator.Validate(json);
        if (!validation.IsSuccess) return validation;

        await using var runLock = await AcquireRunLockAsync(root, cancellationToken);
        try
        {
            await using var seal = new FileStream(SealPath(root), FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
            await seal.WriteAsync(Encoding.UTF8.GetBytes(stableKey), cancellationToken);
            await seal.FlushAsync(cancellationToken);
        }
        catch (IOException) { return Blocked(BlockerCode.EvidenceSchemaInvalid, "RUN_ALREADY_SEALED"); }

        // Claim the persistent seal first, so every subsequent path is fail-closed.
        return await WriteValidatedEvidenceUnderLockAsync(root, stableKey, json, validator, cancellationToken);
    }

    public SafeResult Diagnose(Guid runId, int maxRecords, out IReadOnlyList<string> stableKeys)
    {
        stableKeys = [];
        var root = ResolveRun(runId);
        if (maxRecords is < 1 or > 100 || root is null) return MissingRun();
        var names = Directory.EnumerateFiles(root.EvidencePath, "*.json").OrderBy(path => path, StringComparer.Ordinal).Take(maxRecords + 1).ToArray();
        if (names.Length > maxRecords) return SafeResult.Blocked(new Blocker(BlockerCode.EvidenceSchemaInvalid, "diagnose", "DIAGNOSE_LIMIT_EXCEEDED", "Request a smaller bounded diagnostic window.", "Do not enumerate the run tree."));
        var validator = new EvidenceEnvelopeValidator();
        foreach (var path in names)
            if (!validator.Validate(File.ReadAllText(path)).IsSuccess) return SafeResult.Blocked(new Blocker(BlockerCode.EvidenceSchemaInvalid, "diagnose", "EVIDENCE_SCHEMA_INVALID", "Inspect the safe blocked terminal record.", "Stop diagnosis."));
        stableKeys = names.Select(path => Path.GetFileNameWithoutExtension(path)!).ToArray();
        return SafeResult.SuccessForHumanReview("safe-run-diagnosis");
    }

    public RunRoot GetRunRoot(Guid runId) => ResolveRun(runId) ?? throw new DirectoryNotFoundException("RUN_NOT_FOUND");

    public SafeResult ValidateAcceptedQualification(CatalogQualification qualification)
    {
        ArgumentNullException.ThrowIfNull(qualification);
        var root = ResolveRun(qualification.RunId);
        return root is null ? MissingRun() : ValidateAcceptedQualificationUnderLock(root, qualification);
    }

    public async ValueTask<SafeResult> RecoverIncompleteWorkbookPublicationAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var root = ResolveRun(runId);
        if (root is null) return MissingRun();
        await using var runLock = await AcquireRunLockAsync(root, cancellationToken);
        try { return RecoverIncompleteWorkbookPublicationUnderLock(root); }
        catch (Exception) { return Blocked(BlockerCode.EvidenceSchemaInvalid, "WORKBOOK_PUBLICATION_RECOVERY_FAILED"); }
    }

    internal async ValueTask<SafeResult> CommitWorkbookPairAsync(CatalogQualification qualification, StagedWorkbookPair pair, string stagingDirectory, QualificationEvidenceEnvelope pairEnvelope, IWorkbookPublicationFaultInjector? faults, CancellationToken cancellationToken)
    {
        var root = ResolveRun(qualification.RunId);
        if (root is null) return MissingRun();
        await using var runLock = await AcquireRunLockAsync(root, cancellationToken);
        var accepted = ValidateAcceptedQualificationUnderLock(root, qualification);
        if (!accepted.IsSuccess) return accepted;
        var validator = new EvidenceEnvelopeValidator();
        var pairJson = JsonSerializer.Serialize(pairEnvelope, JsonOptions);
        var sealEnvelope = SealEnvelope(qualification.RunId, root, SafeResult.SuccessForHumanReview("workbook-pair"));
        var sealJson = JsonSerializer.Serialize(sealEnvelope, JsonOptions);
        if (!validator.Validate(pairJson).IsSuccess || !validator.Validate(sealJson).IsSuccess) return Blocked(BlockerCode.EvidenceSchemaInvalid, "EVIDENCE_SCHEMA_INVALID");
        if (pair.RunId != qualification.RunId || pairEnvelope.Metadata.RunId != qualification.RunId || pairEnvelope.StableKey != "workbook-pair") return Blocked(BlockerCode.EvidenceSchemaInvalid, "WORKBOOK_PAIR_BINDING_INVALID");

        var staging = Path.GetFullPath(stagingDirectory);
        var expectedParent = Path.GetFullPath(root.RootPath) + Path.DirectorySeparatorChar;
        if (!staging.StartsWith(expectedParent, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(staging) || Directory.EnumerateDirectories(staging).Any()) return Blocked(BlockerCode.EvidenceSchemaInvalid, "WORKBOOK_STAGING_INVALID");
        var stagedFiles = Directory.EnumerateFiles(staging).Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!stagedFiles.SequenceEqual(new[] { WorkbookContract.LookupFileName, WorkbookContract.ModelFileName }, StringComparer.Ordinal)) return Blocked(BlockerCode.EvidenceSchemaInvalid, "WORKBOOK_STAGING_INVALID");

        var outputPublished = false;
        var outputDetached = false;
        var journalExisted = File.Exists(root.JournalPath);
        var journalLength = journalExisted ? new FileInfo(root.JournalPath).Length : 0L;
        var pairEvidencePath = Path.Combine(root.EvidencePath, "workbook-pair.json");
        var sealEvidencePath = Path.Combine(root.EvidencePath, "review-only-seal.json");
        var sealPath = SealPath(root);
        var pendingPath = PendingPublicationPath(root);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            faults?.Check(WorkbookPublicationCheckpoint.BeforePublication);
            if (!Directory.Exists(root.OutputPath) || Directory.EnumerateFileSystemEntries(root.OutputPath).Any() || File.Exists(pairEvidencePath) || File.Exists(sealEvidencePath) || File.Exists(sealPath)) throw new IOException("WORKBOOK_PUBLICATION_TARGET_NOT_EMPTY");
            WriteDurableNew(pendingPath, JsonSerializer.Serialize(new { schema = "WorkbookPublicationPending/v1", runId = qualification.RunId.ToString("D"), pairId = pair.PairId.ToString("D"), journalExisted, journalLength }));
            Directory.Delete(root.OutputPath, false);
            outputDetached = true;
            Directory.Move(staging, root.OutputPath);
            outputPublished = true;
            outputDetached = false;
            faults?.Check(WorkbookPublicationCheckpoint.AfterPublication);

            WriteDurableNew(pairEvidencePath, pairJson);
            AppendDurable(root.JournalPath, pairJson + Environment.NewLine);
            faults?.Check(WorkbookPublicationCheckpoint.AfterPairEvidence);

            WriteDurableNew(sealPath, "review-only-seal");
            WriteDurableNew(sealEvidencePath, sealJson);
            AppendDurable(root.JournalPath, sealJson + Environment.NewLine);
            faults?.Check(WorkbookPublicationCheckpoint.AfterSeal);
            TryDeleteFile(pendingPath);
            return validator.Validate(sealJson);
        }
        catch (Exception)
        {
            try
            {
                TryDeleteFile(sealEvidencePath);
                TryDeleteFile(sealPath);
                TryDeleteFile(pairEvidencePath);
                RestoreJournal(root.JournalPath, journalExisted, journalLength);
                if (outputPublished && Directory.Exists(root.OutputPath)) Directory.Delete(root.OutputPath, true);
                if ((outputPublished || outputDetached) && !Directory.Exists(root.OutputPath)) Directory.CreateDirectory(root.OutputPath);
                TryDeleteFile(pendingPath);
            }
            catch (Exception) { /* The durable pending marker makes the next invocation recover deterministically. */ }
            return Blocked(BlockerCode.EvidenceSchemaInvalid, "WORKBOOK_PAIR_PUBLICATION_FAILED");
        }
    }

    private static async Task<SafeResult> WriteValidatedEvidenceUnderLockAsync(RunRoot root, string stableKey, string json, EvidenceEnvelopeValidator validator, CancellationToken cancellationToken)
    {
        var validation = validator.Validate(json);
        if (!validation.IsSuccess) return validation;
        var path = Path.Combine(root.EvidencePath, stableKey + ".json");
        try
        {
            await using var evidence = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
            await evidence.WriteAsync(Encoding.UTF8.GetBytes(json), cancellationToken);
            await evidence.FlushAsync(cancellationToken);
        }
        catch (IOException) { return Blocked(BlockerCode.EvidenceSchemaInvalid, "EVIDENCE_ALREADY_EXISTS"); }
        validation = validator.Validate(await File.ReadAllTextAsync(path, cancellationToken));
        if (!validation.IsSuccess) return validation;
        await using var journal = new FileStream(root.JournalPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await journal.WriteAsync(Encoding.UTF8.GetBytes(json + Environment.NewLine), cancellationToken);
        await journal.FlushAsync(cancellationToken);
        return validation;
    }

    private static SafeResult ValidateAcceptedQualificationUnderLock(RunRoot root, CatalogQualification qualification)
    {
        var snapshot = qualification.Snapshot;
        var passA = qualification.PassA;
        var passB = qualification.PassB;
        if (!qualification.IsQualified || qualification.Result.Reason != "HUMAN_REVIEW_REQUIRED" || qualification.RetryCount != 0 || snapshot is null || passA is null || passB is null ||
            snapshot.RunId != qualification.RunId || snapshot.PassADigest != passA.ReconciliationDigest || snapshot.PassBDigest != passB.ReconciliationDigest || snapshot.Scope.Digest != passB.Scope.Digest ||
            snapshot.SourceIdentity != passB.SourceIdentity || snapshot.TargetFingerprint.Digest != passB.Fingerprint.Digest || snapshot.PullStartedUtc == default || snapshot.PullCompletedUtc < snapshot.PullStartedUtc ||
            !ReferenceEquals(snapshot.Workspace, passB.Content.Workspace) || !ReferenceEquals(snapshot.Lookups, passB.Content.Lookups) || !CatalogPassBuilder.IsCurrentContentBindingValid(passB) ||
            !snapshot.OrderedIdentities.SequenceEqual(passB.OrderedIdentities, StringComparer.Ordinal) || !snapshot.ComponentDigests.SequenceEqual(passB.ComponentDigests) || !DictionaryEqual(snapshot.Counts, passB.Counts) ||
            File.Exists(SealPath(root)) || !Directory.Exists(root.OutputPath) || Directory.EnumerateFileSystemEntries(root.OutputPath).Any()) return Blocked(BlockerCode.EvidenceSchemaInvalid, "ACCEPTED_S04_EVIDENCE_REQUIRED");

        var expected = new[] { ExpectedPass(qualification.RunId, passA, "pass-a"), ExpectedPass(qualification.RunId, passB, "pass-b"), ExpectedReconciliation(qualification), ExpectedSnapshot(qualification) };
        var validator = new EvidenceEnvelopeValidator();
        if (!File.Exists(root.JournalPath)) return Blocked(BlockerCode.EvidenceSchemaInvalid, "ACCEPTED_S04_EVIDENCE_REQUIRED");
        var journalLines = File.ReadAllLines(root.JournalPath).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        foreach (var envelope in expected)
        {
            var path = Path.Combine(root.EvidencePath, envelope.StableKey + ".json");
            if (!File.Exists(path)) return Blocked(BlockerCode.EvidenceSchemaInvalid, "ACCEPTED_S04_EVIDENCE_REQUIRED");
            var actual = File.ReadAllText(path);
            var expectedJson = JsonSerializer.Serialize(envelope, JsonOptions);
            if (!validator.Validate(actual).IsSuccess || !string.Equals(actual, expectedJson, StringComparison.Ordinal) || journalLines.Count(line => string.Equals(line, actual, StringComparison.Ordinal)) != 1)
                return Blocked(BlockerCode.EvidenceSchemaInvalid, "ACCEPTED_S04_EVIDENCE_BINDING_INVALID");
        }
        return SafeResult.SuccessForHumanReview("accepted-s04-evidence");
    }

    private static QualificationEvidenceEnvelope ExpectedPass(Guid runId, CatalogPass pass, string stableKey) => Envelope(runId, pass.Scope.TargetAlias, stableKey, pass.ReconciliationDigest, "PASS_CAPTURED", pass.Counts,
        new Dictionary<string, string> { ["components"] = pass.ComponentDigest, ["manifests"] = pass.PageManifestDigest, ["orderedIdentities"] = pass.OrderedIdentityDigest, ["readAttestation"] = pass.ReadAttestationDigest, ["scope"] = pass.Scope.Digest, ["scopeContracts"] = pass.AppliedCollectionContractsDigest, ["targetFingerprint"] = pass.Fingerprint.Digest, ["unsupported"] = pass.UnsupportedDigest },
        DurationBucket(pass.Duration), "not-recorded", pass.ResponseSizeBuckets.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => item.Value).ToArray(), ScopeEvidence(pass.Scope), ["FULL_READ_COMPLETE"]);

    private static QualificationEvidenceEnvelope ExpectedReconciliation(CatalogQualification qualification)
    {
        var snapshot = qualification.Snapshot!;
        var passB = qualification.PassB!;
        return Envelope(qualification.RunId, snapshot.Scope.TargetAlias, "reconciliation", Digest(qualification.Result.Reason), "HUMAN_REVIEW_REQUIRED", passB.Counts,
            new Dictionary<string, string> { ["scope"] = passB.Scope.Digest, ["scopeContracts"] = passB.AppliedCollectionContractsDigest, ["targetFingerprint"] = passB.Fingerprint.Digest, ["passA"] = qualification.PassA!.ReconciliationDigest, ["passB"] = passB.ReconciliationDigest, ["pullWindow"] = PullWindowDigest(snapshot), ["snapshot"] = SnapshotBindingDigest(snapshot, passB) },
            "not-recorded", "not-recorded", [], ScopeEvidence(snapshot.Scope), ["HUMAN_REVIEW_REQUIRED"]);
    }

    private static QualificationEvidenceEnvelope ExpectedSnapshot(CatalogQualification qualification)
    {
        var snapshot = qualification.Snapshot!;
        var passB = qualification.PassB!;
        return Envelope(qualification.RunId, snapshot.Scope.TargetAlias, "qualified-snapshot", snapshot.PassBDigest, "HUMAN_REVIEW_REQUIRED", snapshot.Counts,
            new Dictionary<string, string> { ["passA"] = snapshot.PassADigest, ["passB"] = snapshot.PassBDigest, ["targetFingerprint"] = snapshot.TargetFingerprint.Digest, ["scope"] = snapshot.Scope.Digest, ["scopeContracts"] = passB.AppliedCollectionContractsDigest, ["pullWindow"] = PullWindowDigest(snapshot), ["snapshot"] = SnapshotBindingDigest(snapshot, passB) },
            DurationBucket(passB.Duration), snapshot.Scale.RowBucket, [], ScopeEvidence(snapshot.Scope), ["HUMAN_REVIEW_REQUIRED", snapshot.Scale.Reason]);
    }

    private static QualificationEvidenceEnvelope Envelope(Guid runId, string targetAlias, string stableKey, string payloadDigest, string outcome, IReadOnlyDictionary<string, int> counts, IReadOnlyDictionary<string, string> digests, string durationBucket, string scaleBucket, IReadOnlyList<string> responseSizeBuckets, IReadOnlyList<EvidenceScopeContract> scopeContracts, IReadOnlyList<string> gates) =>
        new(QualificationEvidenceEnvelope.SchemaVersion, stableKey, payloadDigest, new(runId, targetAlias, stableKey, outcome, 0, counts, digests, durationBucket, scaleBucket, responseSizeBuckets, scopeContracts, gates));

    private static IReadOnlyList<EvidenceScopeContract> ScopeEvidence(ScopeDescriptor scope) => scope.Collections.Select(item => new EvidenceScopeContract(SafeCollectionId(item.CollectionId), item.OrderKeyId, item.QueryContractId, item.Limits.PageSize, item.Limits.MaxPages, item.Limits.MaxRows, item.Limits.MaxResponseBytes)).ToArray();
    private static string SafeCollectionId(string collectionId) => collectionId is "workspace" or "schemas" or "lookup-registry" ? collectionId : "lookup-sha256:" + Digest(collectionId);
    private static string DurationBucket(TimeSpan duration) => duration.TotalSeconds switch { < 0 => "invalid", <= 1 => "0-1s", <= 10 => "1-10s", <= 60 => "10-60s", _ => "60s+" };
    private static string PullWindowDigest(QualifiedCatalogSnapshot snapshot) => Digest(snapshot.PullStartedUtc.ToUniversalTime().ToString("O") + "\n" + snapshot.PullCompletedUtc.ToUniversalTime().ToString("O"));
    private static string SnapshotBindingDigest(QualifiedCatalogSnapshot snapshot, CatalogPass passB) => Digest(JsonSerializer.Serialize(new { snapshot.Schema, runId = snapshot.RunId.ToString("D"), pairId = snapshot.PairId.ToString("D"), scope = snapshot.Scope.Digest, sourceIdentity = Digest(snapshot.SourceIdentity), snapshot.PassADigest, snapshot.PassBDigest, targetFingerprint = snapshot.TargetFingerprint.Digest, currentContent = CatalogPassBuilder.CurrentContentBindingDigest(passB), counts = Digest(JsonSerializer.Serialize(snapshot.Counts.OrderBy(item => item.Key, StringComparer.Ordinal))), pullWindow = PullWindowDigest(snapshot) }));
    private static QualificationEvidenceEnvelope SealEnvelope(Guid runId, RunRoot root, SafeResult outcome)
    {
        var stableKey = outcome.IsSuccess ? "review-only-seal" : "blocked-terminal";
        var safeOutcome = OutcomeToken(outcome);
        return new QualificationEvidenceEnvelope(QualificationEvidenceEnvelope.SchemaVersion, stableKey, Digest(safeOutcome), new QualificationEvidenceMetadata(runId, ReadTargetAlias(root), stableKey, safeOutcome, 0, new Dictionary<string, int>(), new Dictionary<string, string>(), "not-recorded", "not-recorded", [], [], [safeOutcome]));
    }

    private static void WriteDurableNew(string path, string text)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        var bytes = Encoding.UTF8.GetBytes(text);
        stream.Write(bytes);
        stream.Flush(true);
    }

    private static void AppendDurable(string path, string text)
    {
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        var bytes = Encoding.UTF8.GetBytes(text);
        stream.Write(bytes);
        stream.Flush(true);
    }

    private static void RestoreJournal(string path, bool existed, long length)
    {
        if (!File.Exists(path)) return;
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough)) { stream.SetLength(length); stream.Flush(true); }
        if (!existed && length == 0) File.Delete(path);
    }

    private static void TryDeleteFile(string path) { if (File.Exists(path)) File.Delete(path); }

    private static SafeResult RecoverIncompleteWorkbookPublicationUnderLock(RunRoot root)
    {
        foreach (var staging in Directory.EnumerateDirectories(root.RootPath, ".pair-staging-*", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
            Directory.Delete(staging, true);

        var pairEvidencePath = Path.Combine(root.EvidencePath, "workbook-pair.json");
        var sealEvidencePath = Path.Combine(root.EvidencePath, "review-only-seal.json");
        var sealPath = SealPath(root);
        var pendingPath = PendingPublicationPath(root);
        var outputEntries = Directory.Exists(root.OutputPath) ? Directory.EnumerateFileSystemEntries(root.OutputPath).ToArray() : [];
        var outputComplete = outputEntries.Length == 2 && outputEntries.All(File.Exists) && outputEntries.Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(new[] { WorkbookContract.LookupFileName, WorkbookContract.ModelFileName }, StringComparer.Ordinal);
        var commitComplete = outputComplete && File.Exists(pairEvidencePath) && File.Exists(sealEvidencePath) && File.Exists(sealPath) && !File.Exists(pendingPath);
        if (commitComplete)
        {
            TryDeleteFile(pendingPath);
            return SafeResult.SuccessForHumanReview("workbook-publication-complete");
        }

        var hasS05State = outputEntries.Length != 0 || File.Exists(pairEvidencePath) || File.Exists(sealEvidencePath) || File.Exists(pendingPath) || (File.Exists(sealPath) && File.ReadAllText(sealPath) == "review-only-seal");
        if (!hasS05State) return SafeResult.SuccessForHumanReview("workbook-publication-clean");
        var allowedOutputNames = new HashSet<string>([WorkbookContract.ModelFileName, WorkbookContract.LookupFileName], StringComparer.Ordinal);
        if (outputEntries.Any(path => !File.Exists(path) || !allowedOutputNames.Contains(Path.GetFileName(path)))) return Blocked(BlockerCode.EvidenceSchemaInvalid, "WORKBOOK_PUBLICATION_RECOVERY_TARGET_INVALID");

        if (Directory.Exists(root.OutputPath)) Directory.Delete(root.OutputPath, true);
        Directory.CreateDirectory(root.OutputPath);
        TryDeleteFile(pairEvidencePath);
        TryDeleteFile(sealEvidencePath);
        if (File.Exists(sealPath) && File.ReadAllText(sealPath) == "review-only-seal") TryDeleteFile(sealPath);
        TryDeleteFile(pendingPath);
        RecoverJournal(root);
        return SafeResult.SuccessForHumanReview("workbook-publication-recovered");
    }

    private static void RecoverJournal(RunRoot root)
    {
        string[] stableKeys = ["pass-a", "pass-b", "reconciliation", "qualified-snapshot"];
        var validator = new EvidenceEnvelopeValidator();
        var keep = new List<string>(stableKeys.Length);
        foreach (var stableKey in stableKeys)
        {
            var evidencePath = Path.Combine(root.EvidencePath, stableKey + ".json");
            if (!File.Exists(evidencePath)) throw new InvalidDataException("WORKBOOK_PUBLICATION_RECOVERY_S04_EVIDENCE_MISSING");
            var json = File.ReadAllText(evidencePath);
            if (!validator.Validate(json).IsSuccess) throw new InvalidDataException("WORKBOOK_PUBLICATION_RECOVERY_S04_EVIDENCE_INVALID");
            keep.Add(json);
        }
        using var stream = new FileStream(root.JournalPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        var bytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, keep) + Environment.NewLine);
        stream.Write(bytes);
        stream.Flush(true);
    }

    private static bool DictionaryEqual(IReadOnlyDictionary<string, int> first, IReadOnlyDictionary<string, int> second) => first.Count == second.Count && first.All(item => second.TryGetValue(item.Key, out var value) && value == item.Value);
    private static string PendingPublicationPath(RunRoot root) => Path.Combine(root.RootPath, ".workbook-publication.pending.json");

    private static async Task<FileStream> AcquireRunLockAsync(RunRoot root, CancellationToken cancellationToken)
    {
        var path = Path.Combine(root.RootPath, ".run.lock");
        for (var attempt = 0; attempt < 100; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.Asynchronous | FileOptions.WriteThrough); }
            catch (IOException) when (attempt < 99) { await Task.Delay(10, cancellationToken); }
        }
        throw new IOException("RUN_BUSY");
    }

    private RunRoot? ResolveRun(Guid runId)
    {
        lock (_sync) if (_runs.TryGetValue(runId, out var current)) return current;
        var persisted = FindPersistedRun(runId);
        if (persisted is null) return null;
        lock (_sync) _runs.TryAdd(runId, persisted);
        return persisted;
    }

    private RunRoot? FindPersistedRun(Guid runId)
    {
        var runs = Path.Combine(_basePath, "runs");
        if (!Directory.Exists(runs)) return null;
        var roots = Directory.EnumerateDirectories(runs, runId.ToString("D"), SearchOption.AllDirectories).Take(2).ToArray();
        if (roots.Length != 1) return null;
        var root = roots[0];
        return new RunRoot(runId, root, Path.Combine(root, "audit"), Path.Combine(root, "evidence"), Path.Combine(root, "output"), Path.Combine(root, "run-journal.json"));
    }

    private static string ReadTargetAlias(RunRoot root)
    {
        var path = Path.Combine(root.RootPath, ".target-alias");
        if (!File.Exists(path)) return "fixture";
        var alias = File.ReadAllText(path);
        return IsTargetAlias(alias) ? alias : "fixture";
    }

    private static string SealPath(RunRoot root) => Path.Combine(root.RootPath, ".sealed");
    private static bool IsTargetAlias(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    private static string OutcomeToken(SafeResult outcome) => outcome.IsSuccess ? "HUMAN_REVIEW_REQUIRED" : outcome.Blocker?.Code switch
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
        _ => "EVIDENCE_SCHEMA_INVALID"
    };
    private static SafeResult MissingRun() => SafeResult.Blocked(new Blocker(BlockerCode.EvidenceSchemaInvalid, "diagnose", "RUN_NOT_FOUND", "Use a RunId produced by this local process.", "Stop diagnosis."));
    private static SafeResult Blocked(BlockerCode code, string reason) => SafeResult.Blocked(new Blocker(code, "evidence", reason, "Create a new append-only run with safe typed evidence.", "Do not overwrite or append to the sealed run."));
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
