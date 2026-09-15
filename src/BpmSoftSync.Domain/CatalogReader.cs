using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BpmSoftSync.Domain;

public sealed record CollectionDefinition(string CollectionId, string OrderKeyId, int MaxPages);

public sealed record CatalogPage(string CursorToken, IReadOnlyList<string> Identities, bool IsTerminal);

// This is the classified, transport-neutral input consumed by the application use case.
// It is deliberately composed of safe typed metadata and page identities only.
public sealed record CatalogPassInput(
    string FixtureId,
    string TargetAlias,
    string ScopeDescriptorHash,
    IReadOnlyList<string> ObservedTargetVersionEvidence,
    CollectionDefinition Collection,
    IReadOnlyList<CatalogPage> Pages,
    IReadOnlyList<FingerprintWorkspaceEntry> Workspace,
    IReadOnlyList<FingerprintSchemaEntry> StructuredSchemas,
    IReadOnlyList<FingerprintUnsupportedEntry> Unsupported,
    int? DeclaredWorkbookLimit);

public sealed record CatalogReadResult(bool IsQualified, IReadOnlyList<PageManifest> Manifests, Blocker? Blocker)
{
    public static CatalogReadResult Blocked(IReadOnlyList<PageManifest> manifests, string reason) => new(false, manifests, new Blocker(BlockerCode.CatalogOrderOrPagingUnqualified, "catalog", reason, "Inspect the declared order and sanitized paging fixture.", "Stop this qualification run; do not retry."));
}

public static class OrderedCatalogReader
{
    public static CatalogReadResult Read(CollectionDefinition definition, IReadOnlyList<CatalogPage> pages)
    {
        var manifests = new List<PageManifest>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(definition.CollectionId) || string.IsNullOrWhiteSpace(definition.OrderKeyId) || definition.MaxPages < 1 || pages.Count == 0 || pages.Count > definition.MaxPages)
        {
            return CatalogReadResult.Blocked(manifests, "CATALOG_ORDER_OR_PAGING_UNQUALIFIED");
        }

        for (var ordinal = 0; ordinal < pages.Count; ordinal++)
        {
            var page = pages[ordinal];
            if (!string.Equals(page.CursorToken, ordinal.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) ||
                (page.Identities.Count == 0 && !page.IsTerminal) ||
                (page.IsTerminal && ordinal != pages.Count - 1))
            {
                return CatalogReadResult.Blocked(manifests, "CATALOG_ORDER_OR_PAGING_UNQUALIFIED");
            }

            if (page.Identities.Any(string.IsNullOrWhiteSpace) || page.Identities.Any(identity => !seen.Add(identity)))
            {
                return CatalogReadResult.Blocked(manifests, "CATALOG_ORDER_OR_PAGING_UNQUALIFIED");
            }

            manifests.Add(PageManifest.Create(ordinal, page.CursorToken, page.Identities));
        }

        return !pages[^1].IsTerminal
            ? CatalogReadResult.Blocked(manifests, "CATALOG_ORDER_OR_PAGING_UNQUALIFIED")
            : new CatalogReadResult(true, manifests, null);
    }
}
