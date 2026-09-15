using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Cli.Diagnostics;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public sealed class CatalogDiagnoseCommand(AppendOnlyRunStore store)
{
    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output)
    {
        var result = args.Length == 2 && args[0] == "--run" && Guid.TryParse(args[1], out var runId)
            ? store.Diagnose(runId, 20, out _)
            : SafeResult.Blocked(new Blocker(BlockerCode.EvidenceSchemaInvalid, "diagnose", "RUN_NOT_FOUND", "Supply one local RunId.", "Stop diagnosis."));
        await output.WriteLineAsync(SafeDiagnosticRenderer.Render(result));
        return result;
    }
}
