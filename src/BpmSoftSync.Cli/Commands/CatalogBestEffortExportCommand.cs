using BpmSoftSync.Application;
using BpmSoftSync.Cli.Diagnostics;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public sealed class CatalogBestEffortExportCommand(IInvocationPolicy policy, ILiveBestEffortExportRunner runner)
{
    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out var targetAlias, out var outputRoot, out var live)) return await WriteAsync(Blocked("BEST_EFFORT_COMMAND_INVALID", "Supply --target <safe-alias> --scope full --manual --live [--output-root <path>]."), output);
        if (!policy.Check(InvocationSource.ManualTerminal, interactive: true).IsSuccess) return await WriteAsync(Blocked("MANUAL_ADMISSION_REQUIRED", "Run only from an interactive manually authorized terminal."), output);
        if (!live) return await WriteAsync(Blocked("LIVE_ADMISSION_REQUIRED", "The best-effort export requires --manual and --live."), output);
        if (!CatalogOutputRoot.TryResolve(outputRoot, out var root)) return await WriteAsync(Blocked("OUTPUT_ROOT_NOT_ALLOWED", "Choose a local user output root outside the repository and temporary storage."), output);
        try { return await WriteAsync(await runner.ExecuteAsync(targetAlias!, root, cancellationToken), output); }
        catch (BpmSoftSync.Adapters.BpmSoft.BpmSoftTransportException) { return await WriteAsync(Blocked("LIVE_READ_UNAVAILABLE", "Inspect the interactive read-only session and start a new run after human review."), output); }
    }

    private static bool TryParse(string[] args, out string? targetAlias, out string? outputRoot, out bool live)
    {
        targetAlias = null; outputRoot = null; live = false;
        if (args.Any(value => value.Contains("password", StringComparison.OrdinalIgnoreCase) || value.Contains("cookie", StringComparison.OrdinalIgnoreCase) || value.Contains("csrf", StringComparison.OrdinalIgnoreCase) || value.Contains("authorization", StringComparison.OrdinalIgnoreCase))) return false;
        for (var i = 0; i < args.Length; i++) switch (args[i])
        {
            case "--target" when targetAlias is null && i + 1 < args.Length: targetAlias = args[++i]; break;
            case "--scope" when i + 1 < args.Length && string.Equals(args[++i], "full", StringComparison.Ordinal): break;
            case "--manual": break;
            case "--live" when !live: live = true; break;
            case "--output-root" when outputRoot is null && i + 1 < args.Length: outputRoot = args[++i]; break;
            default: return false;
        }
        return targetAlias is not null && targetAlias.All(character => char.IsLetterOrDigit(character) || character is '-' or '_') && args.Contains("--manual", StringComparer.Ordinal) && args.Contains("--scope", StringComparer.Ordinal);
    }

    private static SafeResult Blocked(string reason, string recovery) => SafeResult.Blocked(new Blocker(BlockerCode.FullCatalogNotQualified, "catalog-best-effort-export", reason, recovery, "Do not retry automatically, Compare or Apply."));
    private static async Task<SafeResult> WriteAsync(SafeResult result, TextWriter output) { await output.WriteLineAsync(SafeDiagnosticRenderer.Render(result)); return result; }
}
