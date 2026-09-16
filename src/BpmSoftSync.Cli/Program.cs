namespace BpmSoftSync.Cli;

public static class Program
{
    // CLI runs are user-local and persistent for bounded safe diagnosis; never use the fixture/test temp root.
    private static readonly string RunRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BpmSoftSync");
    private static readonly Adapters.FileSystem.AppendOnlyRunStore Store = new(RunRoot);
    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "catalog", StringComparison.Ordinal) && args.Length > 1 && string.Equals(args[1], "validate-offline", StringComparison.Ordinal))
        {
            var result = await new Commands.CatalogValidateOfflineCommand(RunRoot).ExecuteAsync(args[2..], Console.Out);
            return result.IsSuccess ? 0 : 2;
        }

        if (args.Length > 1 && string.Equals(args[0], "catalog", StringComparison.Ordinal) && string.Equals(args[1], "qualify-offline", StringComparison.Ordinal))
        {
            var result = await new Commands.CatalogQualifyOfflineCommand().ExecuteAsync(args[2..], Console.Out);
            return result.IsSuccess ? 0 : 2;
        }

        if (args.Length > 1 && string.Equals(args[0], "catalog", StringComparison.Ordinal) && string.Equals(args[1], "qualify", StringComparison.Ordinal))
        {
            var result = await new Commands.CatalogQualifyCommand(new Application.ManualInvocationPolicy(), new Commands.LiveCatalogQualificationRunner()).ExecuteAsync(args[2..], Console.Out);
            return result.IsSuccess ? 0 : 2;
        }

        if (args.Length > 1 && string.Equals(args[0], "catalog", StringComparison.Ordinal) && string.Equals(args[1], "export-best-effort", StringComparison.Ordinal))
        {
            var result = await new Commands.CatalogBestEffortExportCommand(new Application.ManualInvocationPolicy(), new Commands.LiveBestEffortExportRunner()).ExecuteAsync(args[2..], Console.Out);
            return result.IsSuccess ? 0 : 2;
        }

        if (args.Length > 1 && string.Equals(args[0], "catalog", StringComparison.Ordinal) && string.Equals(args[1], "diagnose-schema", StringComparison.Ordinal))
        {
            var result = await new Commands.CatalogSchemaDiagnoseCommand(new Application.ManualInvocationPolicy(), new Commands.LiveSchemaDiagnosticRunner()).ExecuteAsync(args[2..], Console.Out);
            return result.IsSuccess ? 0 : 2;
        }

        if (args.Length > 1 && string.Equals(args[0], "catalog", StringComparison.Ordinal) && string.Equals(args[1], "diagnose", StringComparison.Ordinal))
        {
            var result = await new Commands.CatalogDiagnoseCommand(Store).ExecuteAsync(args[2..], Console.Out);
            return result.IsSuccess ? 0 : 2;
        }

        Console.Error.WriteLine("Supported commands: catalog validate-offline --fixture <sanitized-fixture>; catalog qualify-offline --fixture <sanitized-fixture> --output-root <path>; catalog qualify --target <alias> --scope full --manual --live [--output-root <path>]; catalog export-best-effort --target <alias> --scope full --manual --live [--output-root <path>]; catalog diagnose-schema --target <alias> --manual --live [--evidence-root <absolute-user-local-path>]; catalog diagnose --run <RunId>.");
        return 2;
    }
}
