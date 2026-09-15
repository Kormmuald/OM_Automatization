using BpmSoftSync.Adapters.FileSystem;

namespace BpmSoftSync.Adapters.FileSystem.Tests;
public static class RunStoreTests
{
    public static void RootsAreDatePartitionedUniqueAndNonOverwriting(string root)
    {
        var store = new AppendOnlyRunStore(root); var now = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var one = store.CreateRun(now); var two = store.CreateRun(now);
        Assert(one.RootPath.Contains(Path.Combine("runs", "2026", "09", "12"), StringComparison.Ordinal) && one.RootPath != two.RootPath, "Roots must be unique date partitions.");
        Assert(Directory.Exists(one.AuditPath) && Directory.Exists(one.EvidencePath) && Directory.Exists(one.OutputPath) && one.JournalPath.EndsWith("run-journal.json", StringComparison.Ordinal), "Run root does not expose the canonical S04 lifecycle layout.");
        var collision = false; try { store.CreateRun(now, one.RunId); } catch (IOException) { collision = true; }
        Assert(collision, "Existing root must not be overwritten.");
    }
    public static async Task SeparateStoreInstancesCannotClaimSameRunIdAsync(string root)
    {
        var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => { try { new AppendOnlyRunStore(root).CreateRun(now, id); return true; } catch (IOException) { return false; } })));
        Assert(attempts.Count(value => value) == 1, "Separate stores raced into the same append-only RunId root.");
    }

    public static async Task SealedRunRejectsFurtherEvidenceAsync(string root)
    {
        var store = new AppendOnlyRunStore(root); var runId = await store.BeginRunAsync("fixture");
        var seal = await store.SealAsync(runId, BpmSoftSync.Domain.SafeResult.SuccessForHumanReview("test"));
        var envelope = EvidenceEnvelopeTests.Envelope(runId, "late");
        var late = await store.AppendAsync(runId, envelope);
        Assert(seal.IsSuccess && !late.IsSuccess && late.Reason == "RUN_ALREADY_SEALED", "A sealed run accepted an append or overwrite.");
    }

    public static async Task PersistentStableKeysAndSealDefendAcrossStoreInstancesAsync(string root)
    {
        var first = new AppendOnlyRunStore(root); var runId = await first.BeginRunAsync("fixture");
        var second = new AppendOnlyRunStore(root);
        var claims = await Task.WhenAll(
            first.AppendAsync(runId, EvidenceEnvelopeTests.Envelope(runId, "pass-a")).AsTask(),
            second.AppendAsync(runId, EvidenceEnvelopeTests.Envelope(runId, "pass-a")).AsTask());
        Assert(claims.Count(result => result.IsSuccess) == 1 && claims.Single(result => !result.IsSuccess).Reason == "EVIDENCE_ALREADY_EXISTS", "Filesystem CreateNew did not atomically enforce a stable-key claim across store instances.");
        var overwrite = await second.AppendAsync(runId, EvidenceEnvelopeTests.Envelope(runId, "pass-a"));
        Assert(!overwrite.IsSuccess && overwrite.Reason == "EVIDENCE_ALREADY_EXISTS", "A second store did not observe the persistent stable-key claim.");
        var seal = await first.SealAsync(runId, BpmSoftSync.Domain.SafeResult.Blocked(new BpmSoftSync.Domain.Blocker(BpmSoftSync.Domain.BlockerCode.FullCatalogNotQualified, "test", "FULL_CATALOG_NOT_QUALIFIED", "Stop.", "Stop.")));
        var afterSeal = await second.AppendAsync(runId, EvidenceEnvelopeTests.Envelope(runId, "pass-b"));
        Assert(seal.IsSuccess && !afterSeal.IsSuccess && afterSeal.Reason == "RUN_ALREADY_SEALED", "A second store appended after the persistent terminal seal.");
    }
    public static async Task SameRunIdCannotRaceOrOverwriteAsync(string root)
    {
        var store = new AppendOnlyRunStore(root); var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var attempts = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => { try { store.CreateRun(now, id); return true; } catch (IOException) { return false; } })));
        Assert(attempts.Count(value => value) == 1, "Concurrent same-RunId creation was not rejected.");
    }
    public static void SameRunIdCannotBeClaimedOnAnotherDateByAnotherStore(string root)
    {
        var id = Guid.NewGuid();
        var firstDate = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var secondDate = firstDate.AddDays(1);
        new AppendOnlyRunStore(root).CreateRun(firstDate, id);
        var collision = false;
        try { new AppendOnlyRunStore(root).CreateRun(secondDate, id); }
        catch (IOException) { collision = true; }
        Assert(collision && Directory.EnumerateDirectories(Path.Combine(root, "runs"), id.ToString("D"), SearchOption.AllDirectories).Count() == 1, "RunId uniqueness was not enforced globally across date partitions and store instances.");
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
