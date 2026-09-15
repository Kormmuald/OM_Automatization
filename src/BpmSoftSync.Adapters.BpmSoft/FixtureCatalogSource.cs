using System.Security.Cryptography;
using System.Text.Json;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed class FixtureCatalogSource(string fixturePath) : ICatalogSource
{
    private readonly string _fixturePath = Path.GetFullPath(fixturePath);
    public int ReadCount { get; private set; }

    public ValueTask<CatalogPassInput> ReadPassAsync(string exactTargetAlias, string scopeDescriptorHash, int passNumber, CancellationToken cancellationToken = default)
    {
        VerifyManifest(_fixturePath);
        using var document = JsonDocument.Parse(File.ReadAllText(_fixturePath));
        var root = document.RootElement;
        if (root.GetProperty("classification").GetString() != "sanitized") throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
        ReadCount++;
        var pages = root.TryGetProperty("pages", out var pageSource)
            ? pageSource.EnumerateArray().Select(page => new CatalogPage(page.GetProperty("cursor").GetString() ?? string.Empty, page.GetProperty("identities").EnumerateArray().Select(identity => identity.GetString() ?? string.Empty).ToArray(), page.GetProperty("terminal").GetBoolean())).ToArray()
            : new[] { new CatalogPage("0", root.TryGetProperty("workspaceItemIds", out var ids) ? ids.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray() : ["fixture-item"], true) };
        var version = root.TryGetProperty("passA", out var passA) && root.TryGetProperty("passB", out var passB)
            ? new[] { (passNumber == 1 ? passA : passB).GetString() ?? "fixture-version" }
            : new[] { "fixture-version" };
        var target = root.TryGetProperty("targetAlias", out var alias) ? alias.GetString() ?? "fixture" : "fixture";
        var scope = root.TryGetProperty("scopeHash", out var scopeValue) ? scopeValue.GetString() ?? "fixture-scope" : "fixture-scope";
        if (ReadCount > 1 && (!string.Equals(target, exactTargetAlias, StringComparison.Ordinal) || !string.Equals(scope, scopeDescriptorHash, StringComparison.Ordinal))) throw new InvalidDataException("TARGET_SCOPE_CHANGED_DURING_QUALIFICATION");
        return ValueTask.FromResult(new CatalogPassInput(root.GetProperty("fixtureId").GetString() ?? "fixture", target, scope, version, new CollectionDefinition("fixture-catalog", "fixture-id", 32), pages, [], [], [], root.TryGetProperty("declaredWorkbookLimit", out var limit) ? limit.GetInt32() : pages.Sum(page => page.Identities.Count)));
    }

    public static void VerifyManifest(string fixturePath)
    {
        var directory = Path.GetDirectoryName(fixturePath) ?? throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
        var manifestPath = Path.Combine(directory, "fixture-manifest.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (manifest.RootElement.GetProperty("schema").GetString() != "SanitizedFixtureManifest/v1") throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
        var entries = manifest.RootElement.GetProperty("fixtures").EnumerateArray().ToArray();
        var names = entries.Select(entry => entry.GetProperty("file").GetString() ?? string.Empty).ToArray();
        if (names.Length == 0 || names.Distinct(StringComparer.Ordinal).Count() != names.Length || entries.Select(entry => entry.GetProperty("id").GetString() ?? string.Empty).Distinct(StringComparer.Ordinal).Count() != entries.Length) throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
        var actualNames = Directory.EnumerateFiles(directory, "*.json").Select(Path.GetFileName).Where(name => !string.Equals(name, "fixture-manifest.json", StringComparison.Ordinal)).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        if (!names.OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(actualNames, StringComparer.Ordinal) || !names.Contains(Path.GetFileName(fixturePath), StringComparer.Ordinal)) throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
        foreach (var entry in entries)
        {
            var name = entry.GetProperty("file").GetString() ?? string.Empty;
            var expected = entry.GetProperty("sha256").GetString() ?? string.Empty;
            if (Path.GetFileName(name) != name || entry.GetProperty("classification").GetString() != "sanitized" || !File.Exists(Path.Combine(directory, name))) throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, name)))).ToLowerInvariant();
            if (!string.Equals(actual, expected, StringComparison.Ordinal)) throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
        }
    }
}
