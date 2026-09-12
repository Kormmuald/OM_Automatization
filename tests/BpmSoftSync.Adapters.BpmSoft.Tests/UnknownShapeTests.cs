using System.Text.Json;
using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class UnknownShapeTests
{
    public static void UnknownShapeUsesOnlyStructuralEnvelope()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "unknown-shape-safe.json")));
        var result = WorkspaceInventoryAdapter.AdaptUnknown(document.RootElement.GetProperty("workspaceItem"));
        Assert(result.InventoryItem is not null && result.InventoryItem.SupportStatus == SupportStatus.Unsupported && result.InventoryItem.Envelope is not null && !result.InventoryItem.Envelope.StructuralTree.Contains("RAW_LOOKUP_CANARY", StringComparison.Ordinal), "Unknown shape leaked raw scalar or was dropped.");
    }

    public static void IndexesUseOnlyColumnUIdAndRemainReadOnly()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "workspace-inventory.json")));
        var result = WorkspaceInventoryAdapter.ReadIndexes(document.RootElement.GetProperty("schema"));
        Assert(result.Blocker.Code == BlockerCode.IndexSyncUnresolved && result.Relations.Single().ColumnUIds.Single() == Guid.Parse("44444444-4444-4444-4444-444444444444"), "Index relation did not use columnUId only.");
    }

    public static void WorkspaceFixtureHasCompleteTypedStatusCoverage()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "workspace-inventory.json")));
        var inventory = WorkspaceInventoryAdapter.AdaptWorkspace(document.RootElement);
        Assert(inventory.IsQualified && inventory.Items.Count == 2 && inventory.Items.Select(item => item.SupportStatus).Distinct().Count() == 2 && inventory.Items.All(item => !item.Identity.ItemType.Contains("Shared label", StringComparison.Ordinal)), "Workspace fixture did not retain complete typed status coverage.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
