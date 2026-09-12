using System.Security.Cryptography;
using System.Text;

namespace BpmSoftSync.Domain;

public sealed record PageManifest(int Ordinal, string ProgressTokenDigest, int Count, string? FirstIdentity, string? LastIdentity, string IdentityDigest, string ResponseSizeBucket)
{
    public static PageManifest Create(int ordinal, string cursorToken, IReadOnlyList<string> identities)
    {
        return new PageManifest(
            ordinal,
            Digest(cursorToken),
            identities.Count,
            identities.Count == 0 ? null : Digest(identities[0]),
            identities.Count == 0 ? null : Digest(identities[^1]),
            Digest(string.Join("\n", identities)),
            identities.Count switch { 0 => "empty", <= 10 => "1-10", <= 100 => "11-100", _ => "101+" });
    }

    public static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
