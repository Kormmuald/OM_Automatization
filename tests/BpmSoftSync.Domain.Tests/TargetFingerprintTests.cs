using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;

public static class TargetFingerprintTests
{
    public static void PropertyOrderDoesNotChangeDigestButContractDataDoes()
    {
        var source = Input("v1", SupportStatus.Structured, "identity-a");
        var reordered = Input("v1", SupportStatus.Structured, "identity-a", reverse: true);
        var changedIdentity = Input("v1", SupportStatus.Structured, "identity-b");
        var changedPage = Input("v1", SupportStatus.Structured, "identity-a", pageDigest: "other-page-digest");
        var changedStatus = Input("v1", SupportStatus.Unsupported, "identity-a");
        var changedVersion = Input("v2", SupportStatus.Structured, "identity-a");
        var digest = TargetFingerprint.Create(source).Digest;
        Assert(digest == TargetFingerprint.Create(reordered).Digest, "Canonical digest depends on source order.");
        Assert(digest != TargetFingerprint.Create(changedIdentity).Digest && digest != TargetFingerprint.Create(changedPage).Digest && digest != TargetFingerprint.Create(changedStatus).Digest && digest != TargetFingerprint.Create(changedVersion).Digest, "Contract change did not alter fingerprint.");
    }

    private static TargetFingerprintInput Input(string version, SupportStatus status, string identity, bool reverse = false, string pageDigest = "page-digest")
    {
        var workspace = new[] { new FingerprintWorkspaceEntry(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "EntitySchema", status), new FingerprintWorkspaceEntry(Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "EntitySchema", SupportStatus.InventoryOnly) };
        if (reverse) workspace = workspace.Reverse().ToArray();
        return new TargetFingerprintInput("ReadEndpointAllowlist/v1", "scope-hash", new[] { version }, workspace, Array.Empty<FingerprintSchemaEntry>(), new[] { new FingerprintCollectionEntry("entities", "id", 1, identity, pageDigest) }, Array.Empty<FingerprintUnsupportedEntry>());
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
