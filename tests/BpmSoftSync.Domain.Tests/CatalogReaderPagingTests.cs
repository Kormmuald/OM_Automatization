using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;

public static class CatalogReaderPagingTests
{
    public static void EveryAdversarialFixtureTerminatesWithNamedBlocker()
    {
        foreach (var name in new[] { "paging-duplicate", "paging-overlap", "paging-gap", "paging-empty-middle", "paging-nonempty-after-terminal", "paging-loop" })
        {
            var result = OrderedCatalogReader.Read(new CollectionDefinition("fixture", "fixture-id", 4), Pages(name));
            Assert(!result.IsQualified && result.Blocker?.Code == BlockerCode.CatalogOrderOrPagingUnqualified, name + " did not fail closed.");
        }

        var maxPage = OrderedCatalogReader.Read(new CollectionDefinition("fixture", "fixture-id", 2), Pages("paging-max-page"));
        Assert(!maxPage.IsQualified && maxPage.Blocker?.Code == BlockerCode.CatalogOrderOrPagingUnqualified, "Max-page guard did not fail closed.");
    }

    public static void ValidPagingHasCanonicalSafeManifests()
    {
        var result = OrderedCatalogReader.Read(new CollectionDefinition("fixture", "fixture-id", 4), Pages("paging-valid"));
        Assert(result.IsQualified && result.Manifests.Count == 2 && result.Manifests.All(manifest => manifest.IdentityDigest.Length == 64 && manifest.ProgressTokenDigest.Length == 64), "Valid paging did not produce canonical manifests.");
    }

    private static IReadOnlyList<CatalogPage> Pages(string name)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", name + ".json")));
        return document.RootElement.GetProperty("pages").EnumerateArray().Select(page => new CatalogPage(page.GetProperty("cursor").GetString()!, page.GetProperty("identities").EnumerateArray().Select(identity => identity.GetString()!).ToArray(), page.GetProperty("terminal").GetBoolean())).ToArray();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
