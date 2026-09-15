using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

public interface ICatalogSource
{
    ValueTask<CatalogPassInput> ReadPassAsync(string exactTargetAlias, string scopeDescriptorHash, int passNumber, CancellationToken cancellationToken = default);
}

public interface IFullCatalogSource
{
    ValueTask<FullCatalogRead> ReadFullAsync(CatalogReadRequest request, CancellationToken cancellationToken = default);
}

public interface IRunEvidenceStore
{
    ValueTask<Guid> BeginRunAsync(string targetAlias, CancellationToken cancellationToken = default);
    ValueTask<SafeResult> AppendAsync(Guid runId, QualificationEvidenceEnvelope envelope, CancellationToken cancellationToken = default);
    ValueTask<SafeResult> SealAsync(Guid runId, SafeResult outcome, CancellationToken cancellationToken = default);
}

public interface ICatalogQualificationService
{
    ValueTask<CatalogQualification> QualifyAsync(CancellationToken cancellationToken = default);
}

public interface IInvocationPolicy
{
    SafeResult Check(InvocationSource? source, bool interactive);
}
