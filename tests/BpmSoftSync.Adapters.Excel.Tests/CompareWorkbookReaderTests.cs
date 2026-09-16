using BpmSoftSync.Adapters.Excel;

namespace BpmSoftSync.Adapters.Excel.Tests;

internal static class CompareWorkbookReaderTests
{
    public static async Task SyntheticBestEffortPairBindsStrictLookupColumnIdentityAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "BpmSoftSync-CompareReader-" + Guid.NewGuid().ToString("N"));
        try
        {
            var snapshot = SyntheticSnapshot.CreateQualification().Snapshot! with { ExportNotice = "UNVERIFIED_SINGLE_READ; synthetic" };
            var pair = await new WorkbookPairMaterializer().StageBestEffortPairAsync(snapshot, root);
            var read = CompareWorkbookPairReader.Read(pair.ModelPath, pair.LookupPath, "fixture");
            var values = read.LookupValues;
            Assert(values.Count != 0 && values.All(row => Guid.TryParse(row["ColumnUId"], out _)) && read.TargetAlias == "fixture", "Compare reader did not bind every lookup value to strict GUID identity.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
