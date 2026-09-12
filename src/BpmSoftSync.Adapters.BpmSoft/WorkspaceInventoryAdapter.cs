using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed record UnknownShapeResult(WorkspaceInventoryItem? InventoryItem, Blocker? Blocker);

public static class WorkspaceInventoryAdapter
{
    public static WorkspaceInventory AdaptWorkspace(JsonElement root)
    {
        if (!root.TryGetProperty("workspaceItems", out var source) || source.ValueKind != JsonValueKind.Array)
        {
            return WorkspaceInventory.Create([]);
        }

        var items = new List<WorkspaceInventoryItem>();
        foreach (var item in source.EnumerateArray())
        {
            if (!TryGuid(item, "workspaceItemUId", out var workspaceItemUId) || !TryGuid(item, "packageId", out var packageId) || !item.TryGetProperty("itemType", out var itemType) || string.IsNullOrWhiteSpace(itemType.GetString()) || !item.TryGetProperty("supportStatus", out var statusProperty) || !TrySupportStatus(statusProperty.GetString(), out var status))
            {
                return new WorkspaceInventory(false, items, new Blocker(BlockerCode.UnknownShapeUnqualified, "workspace-inventory", "INVENTORY_UNQUALIFIED", "Preserve a typed identity and explicit support status for every source item.", "Stop this qualification run."));
            }
            var packageUId = TryGuid(item, "packageUId", out var parsedPackageUId) ? parsedPackageUId : (Guid?)null;
            var layerKind = item.TryGetProperty("layerKind", out var layer) && layer.ValueKind == JsonValueKind.String ? layer.GetString() : null;
            var identity = new WorkspaceItemIdentity(workspaceItemUId, new PackageLayerIdentity(packageId, packageUId, layerKind), itemType.GetString()!);
            items.Add(new WorkspaceInventoryItem(identity, status, "FIXTURE_DECLARED_SUPPORT_STATUS", null));
        }
        return WorkspaceInventory.Create(items);
    }

    public static UnknownShapeResult AdaptUnknown(JsonElement item)
    {
        if (!TryGuid(item, "workspaceItemUId", out var workspaceItemUId) || !TryGuid(item, "packageId", out var packageId) || !item.TryGetProperty("itemType", out var itemType) || string.IsNullOrWhiteSpace(itemType.GetString()) || !item.TryGetProperty("payload", out var payload))
        {
            return new UnknownShapeResult(null, new Blocker(BlockerCode.UnknownShapeUnqualified, "workspace-item", "UNKNOWN_SHAPE_ENVELOPE_REQUIRED", "Capture a safe structural envelope or a bounded research question.", "Stop this qualification run."));
        }

        var packageUId = TryGuid(item, "packageUId", out var parsedPackageUId) ? parsedPackageUId : (Guid?)null;
        var layerKind = item.TryGetProperty("layerKind", out var layer) && layer.ValueKind == JsonValueKind.String ? layer.GetString() : null;
        var envelope = BuildEnvelope(itemType.GetString()!, payload);
        var identity = new WorkspaceItemIdentity(workspaceItemUId, new PackageLayerIdentity(packageId, packageUId, layerKind), itemType.GetString()!);
        return new UnknownShapeResult(new WorkspaceInventoryItem(identity, SupportStatus.Unsupported, "UNKNOWN_SHAPE_INVENTORY_ONLY", envelope), null);
    }

    public static IndexReadResult ReadIndexes(JsonElement schema)
    {
        var relations = new List<IndexRelation>();
        if (schema.TryGetProperty("indexes", out var indexes) && indexes.ValueKind == JsonValueKind.Array)
        {
            foreach (var index in indexes.EnumerateArray())
            {
                if (!TryGuid(index, "indexUId", out var indexUId) || !index.TryGetProperty("columns", out var columns) || columns.ValueKind != JsonValueKind.Array) continue;
                var columnUIds = columns.EnumerateArray().Where(column => TryGuid(column, "columnUId", out _)).Select(column => Guid.Parse(column.GetProperty("columnUId").GetString()!)).ToArray();
                relations.Add(new IndexRelation(indexUId, columnUIds));
            }
        }
        return new IndexReadResult(relations, new Blocker(BlockerCode.IndexSyncUnresolved, "indexes", "INDEX_SYNC_UNRESOLVED", "Index data is read-only inventory evidence.", "Do not plan, load, mutate, or apply indexes."));
    }

    private static LosslessShapeEnvelope BuildEnvelope(string typeTag, JsonElement payload)
    {
        var scalarHashes = new List<string>();
        var structure = Describe(payload, scalarHashes);
        return new LosslessShapeEnvelope(typeTag, structure, Digest(string.Join("|", scalarHashes)), scalarHashes.Count, Digest(structure));
    }

    private static string Describe(JsonElement value, List<string> scalarHashes) => value.ValueKind switch
    {
        JsonValueKind.Object => "object{" + string.Join(",", value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => property.Name + ":" + Describe(property.Value, scalarHashes))) + "}",
        JsonValueKind.Array => "array[" + string.Join(",", value.EnumerateArray().Select(element => Describe(element, scalarHashes))) + "]",
        JsonValueKind.String => Scalar("string", value.GetString()?.Length ?? 0, value.GetRawText(), scalarHashes),
        JsonValueKind.Number => Scalar("number", value.GetRawText().Length, value.GetRawText(), scalarHashes),
        JsonValueKind.True or JsonValueKind.False => Scalar("boolean", 1, value.GetRawText(), scalarHashes),
        JsonValueKind.Null => "null",
        _ => Scalar("unknown", value.GetRawText().Length, value.GetRawText(), scalarHashes)
    };

    private static string Scalar(string scalarClass, int length, string raw, List<string> hashes)
    {
        hashes.Add(Digest(raw));
        return scalarClass + "(" + length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
    }

    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static bool TryGuid(JsonElement source, string property, out Guid value)
    {
        value = default;
        return source.TryGetProperty(property, out var candidate) && candidate.ValueKind == JsonValueKind.String && Guid.TryParse(candidate.GetString(), out value);
    }

    private static bool TrySupportStatus(string? source, out SupportStatus status)
    {
        return Enum.TryParse(source, ignoreCase: false, out status) && Enum.IsDefined(status);
    }
}
