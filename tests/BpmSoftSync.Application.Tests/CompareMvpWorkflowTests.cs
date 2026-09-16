using BpmSoftSync.Domain;

namespace BpmSoftSync.Application.Tests;

internal static class CompareMvpWorkflowTests
{
    public static void SyntheticHappyPathProducesTypedPlanOperations()
    {
        var fixture = Fixture();
        var result = new BpmSoftSync.Application.CompareMvpWorkflow().Execute(fixture.Pair, fixture.Workspace, fixture.Lookups);
        Assert(result.Status == "completed" && result.Operations.Count == 2 && result.Operations.Any(item => item.Kind == "Columns.DesiredRequired") && result.Operations.Any(item => item.Kind == "LookupValues.Value" && item.DesiredState == "Value" && item.ValueKind == "Integer") && result.Blockers.Count == 0, "Synthetic compare happy-path did not create only supported typed operations.");
    }

    public static void SyntheticBlockerDoesNotSuppressIndependentOperation()
    {
        var fixture = Fixture(columnUId: "");
        var result = new BpmSoftSync.Application.CompareMvpWorkflow().Execute(fixture.Pair, fixture.Workspace, fixture.Lookups);
        Assert(result.Status == "completed_with_blockers" && result.Operations.Count == 1 && result.Operations[0].Kind == "Columns.DesiredRequired" && result.Blockers.Any(item => item.Code == "LOOKUP_VALUE_COLUMN_IDENTITY_MISSING"), "An independent blocker suppressed the supported required-column operation.");
    }

    public static void UntouchedInheritedColumnIsIgnored()
    {
        var fixture = Fixture();
        var inherited = Row(("ParentSchemaUId", fixture.Workspace.Schemas[0].Identity.SchemaUId.ToString("D")), ("ColumnUId", fixture.Workspace.Schemas[0].Columns[0].ColumnUId.ToString("D")), ("Ownership", "Inherited"), ("DesiredState", "Active"), ("DesiredRequired", ""), ("DesiredIndexed", ""), ("ActualIndexed", "FALSE"));
        var pair = fixture.Pair with { Columns = [inherited], LookupValues = [] };
        var result = new BpmSoftSync.Application.CompareMvpWorkflow().Execute(pair, fixture.Workspace, new LookupCatalog(true, [], null, [], null));
        Assert(result.Operations.Count == 0 && result.Blockers.Count == 0, "An untouched inherited column became a Compare MVP blocker.");
    }

    private static (CompareWorkbookPair Pair, WorkspaceObjectModel Workspace, LookupCatalog Lookups) Fixture(string? columnUId = null)
    {
        var schemaId = Guid.Parse("10000000-0000-0000-0000-000000000001"); var requiredId = Guid.Parse("20000000-0000-0000-0000-000000000001"); var valueId = Guid.Parse("20000000-0000-0000-0000-000000000002"); var recordId = Guid.Parse("30000000-0000-0000-0000-000000000001");
        var package = new PackageLayerIdentity("synthetic", Guid.Parse("40000000-0000-0000-0000-000000000001"), "Current");
        var identity = new SchemaIdentity("Synthetic", schemaId, null, null, null, package);
        var required = new EntityColumnModel("Required", requiredId, 0, ColumnOwnership.Own, 1, 0, false, null, []);
        var value = new EntityColumnModel("Count", valueId, 1, ColumnOwnership.Own, 4, 0, false, null, []);
        var workspace = new WorkspaceObjectModel(true, WorkspaceInventory.Create([]), [new EntitySchemaModel(identity, [required, value], [], [])], null);
        var actual = new NormalizedLookupValue(recordId, valueId, "Count", LookupValueState.Value, LookupValueKind.Integer, new LookupIntegerValue(1), "1", null, new string('a', 64), new string('b', 64));
        var registry = new LookupRegistryRecord(Guid.Parse("50000000-0000-0000-0000-000000000001"), schemaId, identity, null, new string('c', 64));
        var collection = new LookupCollection(registry, workspace.Schemas[0], [new LookupRow(recordId, [actual], new string('d', 64), new string('e', 64))], new OrderedCollectionManifest("lookup:Synthetic", "Id", [], new(1, 1, 1, "0-16KiB", TimeSpan.Zero, new string('f', 64)), new string('g', 64)), new string('h', 64));
        var lookups = new LookupCatalog(true, [registry], new OrderedCollectionManifest("lookup-registry", "Id", [], new(1, 1, 1, "0-16KiB", TimeSpan.Zero, new string('i', 64)), new string('j', 64)), [collection], null);
        var columns = new[] { Row(("ParentSchemaUId", schemaId.ToString("D")), ("ColumnUId", requiredId.ToString("D")), ("Ownership", "Own"), ("DesiredState", "Active"), ("DesiredRequired", "TRUE"), ("DesiredIndexed", ""), ("ActualIndexed", "FALSE")) };
        var values = new[] { Row(("SysEntitySchemaUId", schemaId.ToString("D")), ("RecordId", recordId.ToString("D")), ("ColumnUId", columnUId ?? valueId.ToString("D")), ("ColumnName", "Count"), ("DesiredState", "Active"), ("DraftRowToken", ""), ("ReferenceDraftRowToken", ""), ("Comment", ""), ("ValueKind", "Integer"), ("ValueState", "Value"), ("Value", "2"), ("ReferenceRecordId", "")) };
        return (new CompareWorkbookPair("fixture", new string('1', 64), new string('2', 64), new string('3', 64), columns, values), workspace, lookups);
    }
    private static IReadOnlyDictionary<string, string> Row(params (string Key, string Value)[] values) => values.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
