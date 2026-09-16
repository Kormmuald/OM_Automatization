using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem.Tests;

internal static class CompareArtifactWriterTests
{
    public static void SyntheticArtifactSmokeKeepsValueOutOfReport()
    {
        var root = Path.Combine(Path.GetTempPath(), "BpmSoftSync-Compare-" + Guid.NewGuid().ToString("N"));
        const string syntheticValue = "synthetic-only-value";
        try
        {
            var operation = new CompareMvpOperation("LookupValues.Value", Guid.Parse("10000000-0000-0000-0000-000000000001"), Guid.Parse("20000000-0000-0000-0000-000000000001"), Guid.Parse("30000000-0000-0000-0000-000000000001"), "Value", "Text", syntheticValue, new string('a', 64), new string('b', 64));
            var result = new CompareMvpResult("completed_with_blockers", [operation], [new("VALUE_FORMAT_INVALID", new string('c', 64), "Исправьте synthetic input.")], new Dictionary<string, int> { ["operations"] = 1, ["blockers"] = 1 });
            var paths = CompareArtifactWriter.WriteNew(root, new CompareWorkbookPair("fixture", new string('d', 64), new string('e', 64), new string('f', 64), [], []), result, new string('g', 64));
            var report = File.ReadAllText(paths.ReportPath); var plan = File.ReadAllText(paths.PlanPath);
            Assert(!report.Contains(syntheticValue, StringComparison.Ordinal) && plan.Contains(syntheticValue, StringComparison.Ordinal) && File.Exists(paths.ReportPath) && File.Exists(paths.PlanPath), "Compare artifacts did not preserve the confidential plan/report boundary.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
