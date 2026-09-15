using System.Security.Cryptography;
using System.Text;

namespace BpmSoftSync.Adapters.FileSystem;

public sealed record ScanFinding(string Category, string Location, string Digest);

public sealed class SecretValueScanner
{
    private static readonly string[] Forbidden = ["password", "cookie", "csrf", "login", "rawlookup", "raw_lookup", "lookup_value_canary", "authorization:", "authorization bearer", "token="];
    public IReadOnlyList<ScanFinding> Scan(string content, string location)
    {
        var findings = new List<ScanFinding>();
        foreach (var marker in Forbidden)
            if (content.Contains(marker, StringComparison.OrdinalIgnoreCase))
                findings.Add(new(marker.ToUpperInvariant() + "_MARKER", location, Digest(marker)));
        return findings;
    }
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
