using BpmSoftSync.Application;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Cli.Commands;
using BpmSoftSync.Domain;
namespace BpmSoftSync.Cli.Tests;
public static class CliContractTests
{
    public static async Task OutputContainsOnlySafeDiagnosticFieldsAsync()
    {
        var output = new StringWriter();
        await new CatalogQualifyCommand(new ManualInvocationPolicy()).ExecuteAsync(["--password", "not-allowed"], output);
        var text = output.ToString();
        if (!text.Contains("reason=") || !text.Contains("scope=") || !text.Contains("recovery=") || !text.Contains("nextAction=") || text.Contains("not-allowed", StringComparison.Ordinal)) throw new InvalidOperationException("CLI diagnostics leaked unsafe input.");
    }
    public static void FailedShapeDiagnosticRendersOnlyClosedCategories()
    {
        var diagnostic = new FailedShapeDiagnostic(FailedShapePath.SchemaPackageId, ExpectedShapeCategory.GuidString, ObservedJsonKind.String, ArrayCardinalityBucket.NotApplicable, null, GuidStringPredicateStatus.Passed);
        var result = SafeResult.Blocked(new Blocker(BlockerCode.UnknownShapeUnqualified, "schema", "SCHEMA_INVENTORY_UNQUALIFIED", "Capture safe structure.", "Stop.", diagnostic));
        var text = BpmSoftSync.Cli.Diagnostics.SafeDiagnosticRenderer.Render(result);
        var invalid = diagnostic with { Path = FailedShapePath.SchemaId };
        var invalidText = BpmSoftSync.Cli.Diagnostics.SafeDiagnosticRenderer.Render(SafeResult.Blocked(new Blocker(BlockerCode.UnknownShapeUnqualified, "schema", "SCHEMA_INVENTORY_UNQUALIFIED", "Capture safe structure.", "Stop.", invalid)));
        if (!text.Contains("failedShape=path:SchemaPackageId;expected:GuidString;observed:String;array:NotApplicable;ordinal:none;companionGuidString:Passed", StringComparison.Ordinal) || text.Contains("SchemaPackageId=", StringComparison.Ordinal) || text.Contains("raw", StringComparison.OrdinalIgnoreCase) || invalidText.Contains("companionGuidString", StringComparison.Ordinal)) throw new InvalidOperationException("Failed-shape renderer exposed more than the closed, scoped companion category.");
    }
    public static async Task DiagnoseReadsOnlyValidatedBoundedRecordsAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "bpmsoft-diagnose-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new AppendOnlyRunStore(root); var run = await store.BeginRunAsync("fixture");
            var digest = new string('a', 64);
            var envelope = new QualificationEvidenceEnvelope(QualificationEvidenceEnvelope.SchemaVersion, "qualification-summary", digest,
                new QualificationEvidenceMetadata(run, "fixture", "qualification-summary", "SAFE", 0, new Dictionary<string, int>(), new Dictionary<string, string> { ["target"] = digest }, "not-recorded", "not-recorded", [], [], ["SAFE"]));
            if (!(await store.AppendAsync(run, envelope)).IsSuccess) throw new InvalidOperationException("Test setup did not persist validated evidence.");
            var output = new StringWriter(); var result = await new CatalogDiagnoseCommand(store).ExecuteAsync(["--run", run.ToString("D")], output);
            if (!result.IsSuccess || output.ToString().Contains("abc", StringComparison.Ordinal)) throw new InvalidOperationException("Diagnose exposed a raw record or rejected validated evidence.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
