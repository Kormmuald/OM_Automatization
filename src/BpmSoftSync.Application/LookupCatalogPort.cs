using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

public interface ILookupCatalogSource
{
    ValueTask<LookupCatalog> ReadFullAsync(
        WorkspaceObjectModel objectModel,
        LookupReadLimits limits,
        CancellationToken cancellationToken = default);
}
