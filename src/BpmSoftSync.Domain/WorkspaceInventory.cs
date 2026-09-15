namespace BpmSoftSync.Domain;

public sealed record PackageLayerIdentity(Guid? PackageId, Guid? PackageUId, string? LayerKind, string? PackageName = null);

public sealed record WorkspaceItemIdentity(Guid WorkspaceItemUId, PackageLayerIdentity PackageLayer, string ItemType, Guid? SchemaUId = null);

public enum SupportStatus
{
    Structured,
    InventoryOnly,
    Unreadable,
    Unsupported
}

public sealed record LosslessShapeEnvelope(string TypeTag, string StructuralTree, string ScalarClassDigest, int ScalarCount, string SourceShapeDigest);

public sealed record WorkspaceInventoryItem(
    WorkspaceItemIdentity Identity,
    SupportStatus SupportStatus,
    string SafeReason,
    LosslessShapeEnvelope? Envelope,
    string? DisplayName = null,
    IReadOnlyList<LosslessShapeEnvelope>? UnknownProperties = null);

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

public enum ColumnOwnership { Own, Inherited }

public sealed record SchemaIdentity(
    string SchemaName,
    Guid SchemaUId,
    Guid? ServerSchemaIdCandidate,
    string? ParentSchemaName,
    Guid? ParentSchemaUId,
    PackageLayerIdentity PackageLayer);

public sealed record SchemaReference(string SchemaName, Guid SchemaUId);

public sealed record EntityColumnModel(
    string ColumnName,
    Guid ColumnUId,
    int Ordinal,
    ColumnOwnership Ownership,
    int TypeCode,
    int RequirementType,
    bool ActualIndexed,
    SchemaReference? Reference,
    IReadOnlyList<LosslessShapeEnvelope> UnknownProperties);

public sealed record IndexMember(Guid ColumnUId, int Ordinal, IReadOnlyList<LosslessShapeEnvelope> UnknownProperties);

public sealed record EntityIndexModel(
    Guid IndexUId,
    string Name,
    bool IsUnique,
    bool? AutoName,
    IReadOnlyList<IndexMember> Members,
    IReadOnlyList<LosslessShapeEnvelope> UnknownProperties);

public sealed record EntitySchemaModel(
    SchemaIdentity Identity,
    IReadOnlyList<EntityColumnModel> Columns,
    IReadOnlyList<EntityIndexModel> Indexes,
    IReadOnlyList<LosslessShapeEnvelope> UnknownProperties);

public sealed record WorkspaceObjectModel(
    bool IsQualified,
    WorkspaceInventory Inventory,
    IReadOnlyList<EntitySchemaModel> Schemas,
    Blocker? Blocker)
{
    public static WorkspaceObjectModel Blocked(WorkspaceInventory inventory, IReadOnlyList<EntitySchemaModel> schemas, Blocker blocker) =>
        new(false, inventory, schemas, blocker);
}
