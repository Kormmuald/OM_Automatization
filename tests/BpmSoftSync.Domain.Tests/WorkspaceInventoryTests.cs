using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;

public static class WorkspaceInventoryTests
{
    public static void PackageLayerIdentityDoesNotMergeDisplayNames()
    {
        var first = new WorkspaceItemIdentity(Guid.Parse("11111111-1111-1111-1111-111111111111"), new PackageLayerIdentity("opaque-first", null, "custom"), "EntitySchema");
        var second = new WorkspaceItemIdentity(Guid.Parse("22222222-2222-2222-2222-222222222222"), new PackageLayerIdentity("opaque-second", null, "base"), "EntitySchema");
        var inventory = WorkspaceInventory.Create(new[] { new WorkspaceInventoryItem(first, SupportStatus.Structured, "fixture", null), new WorkspaceInventoryItem(second, SupportStatus.InventoryOnly, "fixture", null) });
        Assert(inventory.IsQualified && inventory.Items.Count == 2 && inventory.Items.Select(item => item.Identity.PackageLayer.OpaquePackageIdDigest).Distinct().Count() == 2, "Package provenance values were merged.");
    }

    public static void EverySourceItemHasExactlyOneSupportStatus()
    {
        var id = new WorkspaceItemIdentity(Guid.Parse("11111111-1111-1111-1111-111111111111"), new PackageLayerIdentity("opaque-unknown", null, "custom"), "Unknown");
        var inventory = WorkspaceInventory.Create(new[] { new WorkspaceInventoryItem(id, SupportStatus.Unsupported, "unknown type", new LosslessShapeEnvelope("Unknown", "object{}", "a", 0, "b")) });
        Assert(inventory.IsQualified && inventory.Items.Single().SupportStatus == SupportStatus.Unsupported, "Item did not retain one support status.");
    }

    public static void TypedInventoryRetainsSchemaCandidateAndDisplayCollision()
    {
        var sharedDisplayName = "same-display-name";
        var first = new WorkspaceItemIdentity(Guid.Parse("11111111-1111-1111-1111-111111111111"), new PackageLayerIdentity("opaque-a", null, "custom", "Package A"), "EntitySchema", Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var second = new WorkspaceItemIdentity(Guid.Parse("22222222-2222-2222-2222-222222222222"), new PackageLayerIdentity("opaque-b", null, "base", "Package B"), "EntitySchema", Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var inventory = WorkspaceInventory.Create(new[]
        {
            new WorkspaceInventoryItem(first, SupportStatus.Structured, "source", null, sharedDisplayName),
            new WorkspaceInventoryItem(second, SupportStatus.InventoryOnly, "source", null, sharedDisplayName)
        });

        Assert(inventory.IsQualified && inventory.Items.Count == 2 && inventory.Items.Select(item => item.Identity.SchemaUId).Distinct().Count() == 2 && inventory.Items.Select(item => item.Identity.PackageLayer.PackageName).Distinct().Count() == 2, "Typed package/schema identities were merged by display name.");
    }

    public static void PackageUIdAloneDefinesPrimaryIdentity()
    {
        var packageUId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var first = new PackageLayerIdentity("opaque-first", packageUId, "custom", "Package A");
        var second = new PackageLayerIdentity("opaque-second", packageUId, "base", "Package B");

        Assert(first.PrimaryIdentityKey == packageUId.ToString("D") && second.PrimaryIdentityKey == packageUId.ToString("D"), "package.uId is not the sole primary package identity.");
        Assert(first.OpaquePackageIdDigest != second.OpaquePackageIdDigest && !first.PrimaryIdentityKey.Contains("opaque", StringComparison.Ordinal), "Opaque package provenance entered the primary identity key.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
