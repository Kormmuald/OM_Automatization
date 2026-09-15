using BpmSoftSync.Domain;

namespace BpmSoftSync.Application.Tests;

public static class BoundedSchemaDiagnosticWorkflowTests
{
    public static async Task ReadsOnceAndReturnsTheFirstSafeBlockerAsync()
    {
        var blocker = new Blocker(BlockerCode.UnknownShapeUnqualified, "schema", "SCHEMA_INVENTORY_UNQUALIFIED", "Capture only closed structure.", "Stop.", new FailedShapeDiagnostic(FailedShapePath.SchemaId, ExpectedShapeCategory.GuidString, ObservedJsonKind.Null, ArrayCardinalityBucket.NotApplicable, null));
        var source = new CountingSource(blocker);

        var result = await new BoundedSchemaDiagnosticWorkflow(source).ExecuteAsync();

        Assert(!result.IsSuccess && result.Reason == "SCHEMA_INVENTORY_UNQUALIFIED" && result.Blocker?.FailedShape == blocker.FailedShape, "The bounded diagnostic did not return its first safe blocker unchanged.");
        Assert(source.Count == 1, "The bounded diagnostic attempted a retry or a second traversal.");
    }

    public static async Task CompletedSchemaPhaseIsAlwaysNonQualifyingAsync()
    {
        var source = new CountingSource(null);

        var result = await new BoundedSchemaDiagnosticWorkflow(source).ExecuteAsync();

        Assert(!result.IsSuccess && result.Reason == "SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER", "A completed bounded schema phase was incorrectly treated as qualification success.");
        Assert(source.Count == 1, "A completed bounded diagnostic attempted a retry or rerun.");
    }

    private sealed class CountingSource(Blocker? blocker) : ISchemaDiagnosticSource
    {
        public int Count { get; private set; }
        public ValueTask<Blocker?> ReadUntilFirstBlockerAsync(CancellationToken cancellationToken = default) { Count++; return ValueTask.FromResult(blocker); }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
