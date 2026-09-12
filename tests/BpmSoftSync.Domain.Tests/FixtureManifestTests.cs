using System.Security.Cryptography;
using System.Text.Json;

namespace BpmSoftSync.Domain.Tests;

public static class FixtureManifestTests
{
    public static void EveryApprovedFixtureHasMatchingSha256Digest()
    {
        var fixtureDirectory = Path.Combine("tests", "fixtures", "read-only");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(fixtureDirectory, "fixture-manifest.json")));
        var entries = document.RootElement.GetProperty("fixtures").EnumerateArray().ToArray();

        Assert(entries.Length > 0, "Fixture manifest has no approved fixtures.");
        Assert(entries.Select(entry => entry.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count() == entries.Length, "Fixture manifest contains duplicate identifiers.");
        Assert(entries.Select(entry => entry.GetProperty("file").GetString()).Distinct(StringComparer.Ordinal).Count() == entries.Length, "Fixture manifest contains duplicate file entries.");

        foreach (var entry in entries)
        {
            var fileName = entry.GetProperty("file").GetString() ?? string.Empty;
            var expectedDigest = entry.GetProperty("sha256").GetString() ?? string.Empty;
            Assert(entry.GetProperty("classification").GetString() == "sanitized", $"Fixture '{fileName}' is not marked sanitized.");
            Assert(fileName.Length > 0 && Path.GetFileName(fileName) == fileName, "Fixture manifest file path must remain within the read-only fixture directory.");
            Assert(expectedDigest.Length == 64 && expectedDigest.All(character => Uri.IsHexDigit(character)) && expectedDigest == expectedDigest.ToLowerInvariant(), $"Fixture '{fileName}' has no canonical lowercase SHA-256 digest.");
            var actualDigest = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(fixtureDirectory, fileName)))).ToLowerInvariant();
            Assert(actualDigest == expectedDigest, $"Fixture '{fileName}' does not match its manifest SHA-256 digest.");
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
