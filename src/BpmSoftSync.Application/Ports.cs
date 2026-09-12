using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

public interface IReadOnlyTransport
{
    ValueTask<SafeResult> SendAsync(EndpointClassification endpoint, CancellationToken cancellationToken = default);
}

public interface ICatalogSource
{
    ValueTask<SafeResult> ValidateOfflineFixtureAsync(string fixturePath, CancellationToken cancellationToken = default);
}

public interface IRunStore
{
    ValueTask StoreAsync(EvidenceEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface ICatalogQualificationService
{
    ValueTask<SafeResult> QualifyAsync(CancellationToken cancellationToken = default);
}

public interface IAuthorizationGate
{
    SafeResult RefuseWithoutAuthorization(string scope);
}
