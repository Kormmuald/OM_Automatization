using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BpmSoftSync.Domain;

public sealed record FingerprintWorkspaceEntry(Guid WorkspaceItemUId, Guid PackageLayerId, string ItemType, SupportStatus SupportStatus);
public sealed record FingerprintSchemaEntry(Guid SchemaUId, Guid PackageLayerId, string CanonicalMetadataHash);
public sealed record FingerprintCollectionEntry(string CollectionId, string OrderKeyId, int Count, string OrderedIdentityDigest, string PageManifestDigest);
public sealed record FingerprintUnsupportedEntry(string StableIdentity, string TypeTag, string LosslessShapeDigest, SupportStatus SupportStatus);
public sealed record TargetFingerprintInput(string AllowlistVersion, string ScopeDescriptorHash, IReadOnlyList<string> ObservedTargetVersionEvidence, IReadOnlyList<FingerprintWorkspaceEntry> Workspace, IReadOnlyList<FingerprintSchemaEntry> StructuredSchemas, IReadOnlyList<FingerprintCollectionEntry> Collections, IReadOnlyList<FingerprintUnsupportedEntry> Unsupported);
public sealed record TargetFingerprint(string Schema, string Digest, IReadOnlyList<string> ObservedTargetVersionEvidence)
{
    public const string SchemaVersion = "TargetFingerprint/v1";

    public static TargetFingerprint Create(TargetFingerprintInput input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.AllowlistVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ScopeDescriptorHash);
        var canonical = CanonicalJson(input);
        return new TargetFingerprint(SchemaVersion, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant(), input.ObservedTargetVersionEvidence.OrderBy(Normalize, StringComparer.Ordinal).ToArray());
    }

    // Decision 2: a compact canonical JSON preimage.  It intentionally contains digests and
    // typed identity only; no response bodies, lookup values, credentials, or session data.
    private static string CanonicalJson(TargetFingerprintInput input)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteString("allowlistVersion", Normalize(input.AllowlistVersion));
            writer.WriteStartArray("collections");
            foreach (var item in input.Collections.OrderBy(item => Normalize(item.CollectionId), StringComparer.Ordinal).ThenBy(item => Normalize(item.OrderKeyId), StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteNumber("count", item.Count); writer.WriteString("collectionId", Normalize(item.CollectionId)); writer.WriteString("orderKeyId", Normalize(item.OrderKeyId)); writer.WriteString("orderedIdentityDigest", Normalize(item.OrderedIdentityDigest)); writer.WriteString("pageManifestDigest", Normalize(item.PageManifestDigest)); writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("observedTargetVersionEvidence");
            foreach (var version in input.ObservedTargetVersionEvidence.DefaultIfEmpty("TARGET_VERSION_METADATA_UNAVAILABLE").Select(Normalize).OrderBy(value => value, StringComparer.Ordinal)) writer.WriteStringValue(version);
            writer.WriteEndArray();
            writer.WriteString("schema", SchemaVersion);
            writer.WriteString("scopeDescriptorHash", Normalize(input.ScopeDescriptorHash));
            writer.WriteStartArray("structuredSchemas");
            foreach (var item in input.StructuredSchemas.OrderBy(item => GuidText(item.SchemaUId), StringComparer.Ordinal).ThenBy(item => GuidText(item.PackageLayerId), StringComparer.Ordinal))
            { writer.WriteStartObject(); writer.WriteString("canonicalMetadataHash", Normalize(item.CanonicalMetadataHash)); writer.WriteString("packageLayerId", GuidText(item.PackageLayerId)); writer.WriteString("schemaUId", GuidText(item.SchemaUId)); writer.WriteEndObject(); }
            writer.WriteEndArray();
            writer.WriteStartArray("unsupported");
            foreach (var item in input.Unsupported.OrderBy(item => Normalize(item.StableIdentity), StringComparer.Ordinal).ThenBy(item => Normalize(item.TypeTag), StringComparer.Ordinal))
            { writer.WriteStartObject(); writer.WriteString("losslessShapeDigest", Normalize(item.LosslessShapeDigest)); writer.WriteString("stableIdentity", Normalize(item.StableIdentity)); writer.WriteString("supportStatus", item.SupportStatus.ToString()); writer.WriteString("typeTag", Normalize(item.TypeTag)); writer.WriteEndObject(); }
            writer.WriteEndArray();
            writer.WriteStartArray("workspace");
            foreach (var item in input.Workspace.OrderBy(item => GuidText(item.WorkspaceItemUId), StringComparer.Ordinal).ThenBy(item => GuidText(item.PackageLayerId), StringComparer.Ordinal))
            { writer.WriteStartObject(); writer.WriteString("itemType", Normalize(item.ItemType)); writer.WriteString("packageLayerId", GuidText(item.PackageLayerId)); writer.WriteString("supportStatus", item.SupportStatus.ToString()); writer.WriteString("workspaceItemUId", GuidText(item.WorkspaceItemUId)); writer.WriteEndObject(); }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string GuidText(Guid value) => value.ToString("D").ToLowerInvariant();
    private static string Normalize(string value) => value.Normalize(NormalizationForm.FormC);
}
