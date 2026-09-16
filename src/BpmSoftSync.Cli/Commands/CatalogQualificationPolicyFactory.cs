using System.Security.Cryptography;
using System.Text;
using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

internal static class CatalogQualificationPolicyFactory
{
    internal static CatalogScopePolicy Create(string targetAlias, BpmSoftTargetOrigin origin)
    {
        const long bytes = 2L * 1024 * 1024 * 1024;
        var originPolicy = "sha256:" + Digest(origin.Uri.GetLeftPart(UriPartial.Authority));
        return CatalogScopePolicy.Create(targetAlias, originPolicy, "ReadEndpointAllowlist/v1",
            new("workspace", "workspaceItemUId", "GetWorkspaceItems/v1", new(64, 10_000, bytes)),
            new("schemas", "schemaUId/packageLayer", "GetSchema/v1", new(1, 10_000, bytes)),
            new("lookup-registry", "Id", "SelectQuery/lookup-registry-legacy-single/v1", new(3000, 5_000_000, bytes)),
            new("Id", "SelectQuery/lookup-values-legacy-single/v1", new(3000, 5_000_000, bytes)),
            QualifiedCatalogSnapshot.SchemaVersion, "WorkbookProjection/v1");
    }

    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
