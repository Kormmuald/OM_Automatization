using BpmSoftSync.Cli.Commands;
using BpmSoftSync.Testing;

namespace BpmSoftSync.Cli.Tests;

public static class CatalogValidateOfflineTests
{
    public static async Task FixtureOnlyCommandProducesSafeResultAsync()
    {
        var fixturePath = Path.GetFullPath(Path.Combine("tests", "fixtures", "read-only", "catalog-valid.json"));
        var output = new StringWriter();
        var capture = new RequestCapture();
        var command = new CatalogValidateOfflineCommand(new FakeReadOnlyTransport(capture));
        var result = await command.ExecuteAsync(["--fixture", fixturePath], output);
        var rendered = output.ToString();
        if (!result.IsSuccess || result.Reason != "HUMAN_REVIEW_REQUIRED" || !rendered.Contains("nextAction=", StringComparison.Ordinal) || rendered.Contains("test-password", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Offline command did not produce a safe fixture-only result.");
        }

        var rejectedOutput = new StringWriter();
        var rejected = await command.ExecuteAsync(["--password", "test-password"], rejectedOutput);
        if (rejected.IsSuccess || capture.Requests.Count != 1 || capture.WriteCallCount != 0 || !rejectedOutput.ToString().Contains("OFFLINE_FIXTURE_REQUIRED", StringComparison.Ordinal) || rejectedOutput.ToString().Contains("test-password", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Offline command accepted or leaked a credential argument.");
        }
    }
}
