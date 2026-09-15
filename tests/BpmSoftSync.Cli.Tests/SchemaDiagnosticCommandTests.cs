using System.Diagnostics;
using BpmSoftSync.Application;
using BpmSoftSync.Cli.Commands;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Tests;

public static class SchemaDiagnosticCommandTests
{
    public static async Task CliRequiresTheExactInteractiveContractAndRejectsSecretsOrRetryAsync()
    {
        var runner = new CaptureRunner();
        var command = new CatalogSchemaDiagnoseCommand(new ManualInvocationPolicy(), runner, _ => new CaptureEvidenceStore());
        var invalid = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live", "--retry"], new StringWriter());
        var secretOutput = new StringWriter();
        var secret = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live", "--password"], secretOutput);

        Assert(!invalid.IsSuccess && invalid.Reason == "SCHEMA_DIAGNOSTIC_COMMAND_INVALID" && !secret.IsSuccess && secret.Reason == "SCHEMA_DIAGNOSTIC_COMMAND_INVALID", "The schema diagnostic admitted an unsupported retry or secret-bearing argument.");
        Assert(runner.Count == 0 && !secretOutput.ToString().Contains("password", StringComparison.OrdinalIgnoreCase), "An invalid schema diagnostic invoked the runner or echoed a secret-bearing argument.");
    }

    public static async Task CliHasNoOutputOrPublicationParameterAndReturnsSafeTerminalResultAsync()
    {
        var runner = new CaptureRunner();
        var evidence = new CaptureEvidenceStore();
        var command = new CatalogSchemaDiagnoseCommand(new ManualInvocationPolicy(), runner, _ => evidence);
        var output = new StringWriter();

        var result = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live"], output);

        Assert(!result.IsSuccess && result.Reason == "SCHEMA_INVENTORY_UNQUALIFIED" && runner.Count == 1 && runner.Target == "fixture", "The schema diagnostic did not dispatch its single bounded runner call.");
        Assert(evidence.Count == 1 && evidence.Terminal?.Reason == "SCHEMA_INVENTORY_UNQUALIFIED" && output.ToString().Contains("evidenceToken=", StringComparison.Ordinal) && !output.ToString().Contains("publication", StringComparison.OrdinalIgnoreCase) && !output.ToString().Contains("output-root", StringComparison.OrdinalIgnoreCase), "The terminal result was not sealed as diagnostic-only safe evidence.");
    }

    public static async Task CliRejectsRelativeWorkspaceAndTemporaryEvidenceRootsBeforeRunnerAsync()
    {
        var runner = new CaptureRunner();
        var command = new CatalogSchemaDiagnoseCommand(new ManualInvocationPolicy(), runner, _ => new CaptureEvidenceStore());
        var roots = new[]
        {
            "relative-evidence",
            Environment.CurrentDirectory,
            Path.GetTempPath(),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            Path.GetPathRoot(Environment.CurrentDirectory)!
        };
        foreach (var root in roots)
        {
            var result = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live", "--evidence-root", root], new StringWriter());
            Assert(!result.IsSuccess && result.Reason == "DIAGNOSTIC_EVIDENCE_ROOT_NOT_ALLOWED", "An unsafe diagnostic evidence root was admitted.");
        }
        Assert(runner.Count == 0, "An unsafe diagnostic evidence root reached the runner.");
    }

    public static async Task CliUsesOnlyUserLocalEvidenceRootsAndRejectsOutputPublicationAsync()
    {
        var runner = new CaptureRunner();
        var seenRoots = new List<string>();
        var command = new CatalogSchemaDiagnoseCommand(
            new ManualInvocationPolicy(),
            runner,
            root =>
            {
                seenRoots.Add(root);
                return new CaptureEvidenceStore();
            });
        var explicitRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync", "diagnostic-evidence-test");

        var defaultResult = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live"], new StringWriter());
        var explicitResult = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live", "--evidence-root", explicitRoot], new StringWriter());
        var outputResult = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live", "--output-root", explicitRoot], new StringWriter());

        Assert(!defaultResult.IsSuccess && !explicitResult.IsSuccess && !outputResult.IsSuccess && outputResult.Reason == "SCHEMA_DIAGNOSTIC_COMMAND_INVALID", "The bounded diagnostic admitted publication arguments.");
        Assert(runner.Count == 2 && seenRoots.Count == 2 && seenRoots.All(root => root.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) && seenRoots.All(root => !root.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase)) && !seenRoots.Any(root => root.StartsWith(Environment.CurrentDirectory, StringComparison.OrdinalIgnoreCase)), "The bounded diagnostic did not resolve only user-local evidence roots.");
    }

    public static async Task CliRejectsUserLocalReparsePointEscapeBeforeRunnerAsync()
    {
        if (!OperatingSystem.IsWindows()) return;
        var runner = new CaptureRunner();
        var command = new CatalogSchemaDiagnoseCommand(new ManualInvocationPolicy(), runner, _ => new CaptureEvidenceStore());
        var baseRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync", "diagnostic-evidence-link-test-" + Guid.NewGuid().ToString("N"));
        var external = Path.Combine(Path.GetTempPath(), "BpmSoftSync-link-target-" + Guid.NewGuid().ToString("N"));
        var link = Path.Combine(baseRoot, "escape");
        try
        {
            Directory.CreateDirectory(baseRoot);
            Directory.CreateDirectory(external);
            try
            {
                Directory.CreateSymbolicLink(link, external);
            }
            catch (Exception error) when (error is UnauthorizedAccessException or IOException)
            {
                // Junctions normally remain available where symbolic links need
                // developer mode. The generated test paths are local scratch only.
                var junction = new ProcessStartInfo("cmd.exe") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                junction.ArgumentList.Add("/c");
                junction.ArgumentList.Add($"mklink /J \"{link}\" \"{external}\"");
                using var process = Process.Start(junction);
                process?.WaitForExit();
                if (process?.ExitCode != 0) return;
            }
            if ((new DirectoryInfo(link).Attributes & FileAttributes.ReparsePoint) == 0) throw new InvalidOperationException("The reparse-point characterization setup did not create a link.");
            var result = await command.ExecuteAsync(["--target", "fixture", "--manual", "--live", "--evidence-root", Path.Combine(link, "child")], new StringWriter());
            Assert(!result.IsSuccess && result.Reason == "DIAGNOSTIC_EVIDENCE_ROOT_NOT_ALLOWED" && runner.Count == 0, "A user-local reparse-point escape reached the bounded runner.");
        }
        finally
        {
            try { if (Directory.Exists(link)) Directory.Delete(link); } catch (IOException) { }
            try { if (Directory.Exists(baseRoot)) Directory.Delete(baseRoot, true); } catch (IOException) { }
            try { if (Directory.Exists(external)) Directory.Delete(external, true); } catch (IOException) { }
        }
    }

    private sealed class CaptureRunner : ILiveSchemaDiagnosticRunner
    {
        public int Count { get; private set; }
        public string? Target { get; private set; }
        public ValueTask<SafeResult> ExecuteAsync(string targetAlias, CancellationToken cancellationToken = default)
        {
            Count++;
            Target = targetAlias;
            return ValueTask.FromResult(SafeResult.Blocked(new Blocker(BlockerCode.UnknownShapeUnqualified, "schema", "SCHEMA_INVENTORY_UNQUALIFIED", "Capture closed shape categories.", "Stop.")));
        }
    }

    private sealed class CaptureEvidenceStore : ISchemaDiagnosticEvidenceStore
    {
        public int Count { get; private set; }
        public SafeResult? Terminal { get; private set; }
        public ValueTask<SchemaDiagnosticEvidenceWrite> SealTerminalAsync(string targetAlias, SafeResult terminal, CancellationToken cancellationToken = default)
        {
            Count++;
            Terminal = terminal;
            return ValueTask.FromResult(new SchemaDiagnosticEvidenceWrite(SafeResult.SuccessForHumanReview("test"), "8888888888888888888888888888888888888888"));
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
