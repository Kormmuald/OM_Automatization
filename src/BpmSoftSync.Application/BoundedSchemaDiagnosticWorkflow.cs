using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

/// <summary>
/// Executes exactly one bounded schema traversal. A completed traversal is not a
/// qualification result: only the full two-pass workflow may qualify a catalog.
/// </summary>
public sealed class BoundedSchemaDiagnosticWorkflow(ISchemaDiagnosticSource source)
{
    private readonly ISchemaDiagnosticSource _source = source ?? throw new ArgumentNullException(nameof(source));

    public async ValueTask<SafeResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var blocker = await _source.ReadUntilFirstBlockerAsync(cancellationToken);
        return blocker is not null
            ? SafeResult.Blocked(blocker)
            : SafeResult.Blocked(new Blocker(
                BlockerCode.FullCatalogNotQualified,
                "schema-diagnostic",
                "SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER",
                "The bounded schema diagnostic is intentionally non-qualifying; review the terminal-safe result.",
                "Stop. Do not retry, run qualification, query lookups, or publish output from this diagnostic."));
    }
}
