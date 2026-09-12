using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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
        var lines = new List<string>
        {
            "schema=" + SchemaVersion,
            "allowlist=" + Normalize(input.AllowlistVersion),
            "scope=" + Normalize(input.ScopeDescriptorHash)
        };
        Add(lines, "version", input.ObservedTargetVersionEvidence.DefaultIfEmpty("TARGET_VERSION_METADATA_UNAVAILABLE"));
        Add(lines, "workspace", input.Workspace.Select(entry => $"{GuidText(entry.WorkspaceItemUId)}|{GuidText(entry.PackageLayerId)}|{Normalize(entry.ItemType)}|{entry.SupportStatus}"));
        Add(lines, "schema", input.StructuredSchemas.Select(entry => $"{GuidText(entry.SchemaUId)}|{GuidText(entry.PackageLayerId)}|{Normalize(entry.CanonicalMetadataHash)}"));
        Add(lines, "collection", input.Collections.Select(entry => $"{Normalize(entry.CollectionId)}|{Normalize(entry.OrderKeyId)}|{entry.Count.ToString(CultureInfo.InvariantCulture)}|{Normalize(entry.OrderedIdentityDigest)}|{Normalize(entry.PageManifestDigest)}"));
        Add(lines, "unsupported", input.Unsupported.Select(entry => $"{Normalize(entry.StableIdentity)}|{Normalize(entry.TypeTag)}|{Normalize(entry.LosslessShapeDigest)}|{entry.SupportStatus}"));
        var canonical = string.Join("\n", lines);
        return new TargetFingerprint(SchemaVersion, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant(), input.ObservedTargetVersionEvidence.OrderBy(Normalize, StringComparer.Ordinal).ToArray());
    }

    private static void Add(List<string> lines, string category, IEnumerable<string> values) => lines.AddRange(values.Select(Normalize).OrderBy(value => value, StringComparer.Ordinal).Select(value => category + "=" + value));
    private static string GuidText(Guid value) => value.ToString("D").ToLowerInvariant();
    private static string Normalize(string value) => value.Normalize(NormalizationForm.FormC);
}
