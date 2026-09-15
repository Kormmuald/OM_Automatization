using BpmSoftSync.Application;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Testing;
namespace BpmSoftSync.Cli.Tests;
public static class ReadOnlyQualificationE2ETests
{
    public static async Task FakeTwoPassRunReachesHumanReviewAsync()
    {
        var capture = new RequestCapture(); var root = Path.Combine(Path.GetTempPath(), "bpmsoft-e2e-" + Guid.NewGuid().ToString("N"));
        try
        {
            var service = new CatalogQualificationService(new FakeCatalogSource(capture), new AppendOnlyRunStore(root));
            var outcome = await service.QualifyAsync();
            var evidence = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories).Where(path => string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "evidence", StringComparison.Ordinal)).ToArray();
            if (!outcome.IsQualified || outcome.RetryCount != 0 || capture.ReadCount != 2 || evidence.Length != 2 || outcome.Result.Reason != "HUMAN_REVIEW_REQUIRED") throw new InvalidOperationException("Production workflow did not execute exactly two safe passes and one run tree.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
