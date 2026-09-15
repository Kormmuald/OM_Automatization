using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem;

public sealed class EvidenceEnvelopeValidator
{
    private static readonly HashSet<string> AllowedEnvelope = new(StringComparer.Ordinal) { "schema", "stableKey", "payloadDigest", "metadata" };
    private static readonly HashSet<string> AllowedMetadata = new(StringComparer.Ordinal) { "runId", "targetAlias", "phase", "outcome", "retryCount", "counts", "digests", "durationBucket", "scaleBucket", "responseSizeBuckets", "scopeContracts", "gateOutcomes" };
    private static readonly HashSet<string> AllowedStableKeys = new(StringComparer.Ordinal) { "pass-a", "pass-b", "reconciliation", "qualified-snapshot", "qualification-summary", "downstream-stage-probe", "workbook-pair", "blocked-terminal", "review-only-seal" };
    private static readonly HashSet<string> AllowedOutcomes = new(StringComparer.Ordinal) { "SAFE", "PASS_CAPTURED", "HUMAN_REVIEW_REQUIRED", "ENDPOINT_NOT_ALLOWLISTED", "CATALOG_ORDER_OR_PAGING_UNQUALIFIED", "UNKNOWN_SHAPE_UNQUALIFIED", "LEGACY_DISPOSITION_INVALID", "FULL_CATALOG_NOT_QUALIFIED", "TARGET_STATE_CHANGED_DURING_QUALIFICATION", "WORKBOOK_SCALE_DECISION_REQUIRED", "EVIDENCE_SCHEMA_INVALID", "EVIDENCE_SCAN_FAILED", "INDEX_SYNC_UNRESOLVED", "OFFLINE_FIXTURE_INVALID" };
    private static readonly HashSet<string> AllowedCountKeys = new(StringComparer.Ordinal) { "inventory", "workspaceItems", "schemas", "columns", "indexes", "indexMembers", "lookupRegistry", "lookupCollections", "lookupRows", "lookupValues", "pages" };
    private static readonly HashSet<string> AllowedDigestKeys = new(StringComparer.Ordinal) { "components", "manifests", "orderedIdentities", "readAttestation", "scope", "scopeContracts", "targetFingerprint", "unsupported", "passA", "passB", "pullWindow", "snapshot", "target", "sourceIdentity", "modelWorkbook", "lookupWorkbook", "pair" };
    private static readonly HashSet<string> AllowedDurationBuckets = new(StringComparer.Ordinal) { "not-recorded", "0-1s", "1-10s", "10-60s", "60s+" };
    private static readonly HashSet<string> AllowedScaleBuckets = new(StringComparer.Ordinal) { "not-recorded", "0-100", "101-1000", "1001-10000", "10001+" };
    private static readonly HashSet<string> AllowedResponseBuckets = new(StringComparer.Ordinal) { "empty", "0-16KiB", "16KiB-1MiB", "1-16MiB", "16MiB+" };
    private static readonly HashSet<string> AllowedGates = new(StringComparer.Ordinal) { "SAFE", "PASS_CAPTURED", "FULL_READ_COMPLETE", "PAIR_VALIDATED", "PAIR_PUBLISHED", "HUMAN_REVIEW_REQUIRED", "DIAGNOSTIC_ONLY", "S05_NOT_STARTED", "ENDPOINT_NOT_ALLOWLISTED", "CATALOG_ORDER_OR_PAGING_UNQUALIFIED", "UNKNOWN_SHAPE_UNQUALIFIED", "LEGACY_DISPOSITION_INVALID", "FULL_CATALOG_NOT_QUALIFIED", "TARGET_STATE_CHANGED_DURING_QUALIFICATION", "WORKBOOK_SCALE_DECISION_REQUIRED", "EVIDENCE_SCHEMA_INVALID", "EVIDENCE_SCAN_FAILED", "INDEX_SYNC_UNRESOLVED", "OFFLINE_FIXTURE_INVALID" };
    private readonly SecretValueScanner _scanner = new();

    public SafeResult Validate(string json)
    {
        var findings = _scanner.Scan(json, "evidence-envelope");
        if (findings.Count != 0) return Fail(BlockerCode.EvidenceScanFailed, "EVIDENCE_SCAN_FAILED");
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || doc.RootElement.EnumerateObject().Count() != AllowedEnvelope.Count || doc.RootElement.EnumerateObject().Any(p => !AllowedEnvelope.Contains(p.Name)) ||
                !doc.RootElement.TryGetProperty("schema", out var schema) || schema.GetString() != "EvidenceEnvelope/v1" ||
                !doc.RootElement.TryGetProperty("stableKey", out var stableKey) || stableKey.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(stableKey.GetString()) ||
                !doc.RootElement.TryGetProperty("payloadDigest", out var digest) || digest.ValueKind != JsonValueKind.String || !IsSha256(digest.GetString()) ||
                !doc.RootElement.TryGetProperty("metadata", out var metadata) || metadata.ValueKind != JsonValueKind.Object || metadata.EnumerateObject().Any(p => !AllowedMetadata.Contains(p.Name)) ||
                !AllowedStableKeys.Contains(stableKey.GetString()!) || !ValidateQualificationMetadata(metadata, stableKey.GetString()!)) return Fail(BlockerCode.EvidenceSchemaInvalid, "EVIDENCE_SCHEMA_INVALID");
        }
        catch (JsonException) { return Fail(BlockerCode.EvidenceSchemaInvalid, "EVIDENCE_SCHEMA_INVALID"); }
        return SafeResult.SuccessForHumanReview("evidence-envelope");
    }
    private static bool ValidateQualificationMetadata(JsonElement metadata, string stableKey)
    {
        if (metadata.EnumerateObject().Count() != AllowedMetadata.Count) return false;
        if (!metadata.TryGetProperty("runId", out var runId) || runId.ValueKind != JsonValueKind.String || !Guid.TryParse(runId.GetString(), out var parsed) || parsed == Guid.Empty) return false;
        if (!metadata.TryGetProperty("targetAlias", out var aliasElement) || aliasElement.ValueKind != JsonValueKind.String || !IsTargetAlias(aliasElement.GetString())) return false;
        if (!metadata.TryGetProperty("phase", out var phase) || phase.ValueKind != JsonValueKind.String || !string.Equals(phase.GetString(), stableKey, StringComparison.Ordinal)) return false;
        if (!metadata.TryGetProperty("outcome", out var outcome) || outcome.ValueKind != JsonValueKind.String || !AllowedOutcomes.Contains(outcome.GetString()!)) return false;
        if (!metadata.TryGetProperty("retryCount", out var retry) || retry.ValueKind != JsonValueKind.Number || !retry.TryGetInt32(out var retryCount) || retryCount != 0) return false;
        if (!metadata.TryGetProperty("counts", out var counts) || counts.ValueKind != JsonValueKind.Object || counts.EnumerateObject().Any(item => !AllowedCountKeys.Contains(item.Name) || item.Value.ValueKind != JsonValueKind.Number || !item.Value.TryGetInt32(out var value) || value < 0)) return false;
        if (!metadata.TryGetProperty("digests", out var digests) || digests.ValueKind != JsonValueKind.Object || digests.EnumerateObject().Any(item => !AllowedDigestKeys.Contains(item.Name) || item.Value.ValueKind != JsonValueKind.String || !IsSha256(item.Value.GetString()))) return false;
        if (!metadata.TryGetProperty("durationBucket", out var duration) || duration.ValueKind != JsonValueKind.String || !AllowedDurationBuckets.Contains(duration.GetString()!)) return false;
        if (!metadata.TryGetProperty("scaleBucket", out var scale) || scale.ValueKind != JsonValueKind.String || !AllowedScaleBuckets.Contains(scale.GetString()!)) return false;
        if (!metadata.TryGetProperty("responseSizeBuckets", out var sizes) || sizes.ValueKind != JsonValueKind.Array || sizes.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String || !AllowedResponseBuckets.Contains(item.GetString()!))) return false;
        if (!metadata.TryGetProperty("scopeContracts", out var contracts) || contracts.ValueKind != JsonValueKind.Array || !ValidateScopeContracts(contracts)) return false;
        return metadata.TryGetProperty("gateOutcomes", out var gates) && gates.ValueKind == JsonValueKind.Array && gates.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String && AllowedGates.Contains(item.GetString()!));
    }
    private static bool ValidateScopeContracts(JsonElement contracts)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string[] expected = ["collectionId", "orderKeyId", "queryContractId", "pageSize", "maxPages", "maxRows", "maxResponseBytes"];
        foreach (var contract in contracts.EnumerateArray())
        {
            if (contract.ValueKind != JsonValueKind.Object || contract.EnumerateObject().Select(item => item.Name).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal) is false) return false;
            var collectionId = contract.GetProperty("collectionId").GetString();
            if (collectionId is null || !seen.Add(collectionId) || !MatchesClosedContract(collectionId, contract)) return false;
        }
        return true;
    }
    private static bool MatchesClosedContract(string collectionId, JsonElement contract)
    {
        if (contract.GetProperty("orderKeyId").ValueKind != JsonValueKind.String || contract.GetProperty("queryContractId").ValueKind != JsonValueKind.String) return false;
        var orderKeyId = contract.GetProperty("orderKeyId").GetString();
        var queryContractId = contract.GetProperty("queryContractId").GetString();
        return collectionId switch
        {
            "workspace" => orderKeyId == "workspaceItemUId" && queryContractId == "GetWorkspaceItems/v1" && ExactLimits(contract, 64, 10_000, 10_000, 2L * 1024 * 1024 * 1024),
            "schemas" => orderKeyId == "schemaUId/packageLayer" && queryContractId == "GetSchema/v1" && ExactLimits(contract, 1, 10_000, 10_000, 2L * 1024 * 1024 * 1024),
            "lookup-registry" => orderKeyId == "Id" && queryContractId == "SelectQuery/lookup-registry/v1" && ExactLimits(contract, 500, 10_000, 5_000_000, 2L * 1024 * 1024 * 1024),
            _ when IsLookupCollectionToken(collectionId) => orderKeyId == "Id" && queryContractId == "SelectQuery/lookup-values/v1" && ExactLimits(contract, 500, 10_000, 5_000_000, 2L * 1024 * 1024 * 1024),
            _ => false
        };
    }
    private static bool ExactLimits(JsonElement contract, int pageSize, int maxPages, int maxRows, long maxResponseBytes) =>
        contract.GetProperty("pageSize").ValueKind == JsonValueKind.Number && contract.GetProperty("pageSize").TryGetInt32(out var actualPageSize) && actualPageSize == pageSize &&
        contract.GetProperty("maxPages").ValueKind == JsonValueKind.Number && contract.GetProperty("maxPages").TryGetInt32(out var actualMaxPages) && actualMaxPages == maxPages &&
        contract.GetProperty("maxRows").ValueKind == JsonValueKind.Number && contract.GetProperty("maxRows").TryGetInt32(out var actualMaxRows) && actualMaxRows == maxRows &&
        contract.GetProperty("maxResponseBytes").ValueKind == JsonValueKind.Number && contract.GetProperty("maxResponseBytes").TryGetInt64(out var actualMaxResponseBytes) && actualMaxResponseBytes == maxResponseBytes;
    private static bool IsLookupCollectionToken(string value) => value.StartsWith("lookup-sha256:", StringComparison.Ordinal) && IsSha256(value["lookup-sha256:".Length..]);
    private static bool IsTargetAlias(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    private static bool IsSha256(string? value) => value is { Length: 64 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static SafeResult Fail(BlockerCode code, string reason) => SafeResult.Blocked(new Blocker(code, "evidence", reason, "Remove the forbidden or unrecognized field and create a new append-only run.", "Do not seal a PASS artifact."));
}
