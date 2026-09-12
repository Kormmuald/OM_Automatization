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
        var expected = new[]
        {
            new ExpectedManifest(0, "5feceb66ffc86f38d952786c6d696c79c2dbc239dd4e91b46729d73a27fb57e9", 2, "3771dcf24d74407b7106f3f85370caee66e0e37ea57ec777d8259898e3914808", "54eed5cfdee554e061e27acbf172a97dab8191d88428b9be8127b2482a497bd2", "0cb7a658f8b7a14a822993fd686c6926045545284e72a22fe01fdd92d5f03159", "1-10"),
            new ExpectedManifest(1, "6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b", 1, "556620b431cf9217da34773ac10e8fb8ceeb2c625232820e83d4aede4ecd3492", "556620b431cf9217da34773ac10e8fb8ceeb2c625232820e83d4aede4ecd3492", "556620b431cf9217da34773ac10e8fb8ceeb2c625232820e83d4aede4ecd3492", "1-10")
        };

        Assert(result.IsQualified && result.Manifests.Count == expected.Length, "Valid paging did not produce the expected number of canonical manifests.");
        for (var index = 0; index < expected.Length; index++)
        {
            var actual = result.Manifests[index];
            var vector = expected[index];
            Assert(actual.Ordinal == vector.Ordinal && actual.ProgressTokenDigest == vector.ProgressTokenDigest && actual.Count == vector.Count && actual.FirstIdentity == vector.FirstIdentityDigest && actual.LastIdentity == vector.LastIdentityDigest && actual.IdentityDigest == vector.IdentityDigest && actual.ResponseSizeBucket == vector.ResponseSizeBucket, $"Canonical page manifest {index} did not match its independent safe vector.");
        }
    }

    private sealed record ExpectedManifest(int Ordinal, string ProgressTokenDigest, int Count, string FirstIdentityDigest, string LastIdentityDigest, string IdentityDigest, string ResponseSizeBucket);

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
