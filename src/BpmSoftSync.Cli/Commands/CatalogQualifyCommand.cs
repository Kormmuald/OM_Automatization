using BpmSoftSync.Application;
using BpmSoftSync.Cli.Diagnostics;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public sealed class CatalogQualifyCommand(IInvocationPolicy policy, ILiveCatalogQualificationRunner? runner = null)
{
    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out var targetAlias, out var outputRoot, out var live))
            return await WriteAsync(Blocked("QUALIFY_COMMAND_INVALID", "Supply --target <safe-alias> --scope full --manual [--live] [--output-root <path>]."), output);
        var admission = policy.Check(InvocationSource.ManualTerminal, interactive: true);
        if (!admission.IsSuccess) return await WriteAsync(admission, output);
        if (!live) return await WriteAsync(Blocked("LIVE_ADMISSION_REQUIRED", "The live harness requires both --manual and --live; use offline validation otherwise."), output);
        if (!CatalogOutputRoot.TryResolve(outputRoot, out var root)) return await WriteAsync(Blocked("OUTPUT_ROOT_NOT_ALLOWED", "Choose a new local user output root outside repository, templates, prototypes and temporary storage."), output);
        if (runner is null) return await WriteAsync(Blocked("LIVE_HARNESS_NOT_CONFIGURED", "Use the separately configured manual live harness after S06 acceptance and the current human gate."), output);
        try { return await WriteAsync(await runner.ExecuteAsync(targetAlias!, root, cancellationToken), output); }
        catch (BpmSoftSync.Adapters.BpmSoft.BpmSoftTransportException) { return await WriteAsync(Blocked("LIVE_READ_UNAVAILABLE", "Inspect the interactive terminal-only read-only session and start a new run only after human review."), output); }
    }

    private static bool TryParse(string[] args, out string? targetAlias, out string? outputRoot, out bool live)
    {
        targetAlias = null; outputRoot = null; live = false;
        if (args.Any(IsSecretBearing)) return false;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--target" when targetAlias is null && index + 1 < args.Length: targetAlias = args[++index]; break;
                case "--scope" when index + 1 < args.Length && string.Equals(args[++index], "full", StringComparison.Ordinal): break;
                case "--manual": break;
                case "--live" when !live: live = true; break;
                case "--output-root" when outputRoot is null && index + 1 < args.Length: outputRoot = args[++index]; break;
                default: return false;
            }
        }
        return targetAlias is not null && targetAlias.All(character => char.IsLetterOrDigit(character) || character is '-' or '_') && args.Contains("--manual", StringComparer.Ordinal) && args.Contains("--scope", StringComparer.Ordinal);
    }

    private static bool IsSecretBearing(string argument) => argument.Contains("password", StringComparison.OrdinalIgnoreCase) || argument.Contains("cookie", StringComparison.OrdinalIgnoreCase) || argument.Contains("csrf", StringComparison.OrdinalIgnoreCase) || argument.Contains("authorization", StringComparison.OrdinalIgnoreCase);
    private static SafeResult Blocked(string reason, string recovery) => SafeResult.Blocked(new Blocker(BlockerCode.FullCatalogNotQualified, "catalog-qualify", reason, recovery, "Do not send HTTP, retry, run Pass C, Compare or Apply."));
    private static async Task<SafeResult> WriteAsync(SafeResult result, TextWriter output) { await output.WriteLineAsync(SafeDiagnosticRenderer.Render(result)); return result; }
}
