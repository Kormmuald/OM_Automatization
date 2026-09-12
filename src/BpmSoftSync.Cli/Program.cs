namespace BpmSoftSync.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "catalog", StringComparison.Ordinal) && args.Length > 1 && string.Equals(args[1], "validate-offline", StringComparison.Ordinal))
        {
            var capture = new Adapters.BpmSoft.RequestCapture();
            var transport = new Adapters.BpmSoft.CapturedReadOnlyTransport(capture);
            var result = await new Commands.CatalogValidateOfflineCommand(transport).ExecuteAsync(args[2..], Console.Out);
            return result.IsSuccess ? 0 : 2;
        }

        Console.Error.WriteLine("Only catalog validate-offline --fixture <sanitized-fixture> is available.");
        return 2;
    }
}
