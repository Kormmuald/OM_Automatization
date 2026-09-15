using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Application;
using BpmSoftSync.Cli.Diagnostics;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

/// <summary>Offline-only fixture entrypoint that uses the identical qualification/pair composition as live mode.</summary>
public sealed class CatalogQualifyOfflineCommand
{
    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (!TryParse(args, out var fixture, out var outputRoot) || !CatalogOutputRoot.TryResolve(outputRoot, out var root))
            return await WriteAsync(Blocked("OFFLINE_COMMAND_INVALID", "Supply --fixture <sanitized-fixture> --output-root <new-user-local-directory>."), output);
        try
        {
            using var session = await FixtureBpmSoftCatalogSession.OpenAsync(fixture!, cancellationToken);
            var store = new AppendOnlyRunStore(root);
            var qualification = new CatalogQualificationService(session.Source, CatalogQualificationPolicyFactory.Create(session.TargetAlias, session.TargetOrigin), store);
            var publisher = new WorkbookPairSnapshotPublisher(new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()));
            return await WriteAsync(await new CatalogQualificationWorkflow(qualification, publisher).ExecuteAsync(cancellationToken), output);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or System.Text.Json.JsonException or BpmSoftTransportException)
        {
            return await WriteAsync(Blocked("OFFLINE_FIXTURE_INVALID", "Use a manifest-approved complete sanitized fixture."), output);
        }
    }

    private static bool TryParse(string[] args, out string? fixture, out string? outputRoot)
    {
        fixture = null; outputRoot = null;
        if (args.Any(argument => argument.Contains("password", StringComparison.OrdinalIgnoreCase) || argument.Contains("cookie", StringComparison.OrdinalIgnoreCase) || argument.Contains("csrf", StringComparison.OrdinalIgnoreCase) || argument.Contains("authorization", StringComparison.OrdinalIgnoreCase))) return false;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--fixture" && fixture is null && index + 1 < args.Length) fixture = args[++index];
            else if (args[index] == "--output-root" && outputRoot is null && index + 1 < args.Length) outputRoot = args[++index];
            else return false;
        }
        return !string.IsNullOrWhiteSpace(fixture) && !string.IsNullOrWhiteSpace(outputRoot);
    }

    private static SafeResult Blocked(string reason, string recovery) => SafeResult.Blocked(new Blocker(BlockerCode.OfflineFixtureInvalid, "catalog-qualify-offline", reason, recovery, "Do not prompt, send network traffic, retry, Compare or Apply."));
    private static async Task<SafeResult> WriteAsync(SafeResult result, TextWriter output) { await output.WriteLineAsync(SafeDiagnosticRenderer.Render(result)); return result; }
}
