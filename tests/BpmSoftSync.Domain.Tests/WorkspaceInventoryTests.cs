using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;

public static class WorkspaceInventoryTests
{
    public static void PackageLayerIdentityDoesNotMergeDisplayNames()
    {
        var first = new WorkspaceItemIdentity(Guid.Parse("11111111-1111-1111-1111-111111111111"), new PackageLayerIdentity(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, "custom"), "EntitySchema");
        var second = new WorkspaceItemIdentity(Guid.Parse("22222222-2222-2222-2222-222222222222"), new PackageLayerIdentity(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, "base"), "EntitySchema");
        var inventory = WorkspaceInventory.Create(new[] { new WorkspaceInventoryItem(first, SupportStatus.Structured, "fixture", null), new WorkspaceInventoryItem(second, SupportStatus.InventoryOnly, "fixture", null) });
        Assert(inventory.IsQualified && inventory.Items.Count == 2 && inventory.Items.Select(item => item.Identity.PackageLayer.PackageId).Distinct().Count() == 2, "Package layers were merged.");
    }

    public static void EverySourceItemHasExactlyOneSupportStatus()
    {
        var id = new WorkspaceItemIdentity(Guid.Parse("11111111-1111-1111-1111-111111111111"), new PackageLayerIdentity(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, "custom"), "Unknown");
        var inventory = WorkspaceInventory.Create(new[] { new WorkspaceInventoryItem(id, SupportStatus.Unsupported, "unknown type", new LosslessShapeEnvelope("Unknown", "object{}", "a", 0, "b")) });
        Assert(inventory.IsQualified && inventory.Items.Single().SupportStatus == SupportStatus.Unsupported, "Item did not retain one support status.");
    }

    public static void TypedInventoryRetainsSchemaCandidateAndDisplayCollision()
    {
        var sharedDisplayName = "same-display-name";
        var first = new WorkspaceItemIdentity(Guid.Parse("11111111-1111-1111-1111-111111111111"), new PackageLayerIdentity(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), null, "custom", "Package A"), "EntitySchema", Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var second = new WorkspaceItemIdentity(Guid.Parse("22222222-2222-2222-2222-222222222222"), new PackageLayerIdentity(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, "base", "Package B"), "EntitySchema", Guid.Parse("44444444-4444-4444-4444-444444444444"));
        var inventory = WorkspaceInventory.Create(new[]
        {
            new WorkspaceInventoryItem(first, SupportStatus.Structured, "source", null, sharedDisplayName),
            new WorkspaceInventoryItem(second, SupportStatus.InventoryOnly, "source", null, sharedDisplayName)
        });

        Assert(inventory.IsQualified && inventory.Items.Count == 2 && inventory.Items.Select(item => item.Identity.SchemaUId).Distinct().Count() == 2 && inventory.Items.Select(item => item.Identity.PackageLayer.PackageName).Distinct().Count() == 2, "Typed package/schema identities were merged by display name.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
