using System.Text.Json;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem.Tests;

public static class EvidenceEnvelopeTests
{
    private static readonly string Digest = new('a', 64);

    public static QualificationEvidenceEnvelope Envelope(Guid runId, string stableKey, string outcome = "SAFE") =>
        new(QualificationEvidenceEnvelope.SchemaVersion, stableKey, Digest,
            new QualificationEvidenceMetadata(runId, "fixture", stableKey, outcome, 0,
                new Dictionary<string, int> { ["lookupRows"] = 1 }, new Dictionary<string, string> { ["target"] = Digest },
                "0-1s", "0-100", ["0-16KiB"],
                [new EvidenceScopeContract("lookup-sha256:" + new string('b', 64), "Id", "SelectQuery/lookup-values/v1", 500, 10_000, 5_000_000, 2L * 1024 * 1024 * 1024)], [outcome]));

    public static async Task SchemaFailurePreventsDurableWriteAsync(string root)
    {
        var validator = new EvidenceEnvelopeValidator();
        if (validator.Validate("{\"schema\":\"EvidenceEnvelope/v1\",\"unexpected\":true,\"metadata\":{}}").IsSuccess) throw new InvalidOperationException("Schema failure was accepted by the validator.");
        var store = new AppendOnlyRunStore(root);
        var runId = await store.BeginRunAsync("fixture");
        var accepted = await store.AppendAsync(runId, Envelope(runId, "pass-a"));
        var runRoot = Directory.EnumerateDirectories(Path.Combine(root, "runs"), runId.ToString("D"), SearchOption.AllDirectories).Single();
        if (!accepted.IsSuccess || !File.Exists(Path.Combine(runRoot, "evidence", "pass-a.json")) || !File.ReadAllText(Path.Combine(runRoot, "run-journal.json")).Contains("pass-a", StringComparison.Ordinal))
            throw new InvalidOperationException("Validated evidence was not linked into the same append-only journal.");
    }

    public static async Task TypedCanaryAndSealScanPreventEveryDurableWriteAsync(string root)
    {
        var store = new AppendOnlyRunStore(root);
        var runId = await store.BeginRunAsync("fixture");
        var runRoot = Directory.EnumerateDirectories(Path.Combine(root, "runs"), runId.ToString("D"), SearchOption.AllDirectories).Single();
        var rejected = await store.AppendAsync(runId, Envelope(runId, "pass-a", "LOOKUP_VALUE_CANARY_6f58a715"));
        if (rejected.IsSuccess || File.Exists(Path.Combine(runRoot, "evidence", "pass-a.json"))) throw new InvalidOperationException("Typed evidence canary crossed scan-before-write.");
        var seal = await store.SealAsync(runId, SafeResult.Blocked(new Blocker(BlockerCode.FullCatalogNotQualified, "test", "LOOKUP_VALUE_CANARY_6f58a715", "Stop.", "Stop.")));
        if (!seal.IsSuccess || Directory.EnumerateFiles(root, "blocked-terminal.json", SearchOption.AllDirectories).All(path => !File.ReadAllText(path).Contains("FULL_CATALOG_NOT_QUALIFIED", StringComparison.Ordinal)))
            throw new InvalidOperationException("Seal did not map the unsafe raw reason to a safe fixed outcome.");
        if (Directory.EnumerateFiles(runRoot, "*", SearchOption.AllDirectories).Any(path => File.ReadAllText(path).Contains("LOOKUP_VALUE_CANARY_6f58a715", StringComparison.Ordinal)))
            throw new InvalidOperationException("Raw blocker reason crossed the seal boundary.");
    }

    public static void OrdinaryUnmarkedLookupValueIsRejectedInEveryStringBucket()
    {
        var validator = new EvidenceEnvelopeValidator();
        var baseline = Envelope(Guid.NewGuid(), "pass-a");
        const string ordinaryLookupValue = "NorthwindCustomer";
        var variants = new QualificationEvidenceEnvelope[]
        {
            baseline with { PayloadDigest = ordinaryLookupValue },
            baseline with { Metadata = baseline.Metadata with { Outcome = ordinaryLookupValue } },
            baseline with { Metadata = baseline.Metadata with { Digests = new Dictionary<string, string> { ["target"] = ordinaryLookupValue } } },
            baseline with { Metadata = baseline.Metadata with { DurationBucket = ordinaryLookupValue } },
            baseline with { Metadata = baseline.Metadata with { ScaleBucket = ordinaryLookupValue } },
            baseline with { Metadata = baseline.Metadata with { ResponseSizeBuckets = [ordinaryLookupValue] } },
            baseline with { Metadata = baseline.Metadata with { GateOutcomes = [ordinaryLookupValue] } },
            baseline with { Metadata = baseline.Metadata with { ScopeContracts = [baseline.Metadata.ScopeContracts[0] with { CollectionId = ordinaryLookupValue }] } },
            baseline with { Metadata = baseline.Metadata with { ScopeContracts = [baseline.Metadata.ScopeContracts[0] with { OrderKeyId = ordinaryLookupValue }] } },
            baseline with { Metadata = baseline.Metadata with { ScopeContracts = [baseline.Metadata.ScopeContracts[0] with { QueryContractId = ordinaryLookupValue }] } }
        };
        foreach (var variant in variants)
        {
            var json = JsonSerializer.Serialize(variant, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            if (validator.Validate(json).IsSuccess) throw new InvalidOperationException("An unmarked ordinary lookup value crossed strict field-level validation.");
        }
    }

    public static void FailedShapeEvidenceAcceptsOnlyClosedEnums()
    {
        var diagnostic = new FailedShapeDiagnostic(FailedShapePath.SchemaIndexColumnUId, ExpectedShapeCategory.GuidString, ObservedJsonKind.Missing, ArrayCardinalityBucket.One, 0);
        var runId = Guid.NewGuid();
        var envelope = Envelope(runId, "reconciliation");
        envelope = envelope with { Metadata = envelope.Metadata with { FailedShape = diagnostic } };
        var json = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        if (!new EvidenceEnvelopeValidator().Validate(json).IsSuccess || json.Contains("S02-RAW-CANARY", StringComparison.Ordinal) || json.Contains("Account", StringComparison.Ordinal)) throw new InvalidOperationException("Closed failed-shape evidence was rejected or leaked a raw value.");
        var unsafeJson = json.Replace($"\"path\":{(int)diagnostic.Path}", "\"path\":999", StringComparison.Ordinal);
        if (new EvidenceEnvelopeValidator().Validate(unsafeJson).IsSuccess) throw new InvalidOperationException("Unrecognized failed-shape path crossed evidence validation.");

        var packageDiagnostic = new FailedShapeDiagnostic(FailedShapePath.SchemaPackageId, ExpectedShapeCategory.GuidString, ObservedJsonKind.String, ArrayCardinalityBucket.NotApplicable, null, GuidStringPredicateStatus.Passed);
        var packageJson = JsonSerializer.Serialize(envelope with { Metadata = envelope.Metadata with { FailedShape = packageDiagnostic } }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var outsidePackageJson = JsonSerializer.Serialize(envelope with { Metadata = envelope.Metadata with { FailedShape = packageDiagnostic with { Path = FailedShapePath.SchemaId } } }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        if (new EvidenceEnvelopeValidator().Validate(packageJson).IsSuccess || new EvidenceEnvelopeValidator().Validate(outsidePackageJson).IsSuccess) throw new InvalidOperationException("The bounded companion status crossed the generic qualification evidence validator.");
    }
}
