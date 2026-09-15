using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem;

/// <summary>Application-facing adapter; it never begins a run or rereads the catalog.</summary>
public sealed class WorkbookPairSnapshotPublisher(AtomicWorkbookPairPublisher publisher) : IQualifiedSnapshotPublisher
{
    public async ValueTask<SafeResult> PublishAsync(CatalogQualification qualification, CancellationToken cancellationToken = default) =>
        (await publisher.PublishAsync(qualification, cancellationToken)).Result;
}
