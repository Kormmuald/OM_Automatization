using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

/// <summary>One S04-to-S05 process boundary: qualify exactly once, then publish only its accepted snapshot.</summary>
public sealed class CatalogQualificationWorkflow(ICatalogQualificationService qualification, IQualifiedSnapshotPublisher publisher)
{
    public async ValueTask<SafeResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var result = await qualification.QualifyAsync(cancellationToken);
        if (!result.IsQualified || !result.Result.IsSuccess || result.Snapshot is null)
            return result.Result;
        return await publisher.PublishAsync(result, cancellationToken);
    }
}

public interface IQualifiedSnapshotPublisher
{
    ValueTask<SafeResult> PublishAsync(CatalogQualification qualification, CancellationToken cancellationToken = default);
}
