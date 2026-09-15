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

/// <summary>
/// Bounded diagnostic port. It deliberately exposes no catalog, lookup, snapshot,
/// publisher, output-root, or rerun capability.
/// </summary>
public interface ISchemaDiagnosticSource
{
    ValueTask<Blocker?> ReadUntilFirstBlockerAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Writes exactly one schema-validated, closed-category terminal record for an
/// already completed bounded diagnostic. This port cannot receive credentials,
/// transport data, lookup values, output paths, or a successful qualification.
/// </summary>
public interface ISchemaDiagnosticEvidenceStore
{
    ValueTask<SchemaDiagnosticEvidenceWrite> SealTerminalAsync(string targetAlias, SafeResult terminal, CancellationToken cancellationToken = default);
}

public sealed record SchemaDiagnosticEvidenceWrite(SafeResult PersistenceResult, string? EvidenceToken);

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
