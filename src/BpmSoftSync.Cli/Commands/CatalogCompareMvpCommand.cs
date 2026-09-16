using BpmSoftSync.Application;
using BpmSoftSync.Cli.Diagnostics;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public interface ILiveCompareMvpRunner
{
    ValueTask<SafeResult> ExecuteAsync(string modelPath, string lookupPath, string targetAlias, string outputRoot, CancellationToken cancellationToken = default);
}

public sealed class CatalogCompareMvpCommand(IInvocationPolicy policy, ILiveCompareMvpRunner runner)
{
    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out var model, out var lookup, out var target, out var outputRoot)) return await WriteAsync(Blocked("COMPARE_MVP_COMMAND_INVALID", "Supply --model <absolute-path> --lookup <absolute-path> --target <safe-alias> --scope best-effort --manual --live --output-root <new-absolute-user-local-dir>."), output);
        if (!policy.Check(InvocationSource.ManualTerminal, interactive: true).IsSuccess) return await WriteAsync(Blocked("MANUAL_ADMISSION_REQUIRED", "Run only from an interactive manually authorized terminal."), output);
        if (!CatalogOutputRoot.TryResolve(outputRoot, out var root) || Directory.Exists(root)) return await WriteAsync(Blocked("OUTPUT_ROOT_NOT_ALLOWED", "Choose a new user-local directory outside the repository and temporary storage."), output);
        try { return await WriteAsync(await runner.ExecuteAsync(model!, lookup!, target!, root, cancellationToken), output); }
        catch (InvalidDataException) { return await WriteAsync(Blocked("COMPARE_PAIR_UNSAFE_OR_UNBOUND", "Use an intact, closed workbook pair bound to the selected target."), output); }
        catch (BpmSoftSync.Adapters.BpmSoft.BpmSoftTransportException) { return await WriteAsync(Blocked("LIVE_READ_UNAVAILABLE", "Inspect the interactive session and start a new run only after human review."), output); }
    }

    private static bool TryParse(string[] args, out string? model, out string? lookup, out string? target, out string? output)
    {
        model = lookup = target = output = null; var scope = false; var manual = false; var live = false;
        if (args.Any(value => value.Contains("password", StringComparison.OrdinalIgnoreCase) || value.Contains("cookie", StringComparison.OrdinalIgnoreCase) || value.Contains("csrf", StringComparison.OrdinalIgnoreCase) || value.Contains("authorization", StringComparison.OrdinalIgnoreCase))) return false;
        for (var i = 0; i < args.Length; i++) switch (args[i])
        {
            case "--model" when model is null && i + 1 < args.Length: model = args[++i]; break;
            case "--lookup" when lookup is null && i + 1 < args.Length: lookup = args[++i]; break;
            case "--target" when target is null && i + 1 < args.Length: target = args[++i]; break;
            case "--output-root" when output is null && i + 1 < args.Length: output = args[++i]; break;
            case "--scope" when i + 1 < args.Length && args[++i] == "best-effort": scope = true; break;
            case "--manual": manual = true; break;
            case "--live": live = true; break;
            default: return false;
        }
        return model is not null && lookup is not null && output is not null && scope && manual && live && target is not null && target.All(character => char.IsLetterOrDigit(character) || character is '-' or '_') && Path.IsPathFullyQualified(model) && Path.IsPathFullyQualified(lookup) && Path.IsPathFullyQualified(output);
    }
    private static SafeResult Blocked(string reason, string recovery) => SafeResult.Blocked(new Blocker(BlockerCode.FullCatalogNotQualified, "catalog-compare-mvp", reason, recovery, "Do not write BPMSoft or Excel; do not retry automatically."));
    private static async Task<SafeResult> WriteAsync(SafeResult result, TextWriter output) { await output.WriteLineAsync(SafeDiagnosticRenderer.Render(result)); return result; }
}
