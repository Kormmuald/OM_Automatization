using BpmSoftSync.Cli.Commands;
namespace BpmSoftSync.Cli.Tests;
public static class CatalogValidateOfflineTests
{
    public static async Task MainDispatchesTheSharedFixtureWorkflowAsync()
    {
        var original = Console.Out; var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            var fixture = Path.GetFullPath(Path.Combine("tests", "fixtures", "read-only", "catalog-valid.json"));
            var exit = await BpmSoftSync.Cli.Program.Main(["catalog", "validate-offline", "--fixture", fixture]);
            if (exit != 0 || !output.ToString().Contains("HUMAN_REVIEW_REQUIRED", StringComparison.Ordinal)) throw new InvalidOperationException("Program.Main did not dispatch the shared fixture workflow.");
        }
        finally { Console.SetOut(original); }
    }

    public static async Task FixtureOnlyCommandProducesSafeResultAsync()
    {
        var fixture = Path.GetFullPath(Path.Combine("tests", "fixtures", "read-only", "catalog-valid.json"));
        var root = Path.Combine(Path.GetTempPath(), "bpmsoft-cli-" + Guid.NewGuid().ToString("N"));
        try
        {
            var output = new StringWriter();
            var result = await new CatalogValidateOfflineCommand(root).ExecuteAsync(["--fixture", fixture], output);
            if (!result.IsSuccess || result.Reason != "HUMAN_REVIEW_REQUIRED" || !output.ToString().Contains("nextAction=", StringComparison.Ordinal)) throw new InvalidOperationException("Fixture command did not traverse the workflow.");
            var rejected = await new CatalogValidateOfflineCommand(root).ExecuteAsync(["--password", "not-allowed"], new StringWriter());
            if (rejected.IsSuccess) throw new InvalidOperationException("Offline command accepted an unsafe argument.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
