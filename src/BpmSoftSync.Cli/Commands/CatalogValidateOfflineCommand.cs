using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Application;
using BpmSoftSync.Cli.Diagnostics;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public sealed class CatalogValidateOfflineCommand(string runRoot)
{
    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (args.Length != 2 || !string.Equals(args[0], "--fixture", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(args[1]) || args.Any(arg => arg.Contains("password", StringComparison.OrdinalIgnoreCase) || arg.Contains("cookie", StringComparison.OrdinalIgnoreCase) || arg.Contains("csrf", StringComparison.OrdinalIgnoreCase)))
            return await WriteAsync(SafeResult.Blocked(new Blocker(BlockerCode.OfflineFixtureInvalid, "command", "OFFLINE_FIXTURE_REQUIRED", "Supply exactly --fixture <sanitized-fixture>.", "Use a sanitized local fixture.")), output);
        try
        {
            var store = new AppendOnlyRunStore(runRoot);
            var result = await new CatalogQualificationService(new FixtureCatalogSource(args[1]), store).QualifyAsync(cancellationToken);
            return await WriteAsync(result.Result, output);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            return await WriteAsync(SafeResult.Blocked(new Blocker(BlockerCode.OfflineFixtureInvalid, "fixture", "OFFLINE_FIXTURE_INVALID", "Use an approved complete sanitized fixture manifest.", "Stop the offline run.")), output);
        }
    }

    private static async Task<SafeResult> WriteAsync(SafeResult result, TextWriter output)
    {
        await output.WriteLineAsync(SafeDiagnosticRenderer.Render(result));
        return result;
    }
}
