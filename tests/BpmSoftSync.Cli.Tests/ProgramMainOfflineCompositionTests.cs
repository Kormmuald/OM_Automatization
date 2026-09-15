using BpmSoftSync.Adapters.Excel;

namespace BpmSoftSync.Cli.Tests;

public static class ProgramMainOfflineCompositionTests
{
    public static async Task ProductionMainPublishesFixturePairWithoutLiveAdmissionAsync()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync-S06-main-" + Guid.NewGuid().ToString("N"));
        var fixture = Path.GetFullPath(Path.Combine("tests", "fixtures", "read-only", "s06-full-cli-fake.json"));
        var original = Console.Out; var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            var exit = await BpmSoftSync.Cli.Program.Main(["catalog", "qualify-offline", "--fixture", fixture, "--output-root", root]);
            if (exit != 0 || !output.ToString().Contains("HUMAN_REVIEW_REQUIRED", StringComparison.Ordinal)) throw new InvalidOperationException("Program.Main did not execute the offline production composition: " + output);
            var run = Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).Single(path => Guid.TryParse(Path.GetFileName(path), out _));
            var outputNames = Directory.EnumerateFiles(Path.Combine(run, "output")).Select(Path.GetFileName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (!outputNames.SequenceEqual([WorkbookContract.LookupFileName, WorkbookContract.ModelFileName], StringComparer.Ordinal) || !File.Exists(Path.Combine(run, "evidence", "workbook-pair.json")) || !File.Exists(Path.Combine(run, "evidence", "review-only-seal.json"))) throw new InvalidOperationException("Program.Main offline route did not seal the canonical S05 pair.");
        }
        finally
        {
            Console.SetOut(original);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    public static async Task ProductionMainRejectsWorkspaceAndNestedRootsBeforeLivePromptAsync()
    {
        var roots = new[] { Path.GetFullPath(Environment.CurrentDirectory), Path.Combine(Path.GetFullPath(Environment.CurrentDirectory), "src", "BpmSoftSync.Cli", "unsafe-output") };
        foreach (var root in roots)
        {
            var original = Console.Out; var output = new StringWriter();
            try
            {
                Console.SetOut(output);
                var exit = await BpmSoftSync.Cli.Program.Main(["catalog", "qualify", "--target", "fixture", "--scope", "full", "--manual", "--live", "--output-root", root]);
                if (exit != 2 || !output.ToString().Contains("OUTPUT_ROOT_NOT_ALLOWED", StringComparison.Ordinal)) throw new InvalidOperationException("Program.Main admitted a workspace or nested sensitive output root.");
            }
            finally { Console.SetOut(original); }
        }
    }

    public static async Task ProductionMainRejectsBareTempRootBeforeRunnerPromptOrHttpAsync()
    {
        var bareTemp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var originalOut = Console.Out; var originalIn = Console.In; var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            Console.SetIn(new ThrowingReader());
            var exit = await BpmSoftSync.Cli.Program.Main(["catalog", "qualify", "--target", "fixture", "--scope", "full", "--manual", "--live", "--output-root", bareTemp]);
            if (exit != 2 || !output.ToString().Contains("OUTPUT_ROOT_NOT_ALLOWED", StringComparison.Ordinal)) throw new InvalidOperationException("Program.Main admitted bare %TEMP% or reached the live runner.");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
        }
    }

    private sealed class ThrowingReader : TextReader
    {
        public override string? ReadLine() => throw new InvalidOperationException("Terminal prompt must not run for bare %TEMP%.");
    }
}
