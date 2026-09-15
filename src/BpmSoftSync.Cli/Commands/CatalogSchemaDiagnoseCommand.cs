using BpmSoftSync.Application;
using BpmSoftSync.Cli.Diagnostics;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public interface ILiveSchemaDiagnosticRunner
{
    ValueTask<SafeResult> ExecuteAsync(string targetAlias, CancellationToken cancellationToken = default);
}

/// <summary>
/// Explicit admission for the bounded H-001 diagnostic. There is intentionally no
/// output-root, retry, rerun, lookup, qualification, or publication argument.
/// </summary>
public sealed class CatalogSchemaDiagnoseCommand(
    IInvocationPolicy policy,
    ILiveSchemaDiagnosticRunner? runner = null,
    Func<string, ISchemaDiagnosticEvidenceStore>? evidenceStoreFactory = null)
{
    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out var targetAlias, out var evidenceRoot))
            return await WriteAsync(Blocked("SCHEMA_DIAGNOSTIC_COMMAND_INVALID", "Supply --target <safe-alias> --manual --live [--evidence-root <absolute-user-local-path>]."), output);
        var admission = policy.Check(InvocationSource.ManualTerminal, interactive: true);
        if (!admission.IsSuccess) return await WriteAsync(admission, output);
        if (!SchemaDiagnosticEvidenceRoot.TryResolve(evidenceRoot, out var resolvedEvidenceRoot))
            return await WriteAsync(Blocked("DIAGNOSTIC_EVIDENCE_ROOT_NOT_ALLOWED", "Choose an absolute user-local evidence root outside workspace, temporary and system locations."), output);
        if (runner is null)
            return await WriteAsync(Blocked("SCHEMA_DIAGNOSTIC_NOT_CONFIGURED", "Use the separately configured interactive bounded diagnostic after independent review."), output);
        SafeResult terminal;
        try { terminal = await runner.ExecuteAsync(targetAlias!, cancellationToken); }
        catch (BpmSoftSync.Adapters.BpmSoft.BpmSoftTransportException)
        {
            terminal = Blocked("SCHEMA_DIAGNOSTIC_READ_UNAVAILABLE", "Inspect the interactive read-only session and obtain a new human decision before any later invocation.");
        }
        var store = evidenceStoreFactory is null
            ? new BpmSoftSync.Adapters.FileSystem.AppendOnlySchemaDiagnosticEvidenceStore(resolvedEvidenceRoot)
            : evidenceStoreFactory(resolvedEvidenceRoot);
        var write = await store.SealTerminalAsync(targetAlias!, terminal, cancellationToken);
        return !write.PersistenceResult.IsSuccess || write.EvidenceToken is null
            ? await WriteAsync(Blocked("DIAGNOSTIC_EVIDENCE_NOT_PERSISTED", "Do not treat this diagnostic as proven; inspect the safe local evidence boundary before any later human decision."), output)
            : await WriteAsync(terminal, output, write.EvidenceToken);
    }

    private static bool TryParse(string[] args, out string? targetAlias, out string? evidenceRoot)
    {
        targetAlias = null; evidenceRoot = null;
        if (args.Any(IsSecretBearing)) return false;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--target" when targetAlias is null && index + 1 < args.Length:
                    targetAlias = args[++index];
                    break;
                case "--evidence-root" when evidenceRoot is null && index + 1 < args.Length:
                    evidenceRoot = args[++index];
                    break;
                case "--manual":
                case "--live":
                    break;
                default:
                    return false;
            }
        }
        return targetAlias is not null && targetAlias.All(character => char.IsLetterOrDigit(character) || character is '-' or '_') && args.Contains("--manual", StringComparer.Ordinal) && args.Contains("--live", StringComparer.Ordinal);
    }

    private static bool IsSecretBearing(string argument) => argument.Contains("password", StringComparison.OrdinalIgnoreCase) || argument.Contains("cookie", StringComparison.OrdinalIgnoreCase) || argument.Contains("csrf", StringComparison.OrdinalIgnoreCase) || argument.Contains("authorization", StringComparison.OrdinalIgnoreCase);
    private static SafeResult Blocked(string reason, string recovery) => SafeResult.Blocked(new Blocker(BlockerCode.FullCatalogNotQualified, "schema-diagnostic", reason, recovery, "Do not send HTTP, retry, rerun, query lookups, qualify, or publish output."));
    private static async Task<SafeResult> WriteAsync(SafeResult result, TextWriter output, string? evidenceToken = null)
    {
        await output.WriteLineAsync(SafeDiagnosticRenderer.Render(result));
        if (evidenceToken is not null) await output.WriteLineAsync($"evidenceToken={evidenceToken}");
        return result;
    }
}
