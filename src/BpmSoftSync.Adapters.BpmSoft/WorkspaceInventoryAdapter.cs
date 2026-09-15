using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed record UnknownShapeResult(WorkspaceInventoryItem? InventoryItem, Blocker? Blocker);

public static class WorkspaceInventoryAdapter
{
    private static readonly HashSet<string> WorkspaceProperties = new(StringComparer.Ordinal) { "workspaceItemUId", "schemaUId", "packageId", "packageUId", "packageName", "layerKind", "itemType", "displayName", "supportStatus", "payload" };
    private static readonly HashSet<string> IndexMemberProperties = new(StringComparer.Ordinal) { "columnUId" };
    private static readonly HashSet<string> ObservedWorkspaceProperties = new(StringComparer.Ordinal) { "uId", "name", "packageName", "type" };
    private static readonly HashSet<string> ObservedSchemaProperties = new(StringComparer.Ordinal) { "name", "uId", "id", "parentSchema", "package", "columns", "inheritedColumns", "indexes" };
    private static readonly HashSet<string> ObservedColumnProperties = new(StringComparer.Ordinal) { "name", "uId", "type", "requirementType", "indexed", "referenceSchema" };
    private static readonly HashSet<string> ObservedIndexProperties = new(StringComparer.Ordinal) { "uId", "name", "isUnique", "isAutoName", "autoName", "columns" };

    public static async Task<WorkspaceObjectModel> ReadFullAsync(BpmSoftReadTransport transport, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        using var workspaceResponse = await transport.GetWorkspaceItemsAsync(cancellationToken);
        var inventory = AdaptWorkspace(workspaceResponse.Root);
        if (!inventory.IsQualified) return WorkspaceObjectModel.Blocked(inventory, [], inventory.Blocker!);

        var schemas = new List<EntitySchemaModel>();
        foreach (var item in inventory.Items.Where(item => item.SupportStatus == SupportStatus.Structured && string.Equals(item.Identity.ItemType, "EntitySchema", StringComparison.Ordinal)))
        {
            var requestedSchemaUId = item.Identity.SchemaUId ?? item.Identity.WorkspaceItemUId;
            try
            {
                using var schemaResponse = await transport.GetSchemaAsync(requestedSchemaUId, cancellationToken);
                if (!TryAdaptSchema(schemaResponse.Root, out var schema, out var blocker)) return WorkspaceObjectModel.Blocked(inventory, schemas, blocker!);
                if (schema!.Identity.SchemaUId != requestedSchemaUId || !string.Equals(schema.Identity.SchemaName, item.DisplayName, StringComparison.Ordinal) || !string.Equals(schema.Identity.PackageLayer.PackageName, item.Identity.PackageLayer.PackageName, StringComparison.Ordinal))
                    return WorkspaceObjectModel.Blocked(inventory, schemas, UnknownBlocker("schema-identity", "SCHEMA_IDENTITY_MISMATCH"));
                schemas.Add(schema!);
            }
            catch (BpmSoftTransportException error)
            {
                var unreadable = item with { SupportStatus = SupportStatus.Unreadable, SafeReason = "SCHEMA_READ_UNAVAILABLE:" + error.Code };
                var preservedInventory = WorkspaceInventory.Create(inventory.Items.Select(candidate => candidate.Identity == item.Identity ? unreadable : candidate).ToArray());
                var blocker = new Blocker(BlockerCode.UnknownShapeUnqualified, "workspace-schema", "SCHEMA_READ_UNAVAILABLE", "Correct the read-only session or response failure, then start a new manual qualification run.", "Stop this qualification run; do not retry automatically.");
                return WorkspaceObjectModel.Blocked(preservedInventory, schemas, blocker);
            }
        }
        return new WorkspaceObjectModel(true, inventory, schemas, null);
    }

    public static WorkspaceInventory AdaptWorkspace(JsonElement root)
    {
        return root.TryGetProperty("items", out var observedItems) && observedItems.ValueKind == JsonValueKind.Array
            ? AdaptObservedWorkspace(observedItems)
            : InvalidInventory([]);
    }

    public static UnknownShapeResult AdaptUnknown(JsonElement item)
    {
        if (!TryGuid(item, "workspaceItemUId", out var workspaceItemUId) || !TryGuid(item, "packageId", out var packageId) || !TryRequiredString(item, "itemType", out var itemType) || !item.TryGetProperty("payload", out var payload)) return new UnknownShapeResult(null, UnknownBlocker("workspace-item", "UNKNOWN_SHAPE_ENVELOPE_REQUIRED"));
        var packageUId = TryGuid(item, "packageUId", out var parsedPackageUId) ? parsedPackageUId : (Guid?)null;
        var schemaUId = TryGuid(item, "schemaUId", out var parsedSchemaUId) ? parsedSchemaUId : (Guid?)null;
        var identity = new WorkspaceItemIdentity(workspaceItemUId, new PackageLayerIdentity(packageId, packageUId, TryOptionalString(item, "layerKind"), TryOptionalString(item, "packageName")), itemType, schemaUId);
        return new UnknownShapeResult(new WorkspaceInventoryItem(identity, SupportStatus.Unsupported, "UNKNOWN_SHAPE_INVENTORY_ONLY", BuildEnvelope(itemType, payload), TryOptionalString(item, "displayName"), UnknownProperties(item, WorkspaceProperties, "workspace-item-property")), null);
    }

    public static IndexReadResult ReadIndexes(JsonElement schema)
    {
        var relations = new List<IndexRelation>();
        if (schema.TryGetProperty("indexes", out var indexes) && indexes.ValueKind == JsonValueKind.Array)
        {
            foreach (var index in indexes.EnumerateArray())
            {
                if (!TryGuid(index, "uId", out var indexUId) || !index.TryGetProperty("columns", out var columns) || columns.ValueKind != JsonValueKind.Array || columns.EnumerateArray().Any(column => !TryGuid(column, "columnUId", out _))) return new IndexReadResult([], UnknownBlocker("indexes", "INDEX_INVENTORY_UNQUALIFIED"));
                relations.Add(new IndexRelation(indexUId, columns.EnumerateArray().Select(column => Guid.Parse(column.GetProperty("columnUId").GetString()!)).ToArray()));
            }
        }
        return new IndexReadResult(relations, new Blocker(BlockerCode.IndexSyncUnresolved, "indexes", "INDEX_SYNC_UNRESOLVED", "Index data is read-only inventory evidence.", "Do not plan, load, mutate, or apply indexes."));
    }

    private static WorkspaceInventory AdaptObservedWorkspace(JsonElement source)
    {
        var items = new List<WorkspaceInventoryItem>();
        foreach (var item in source.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !TryGuid(item, "uId", out var itemUId) || !TryRequiredString(item, "name", out var name) || !TryRequiredString(item, "packageName", out var packageName) || !item.TryGetProperty("type", out var type) || !type.TryGetInt32(out var typeCode)) return InvalidInventory(items);
            var status = typeCode == 3 ? SupportStatus.Structured : SupportStatus.InventoryOnly;
            var typeName = typeCode == 3 ? "EntitySchema" : "BPMSoftWorkspaceType:" + typeCode.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var identity = new WorkspaceItemIdentity(itemUId, new PackageLayerIdentity(null, null, "workspace-item", packageName), typeName, typeCode == 3 ? itemUId : null);
            items.Add(new WorkspaceInventoryItem(identity, status, typeCode == 3 ? "BPMSOFT_WORKSPACE_ENTITY_SCHEMA" : "BPMSOFT_WORKSPACE_INVENTORY_ONLY", null, name, UnknownProperties(item, ObservedWorkspaceProperties, "workspace-item-property")));
        }
        return WorkspaceInventory.Create(items);
    }

    private static bool TryAdaptSchema(JsonElement responseRoot, out EntitySchemaModel? schema, out Blocker? blocker)
    {
        if (responseRoot.TryGetProperty("schema", out var observedCandidate) && observedCandidate.ValueKind == JsonValueKind.Object && observedCandidate.TryGetProperty("name", out _)) return TryAdaptObservedSchema(observedCandidate, out schema, out blocker);
        schema = null;
        blocker = UnknownBlocker("schema", "SCHEMA_INVENTORY_UNQUALIFIED");
        return false;
    }

    private static bool TryAdaptObservedSchema(JsonElement source, out EntitySchemaModel? schema, out Blocker? blocker)
    {
        schema = null; blocker = null;
        if (!TryRequiredString(source, "name", out var schemaName) || !TryGuid(source, "uId", out var schemaUId) || !TryGuid(source, "id", out var schemaId) || !source.TryGetProperty("package", out var package) || package.ValueKind != JsonValueKind.Object || !TryGuid(package, "id", out var packageId) || !TryGuid(package, "uId", out var packageUId) || !TryRequiredString(package, "name", out var packageName) || !source.TryGetProperty("columns", out var ownColumns) || ownColumns.ValueKind != JsonValueKind.Array || !source.TryGetProperty("inheritedColumns", out var inheritedColumns) || inheritedColumns.ValueKind != JsonValueKind.Array || !source.TryGetProperty("indexes", out var indexes) || indexes.ValueKind != JsonValueKind.Array)
        { blocker = UnknownBlocker("schema", "SCHEMA_INVENTORY_UNQUALIFIED"); return false; }
        string? parentName = null; Guid? parentUId = null;
        if (source.TryGetProperty("parentSchema", out var parent) && parent.ValueKind != JsonValueKind.Null)
        {
            if (parent.ValueKind != JsonValueKind.Object || !TryRequiredString(parent, "name", out parentName) || !TryGuid(parent, "uId", out var parsedParentUId)) { blocker = UnknownBlocker("schema-parent", "SCHEMA_INVENTORY_UNQUALIFIED"); return false; }
            parentUId = parsedParentUId;
        }
        if (!TryReadObservedColumns(ownColumns, ColumnOwnership.Own, 0, out var own) || !TryReadObservedColumns(inheritedColumns, ColumnOwnership.Inherited, own.Count, out var inherited) || !TryReadObservedIndexes(indexes, out var typedIndexes))
        { blocker = UnknownBlocker("schema-members", "SCHEMA_INVENTORY_UNQUALIFIED"); return false; }
        schema = new EntitySchemaModel(new SchemaIdentity(schemaName, schemaUId, schemaId, parentName, parentUId, new PackageLayerIdentity(packageId, packageUId, "schema-package", packageName)), own.Concat(inherited).ToArray(), typedIndexes, UnknownProperties(source, ObservedSchemaProperties, "schema-property"));
        return true;
    }

    private static bool TryReadObservedColumns(JsonElement source, ColumnOwnership ownership, int initialOrdinal, out IReadOnlyList<EntityColumnModel> columns)
    {
        var result = new List<EntityColumnModel>();
        foreach (var column in source.EnumerateArray())
        {
            if (column.ValueKind != JsonValueKind.Object || !TryRequiredString(column, "name", out var name) || !TryGuid(column, "uId", out var uId) || !column.TryGetProperty("type", out var type) || !type.TryGetInt32(out var typeCode) || !column.TryGetProperty("requirementType", out var requirement) || !requirement.TryGetInt32(out var requirementType) || !column.TryGetProperty("indexed", out var indexed) || indexed.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) { columns = []; return false; }
            SchemaReference? reference = null;
            if (column.TryGetProperty("referenceSchema", out var referenceSource) && referenceSource.ValueKind != JsonValueKind.Null)
            {
                if (referenceSource.ValueKind != JsonValueKind.Object || !TryRequiredString(referenceSource, "name", out var referenceName) || !TryGuid(referenceSource, "uId", out var referenceUId)) { columns = []; return false; }
                reference = new SchemaReference(referenceName, referenceUId);
            }
            result.Add(new EntityColumnModel(name, uId, initialOrdinal + result.Count, ownership, typeCode, requirementType, indexed.GetBoolean(), reference, UnknownProperties(column, ObservedColumnProperties, "column-property")));
        }
        columns = result; return true;
    }

    private static bool TryReadObservedIndexes(JsonElement source, out IReadOnlyList<EntityIndexModel> indexes)
    {
        var result = new List<EntityIndexModel>();
        foreach (var index in source.EnumerateArray())
        {
            if (index.ValueKind != JsonValueKind.Object || !TryGuid(index, "uId", out var indexUId) || !TryRequiredString(index, "name", out var name) || !index.TryGetProperty("isUnique", out var unique) || unique.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !index.TryGetProperty("columns", out var members) || members.ValueKind != JsonValueKind.Array) { indexes = []; return false; }
            bool? autoName = null;
            if (index.TryGetProperty("isAutoName", out var observedAuto) || index.TryGetProperty("autoName", out observedAuto)) { if (observedAuto.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) { indexes = []; return false; } autoName = observedAuto.GetBoolean(); }
            var typedMembers = new List<IndexMember>();
            foreach (var member in members.EnumerateArray()) { if (member.ValueKind != JsonValueKind.Object || !TryGuid(member, "columnUId", out var columnUId)) { indexes = []; return false; } typedMembers.Add(new IndexMember(columnUId, typedMembers.Count, UnknownProperties(member, IndexMemberProperties, "index-member-property"))); }
            result.Add(new EntityIndexModel(indexUId, name, unique.GetBoolean(), autoName, typedMembers, UnknownProperties(index, ObservedIndexProperties, "index-property")));
        }
        indexes = result; return true;
    }

    private static WorkspaceInventory InvalidInventory(IReadOnlyList<WorkspaceInventoryItem> items) => new(false, items, UnknownBlocker("workspace-inventory", "INVENTORY_UNQUALIFIED"));
    private static Blocker UnknownBlocker(string scope, string reason) => new(BlockerCode.UnknownShapeUnqualified, scope, reason, "Capture a safe structural envelope or resolve the typed source contract.", "Stop this qualification run.");
    private static IReadOnlyList<LosslessShapeEnvelope> UnknownProperties(JsonElement source, HashSet<string> known, string typePrefix) => source.EnumerateObject().Where(property => !known.Contains(property.Name)).OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => BuildEnvelope(typePrefix + ":" + property.Name, property.Value)).ToArray();
    private static LosslessShapeEnvelope BuildEnvelope(string typeTag, JsonElement payload) { var hashes = new List<string>(); var structure = Describe(payload, hashes); return new LosslessShapeEnvelope(typeTag, structure, Digest(string.Join("|", hashes)), hashes.Count, Digest(structure)); }
    private static string Describe(JsonElement value, List<string> hashes) => value.ValueKind switch { JsonValueKind.Object => "object{" + string.Join(",", value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => property.Name + ":" + Describe(property.Value, hashes))) + "}", JsonValueKind.Array => "array[" + string.Join(",", value.EnumerateArray().Select(element => Describe(element, hashes))) + "]", JsonValueKind.String => Scalar("string", value.GetString()?.Length ?? 0, value.GetRawText(), hashes), JsonValueKind.Number => Scalar("number", value.GetRawText().Length, value.GetRawText(), hashes), JsonValueKind.True or JsonValueKind.False => Scalar("boolean", 1, value.GetRawText(), hashes), JsonValueKind.Null => "null", _ => Scalar("unknown", value.GetRawText().Length, value.GetRawText(), hashes) };
    private static string Scalar(string scalarClass, int length, string raw, List<string> hashes) { hashes.Add(Digest(raw)); return scalarClass + "(" + length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")"; }
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static bool TryGuid(JsonElement source, string property, out Guid value) { value = default; return source.TryGetProperty(property, out var candidate) && candidate.ValueKind == JsonValueKind.String && Guid.TryParse(candidate.GetString(), out value) && value != Guid.Empty; }
    private static bool TryRequiredString(JsonElement source, string property, out string value)
    {
        value = string.Empty;
        if (!source.TryGetProperty(property, out var candidate) || candidate.ValueKind != JsonValueKind.String || candidate.GetString() is not { } text || string.IsNullOrWhiteSpace(text)) return false;
        value = text;
        return true;
    }
    private static string? TryOptionalString(JsonElement source, string property) => source.TryGetProperty(property, out var candidate) && candidate.ValueKind == JsonValueKind.String ? candidate.GetString() : null;
}
