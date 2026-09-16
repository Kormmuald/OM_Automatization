using BpmSoftSync.Application;
using BpmSoftSync.Cli.Commands;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Tests;

internal static class CompareMvpCommandTests
{
    public static async Task ExactManualLiveContractIsRequiredBeforeRunnerAsync()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync-compare-test-" + Guid.NewGuid().ToString("N"));
        var model = Path.Combine(root, "model.xlsx"); var lookup = Path.Combine(root, "lookup.xlsx");
        var runner = new Runner(); var command = new CatalogCompareMvpCommand(new ManualInvocationPolicy(), runner); var output = new StringWriter();
        try
        {
            var rejected = await command.ExecuteAsync(["--model", model, "--lookup", lookup, "--target", "fixture", "--scope", "best-effort", "--manual", "--output-root", root], output);
            Assert(!rejected.IsSuccess && runner.Calls == 0, "Compare runner was reachable without --live.");
            var accepted = await command.ExecuteAsync(["--model", model, "--lookup", lookup, "--target", "fixture", "--scope", "best-effort", "--manual", "--live", "--output-root", root], output);
            Assert(accepted.IsSuccess && runner.Calls == 1 && runner.Target == "fixture", "Exact Compare MVP admission did not reach the injected runner.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private sealed class Runner : ILiveCompareMvpRunner
    {
        public int Calls { get; private set; } public string? Target { get; private set; }
        public ValueTask<SafeResult> ExecuteAsync(string modelPath, string lookupPath, string targetAlias, string outputRoot, CancellationToken cancellationToken = default) { Calls++; Target = targetAlias; return ValueTask.FromResult(SafeResult.SuccessForHumanReview("synthetic")); }
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
