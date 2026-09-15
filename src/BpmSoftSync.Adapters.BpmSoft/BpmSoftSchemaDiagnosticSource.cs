using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

/// <summary>
/// The only live read surface used by the bounded schema diagnostic. This is not
/// the full workspace traversal: after one workspace read it deterministically
/// selects one typed entity-schema identity, reads it once, and terminally returns.
/// It deliberately exposes no lookup, qualification, snapshot, evidence-store, or
/// workbook operation.
/// </summary>
public sealed class BpmSoftSchemaDiagnosticSource(BpmSoftReadTransport transport) : ISchemaDiagnosticSource
{
    private readonly BpmSoftReadTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));

    public async ValueTask<Blocker?> ReadUntilFirstBlockerAsync(CancellationToken cancellationToken = default)
    {
        using var workspaceResponse = await _transport.GetWorkspaceItemsAsync(cancellationToken);
        var inventory = WorkspaceInventoryAdapter.AdaptWorkspace(workspaceResponse.Root);
        if (!inventory.IsQualified) return inventory.Blocker;

        // The candidate is derived only from typed GUID identities, never from a
        // display name or response order. This is a single sample, not traversal.
        var candidate = inventory.Items
            .Where(item => item.SupportStatus == SupportStatus.Structured && string.Equals(item.Identity.ItemType, "EntitySchema", StringComparison.Ordinal))
            .OrderBy(item => item.Identity.SchemaUId ?? item.Identity.WorkspaceItemUId)
            .FirstOrDefault();
        if (candidate is null)
            return new Blocker(
                BlockerCode.FullCatalogNotQualified,
                "schema-diagnostic-candidate",
                "SCHEMA_DIAGNOSTIC_NO_SCHEMA_CANDIDATE",
                "Inspect the typed workspace inventory before a new human-authorized diagnostic.",
                "Stop. Do not retry, traverse another item, query lookups, qualify, or publish output.");

        var requestedSchemaUId = candidate.Identity.SchemaUId ?? candidate.Identity.WorkspaceItemUId;
        using var schemaResponse = await _transport.GetSchemaAsync(requestedSchemaUId, cancellationToken);
        if (!WorkspaceInventoryAdapter.TryAdaptSchema(schemaResponse.Root, out var schema, out var blocker)) return blocker;
        if (schema!.Identity.SchemaUId != requestedSchemaUId ||
            !string.Equals(schema.Identity.SchemaName, candidate.DisplayName, StringComparison.Ordinal) ||
            !string.Equals(schema.Identity.PackageLayer.PackageName, candidate.Identity.PackageLayer.PackageName, StringComparison.Ordinal))
            return new Blocker(
                BlockerCode.UnknownShapeUnqualified,
                "schema-identity",
                "SCHEMA_IDENTITY_MISMATCH",
                "Resolve the typed schema-to-workspace identity contract before a new human-authorized diagnostic.",
                "Stop. Do not retry, traverse another item, query lookups, qualify, or publish output.");

        // A valid sample only proves that this one request completed. Returning
        // null lets the bounded workflow emit its explicit non-qualifying terminal.
        return null;
    }
}
