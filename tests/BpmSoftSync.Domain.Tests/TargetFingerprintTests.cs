using System.Text.Json;
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

    public static void SemanticallyEquivalentJsonPropertyOrdersProduceSameFingerprint()
    {
        const string firstSource = "{\"allowlistVersion\":\"ReadEndpointAllowlist/v1\",\"scopeDescriptorHash\":\"scope-hash\",\"observedTargetVersionEvidence\":[\"v1\"],\"workspace\":[{\"workspaceItemUId\":\"11111111-1111-1111-1111-111111111111\",\"packageLayerId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"itemType\":\"EntitySchema\",\"supportStatus\":\"Structured\"}],\"collections\":[{\"collectionId\":\"entities\",\"orderKeyId\":\"id\",\"count\":1,\"orderedIdentityDigest\":\"identity-a\",\"pageManifestDigest\":\"page-digest\"}]}";
        const string reorderedProperties = "{\"collections\":[{\"pageManifestDigest\":\"page-digest\",\"orderedIdentityDigest\":\"identity-a\",\"count\":1,\"orderKeyId\":\"id\",\"collectionId\":\"entities\"}],\"workspace\":[{\"supportStatus\":\"Structured\",\"itemType\":\"EntitySchema\",\"packageLayerId\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"workspaceItemUId\":\"11111111-1111-1111-1111-111111111111\"}],\"observedTargetVersionEvidence\":[\"v1\"],\"scopeDescriptorHash\":\"scope-hash\",\"allowlistVersion\":\"ReadEndpointAllowlist/v1\"}";

        Assert(TargetFingerprint.Create(InputFromJson(firstSource)).Digest == TargetFingerprint.Create(InputFromJson(reorderedProperties)).Digest, "Equivalent named JSON properties produced different TargetFingerprint/v1 digests.");
    }

    private static TargetFingerprintInput Input(string version, SupportStatus status, string identity, bool reverse = false, string pageDigest = "page-digest")
    {
        var workspace = new[] { new FingerprintWorkspaceEntry(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "EntitySchema", status), new FingerprintWorkspaceEntry(Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "EntitySchema", SupportStatus.InventoryOnly) };
        if (reverse) workspace = workspace.Reverse().ToArray();
        return new TargetFingerprintInput("ReadEndpointAllowlist/v1", "scope-hash", new[] { version }, workspace, Array.Empty<FingerprintSchemaEntry>(), new[] { new FingerprintCollectionEntry("entities", "id", 1, identity, pageDigest) }, Array.Empty<FingerprintUnsupportedEntry>());
    }

    private static TargetFingerprintInput InputFromJson(string source)
    {
        using var document = JsonDocument.Parse(source);
        var root = document.RootElement;
        var workspace = root.GetProperty("workspace").EnumerateArray().Select(entry => new FingerprintWorkspaceEntry(Guid.Parse(entry.GetProperty("workspaceItemUId").GetString()!), Guid.Parse(entry.GetProperty("packageLayerId").GetString()!), entry.GetProperty("itemType").GetString()!, Enum.Parse<SupportStatus>(entry.GetProperty("supportStatus").GetString()!, ignoreCase: false))).ToArray();
        var collections = root.GetProperty("collections").EnumerateArray().Select(entry => new FingerprintCollectionEntry(entry.GetProperty("collectionId").GetString()!, entry.GetProperty("orderKeyId").GetString()!, entry.GetProperty("count").GetInt32(), entry.GetProperty("orderedIdentityDigest").GetString()!, entry.GetProperty("pageManifestDigest").GetString()!)).ToArray();
        return new TargetFingerprintInput(root.GetProperty("allowlistVersion").GetString()!, root.GetProperty("scopeDescriptorHash").GetString()!, root.GetProperty("observedTargetVersionEvidence").EnumerateArray().Select(entry => entry.GetString()!).ToArray(), workspace, Array.Empty<FingerprintSchemaEntry>(), collections, Array.Empty<FingerprintUnsupportedEntry>());
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
