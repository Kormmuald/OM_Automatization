namespace BpmSoftSync.Domain;

public sealed record PackageLayerIdentity(Guid PackageId, Guid? PackageUId, string? LayerKind);

public sealed record WorkspaceItemIdentity(Guid WorkspaceItemUId, PackageLayerIdentity PackageLayer, string ItemType);

public enum SupportStatus
{
    Structured,
    InventoryOnly,
    Unreadable,
    Unsupported
}

public sealed record LosslessShapeEnvelope(string TypeTag, string StructuralTree, string ScalarClassDigest, int ScalarCount, string SourceShapeDigest);

public sealed record WorkspaceInventoryItem(WorkspaceItemIdentity Identity, SupportStatus SupportStatus, string SafeReason, LosslessShapeEnvelope? Envelope);

public sealed record WorkspaceInventory(bool IsQualified, IReadOnlyList<WorkspaceInventoryItem> Items, Blocker? Blocker)
{
    public static WorkspaceInventory Create(IReadOnlyList<WorkspaceInventoryItem> items)
    {
        if (items.Any(item => string.IsNullOrWhiteSpace(item.Identity.ItemType) || string.IsNullOrWhiteSpace(item.SafeReason)) ||
            items.Select(item => item.Identity).Distinct().Count() != items.Count ||
            items.Any(item => item.SupportStatus == SupportStatus.Unsupported && item.Envelope is null))
        {
            return new WorkspaceInventory(false, items, new Blocker(BlockerCode.UnknownShapeUnqualified, "workspace-inventory", "INVENTORY_UNQUALIFIED", "Preserve a typed identity, support status and structural envelope.", "Stop this qualification run."));
        }

        return new WorkspaceInventory(true, items, null);
    }
}

public sealed record IndexRelation(Guid IndexUId, IReadOnlyList<Guid> ColumnUIds);

public sealed record IndexReadResult(IReadOnlyList<IndexRelation> Relations, Blocker Blocker);
