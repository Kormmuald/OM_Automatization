using BpmSoftSync.Adapters.FileSystem;

namespace BpmSoftSync.Adapters.FileSystem.Tests;
public static class SecretValueScannerTests
{
    public static void CanaryReportsOnlyCategoryLocationAndDigest()
    {
        var scan = new SecretValueScanner().Scan("password=RAW_LOOKUP_CANARY", "fixture");
        if (scan.Count == 0 || scan.Any(x => x.ToString().Contains("RAW_LOOKUP_CANARY", StringComparison.Ordinal))) throw new InvalidOperationException("Canary scanner leaked a matched value.");
    }
}
