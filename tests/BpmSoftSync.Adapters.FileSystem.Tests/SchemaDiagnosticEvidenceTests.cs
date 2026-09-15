using System.Text.Json;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem.Tests;

public static class SchemaDiagnosticEvidenceTests
{
    public static async Task TerminalRecordUsesOnlyTheClosedContractAndReadBackSealAsync(string root)
    {
        var store = new AppendOnlySchemaDiagnosticEvidenceStore(root);
        var terminal = Terminal();
        var write = await store.SealTerminalAsync("fixture", terminal);
        if (!write.PersistenceResult.IsSuccess || write.EvidenceToken is null) throw new InvalidOperationException("The diagnostic terminal was not sealed.");
        var read = store.ReadBack(write.EvidenceToken, out var evidence);
        if (!read.IsSuccess || evidence is null || evidence.Outcome != SchemaDiagnosticTerminalOutcome.UnknownShapeUnqualified || evidence.FailedShape?.Path != FailedShapePath.SchemaId) throw new InvalidOperationException("The sealed diagnostic terminal did not validate on read-back.");
        var run = Directory.EnumerateDirectories(Path.Combine(root, "diagnostic-runs"), write.EvidenceToken, SearchOption.AllDirectories).Single();
        var names = Directory.EnumerateFileSystemEntries(run).Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!names.SequenceEqual([".sealed", "diagnostic-terminal.json"], StringComparer.Ordinal)) throw new InvalidOperationException("Diagnostic evidence created a normal journal, output, or other durable artifact.");
        var text = File.ReadAllText(Path.Combine(run, "diagnostic-terminal.json"));
        if (text.Contains("RAW_LOOKUP_CANARY", StringComparison.Ordinal) || text.Contains("password", StringComparison.OrdinalIgnoreCase) || text.Contains("cookie", StringComparison.OrdinalIgnoreCase) || text.Contains("http", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Diagnostic evidence leaked a data or secret canary.");
        File.WriteAllText(Path.Combine(run, ".sealed"), "tampered");
        if (store.ReadBack(write.EvidenceToken, out _).IsSuccess) throw new InvalidOperationException("Diagnostic evidence accepted a tampered seal.");
    }

    public static async Task UniqueRootsAndClosedSchemaRejectCanariesAsync(string root)
    {
        var store = new AppendOnlySchemaDiagnosticEvidenceStore(root);
        var first = await store.SealTerminalAsync("fixture", Terminal());
        var second = await store.SealTerminalAsync("fixture", Terminal());
        if (first.EvidenceToken is null || second.EvidenceToken is null || first.EvidenceToken == second.EvidenceToken) throw new InvalidOperationException("Diagnostic evidence roots are not unique.");
        var evidence = new SchemaDiagnosticTerminalEvidence(SchemaDiagnosticTerminalEvidence.SchemaVersion, "fixture", SchemaDiagnosticTerminalEvidence.RouteVersion, SchemaDiagnosticTerminalOutcome.UnknownShapeUnqualified, null);
        var json = JsonSerializer.Serialize(evidence, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var validator = new SchemaDiagnosticEvidenceValidator();
        if (!validator.Validate(json).IsSuccess || validator.Validate(json[..^1] + ",\"raw\":\"RAW_LOOKUP_CANARY\"}").IsSuccess || validator.Validate(json.Replace("\"targetAlias\":\"fixture\"", "\"targetAlias\":\"password-canary\"", StringComparison.Ordinal)).IsSuccess) throw new InvalidOperationException("Diagnostic evidence allowlist or secret scanner accepted an unsafe record.");
        var rejected = await store.SealTerminalAsync("fixture", SafeResult.Blocked(new Blocker(BlockerCode.UnknownShapeUnqualified, "schema", "unstructured terminal text", "RAW_LOOKUP_CANARY", "Stop.")));
        if (rejected.PersistenceResult.IsSuccess || rejected.EvidenceToken is not null) throw new InvalidOperationException("Diagnostic evidence persisted an unclosed terminal category.");

        var noCandidate = await store.SealTerminalAsync("fixture", SafeResult.Blocked(new Blocker(BlockerCode.FullCatalogNotQualified, "schema-diagnostic-candidate", "SCHEMA_DIAGNOSTIC_NO_SCHEMA_CANDIDATE", "Inspect the typed workspace inventory.", "Stop.")));
        if (!noCandidate.PersistenceResult.IsSuccess || noCandidate.EvidenceToken is null || !store.ReadBack(noCandidate.EvidenceToken, out var noCandidateEvidence).IsSuccess || noCandidateEvidence?.Outcome != SchemaDiagnosticTerminalOutcome.NoSchemaCandidate || noCandidateEvidence.FailedShape is not null) throw new InvalidOperationException("The closed no-schema-candidate terminal was not sealed without data-bearing shape evidence.");
    }

    public static async Task FailedOrCancelledStagingNeverPublishesAnUnsealedTerminalAsync(string root)
    {
        var faultRoot = Path.Combine(root, "fault-injection");
        var faultedStore = new AppendOnlySchemaDiagnosticEvidenceStore(
            faultRoot,
            (step, _) => step == SchemaDiagnosticEvidencePersistenceStep.AfterTerminalWrite
                ? ValueTask.FromException(new IOException("injected terminal-write boundary fault"))
                : ValueTask.CompletedTask);
        var faulted = await faultedStore.SealTerminalAsync("fixture", Terminal());
        if (faulted.PersistenceResult.IsSuccess || faulted.EvidenceToken is not null) throw new InvalidOperationException("A faulted staged diagnostic was reported as published.");
        AssertNoPublishedOrStagingArtifact(faultRoot, "A fault after terminal write exposed a final or leftover staging artifact.");

        var cancelledRoot = Path.Combine(root, "cancellation-injection");
        var cancelledStore = new AppendOnlySchemaDiagnosticEvidenceStore(
            cancelledRoot,
            (step, _) => step == SchemaDiagnosticEvidencePersistenceStep.BeforeSealWrite
                ? ValueTask.FromException(new OperationCanceledException("injected before-seal cancellation"))
                : ValueTask.CompletedTask);
        try
        {
            await cancelledStore.SealTerminalAsync("fixture", Terminal());
            throw new InvalidOperationException("Cancellation before sealing was swallowed.");
        }
        catch (OperationCanceledException) { }
        AssertNoPublishedOrStagingArtifact(cancelledRoot, "Cancellation before seal exposed a final or leftover staging artifact.");
    }

    public static async Task CompanionGuidStatusIsClosedAndRestrictedToSchemaPackageIdAsync(string root)
    {
        var store = new AppendOnlySchemaDiagnosticEvidenceStore(Path.Combine(root, "companion-guid-status"));
        var passed = new FailedShapeDiagnostic(FailedShapePath.SchemaPackageId, ExpectedShapeCategory.GuidString, ObservedJsonKind.String, ArrayCardinalityBucket.NotApplicable, null, GuidStringPredicateStatus.Passed);
        var accepted = await store.SealTerminalAsync("fixture", Terminal(passed));
        if (!accepted.PersistenceResult.IsSuccess || accepted.EvidenceToken is null || !store.ReadBack(accepted.EvidenceToken, out var evidence).IsSuccess || evidence?.FailedShape?.CompanionGuidStringStatus != GuidStringPredicateStatus.Passed) throw new InvalidOperationException("The closed SchemaPackageId companion result was not sealed and read back.");

        var invalid = new FailedShapeDiagnostic(FailedShapePath.SchemaId, ExpectedShapeCategory.GuidString, ObservedJsonKind.String, ArrayCardinalityBucket.NotApplicable, null, GuidStringPredicateStatus.Passed);
        var rejected = await store.SealTerminalAsync("fixture", Terminal(invalid));
        if (rejected.PersistenceResult.IsSuccess || rejected.EvidenceToken is not null) throw new InvalidOperationException("A companion status outside SchemaPackageId crossed the terminal evidence seal.");

        var text = JsonSerializer.Serialize(new SchemaDiagnosticTerminalEvidence(SchemaDiagnosticTerminalEvidence.SchemaVersion, "fixture", SchemaDiagnosticTerminalEvidence.RouteVersion, SchemaDiagnosticTerminalOutcome.UnknownShapeUnqualified, passed), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var validator = new SchemaDiagnosticEvidenceValidator();
        if (!validator.Validate(text).IsSuccess || validator.Validate(text.Replace("\"companionGuidStringStatus\":0", "\"companionGuidStringStatus\":99", StringComparison.Ordinal)).IsSuccess || validator.Validate(text.Replace($"\"path\":{(int)FailedShapePath.SchemaPackageId}", $"\"path\":{(int)FailedShapePath.SchemaId}", StringComparison.Ordinal)).IsSuccess || text.Contains("opaque-package-key", StringComparison.Ordinal)) throw new InvalidOperationException("The companion status validator accepted an unsafe or unscoped terminal record.");
    }

    private static void AssertNoPublishedOrStagingArtifact(string root, string message)
    {
        var diagnosticRuns = Path.Combine(root, "diagnostic-runs");
        if (!Directory.Exists(diagnosticRuns)) return;
        var names = Directory.EnumerateDirectories(diagnosticRuns, "*", SearchOption.AllDirectories).Select(Path.GetFileName).ToArray();
        if (names.Any(name => name is not null && (name.StartsWith(".staging-", StringComparison.Ordinal) || (name.Length == 40 && name.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'))))) throw new InvalidOperationException(message);
    }

    private static SafeResult Terminal(FailedShapeDiagnostic? diagnostic = null) => SafeResult.Blocked(new Blocker(BlockerCode.UnknownShapeUnqualified, "schema", "SCHEMA_INVENTORY_UNQUALIFIED", "Capture only closed structure.", "Stop.", diagnostic ?? new FailedShapeDiagnostic(FailedShapePath.SchemaId, ExpectedShapeCategory.GuidString, ObservedJsonKind.Null, ArrayCardinalityBucket.NotApplicable, null)));
}
