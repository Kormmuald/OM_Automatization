using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.Excel.Tests;

public static class WorkbookContractTests
{
    public static void QualifiedSnapshotProjectsExactWorkbookContracts()
    {
        var snapshot = SyntheticSnapshot.CreateQualification().Snapshot!;
        var pair = WorkbookPairProjection.Create(snapshot);
        var rebound = WorkbookPairProjection.Create(snapshot with { RunId = Guid.NewGuid(), PairId = Guid.NewGuid() });
        Assert(pair.PairBaselineHash == rebound.PairBaselineHash, "Pair baseline hash depends on self-referential RunId/PairId manifest fields.");

        Assert(pair.Model.Sheets.Select(sheet => sheet.Name).SequenceEqual(WorkbookContract.ModelSheetOrder), "Model sheet order differs from contract.");
        Assert(pair.Lookup.Sheets.Select(sheet => sheet.Name).SequenceEqual(WorkbookContract.LookupSheetOrder), "Lookup sheet order differs from contract.");
        AssertHeaders(pair.Model, WorkbookContract.ModelHeaders);
        AssertHeaders(pair.Lookup, WorkbookContract.LookupHeaders);

        var inventory = pair.Model.Sheet("WorkspaceInventory");
        var schemas = pair.Model.Sheet("Schemas");
        var columns = pair.Model.Sheet("Columns");
        var indexes = pair.Model.Sheet("Indexes");
        var registry = pair.Lookup.Sheet("LookupRegistry");
        var values = pair.Lookup.Sheet("LookupValues");
        Assert(inventory.Rows.Count == snapshot.Workspace.Inventory.Items.Count, "Workspace inventory is not a 1:1 projection.");
        Assert(schemas.Rows.Count == snapshot.Workspace.Schemas.Count, "Schemas are not a 1:1 projection.");
        Assert(columns.Rows.Count == snapshot.Workspace.Schemas.Sum(schema => schema.Columns.Count), "Columns are not a 1:1 projection.");
        Assert(indexes.Rows.Count == snapshot.Workspace.Schemas.Sum(schema => schema.Indexes.Sum(index => index.Members.Count)), "Index members are not a 1:1 projection.");
        Assert(registry.Rows.Count == snapshot.Lookups.Registry.Count, "Lookup registry is not a 1:1 projection.");
        Assert(values.Rows.Count == snapshot.Lookups.Collections.Sum(collection => collection.Values.Count), "Lookup values are not a 1:1 projection.");

        var schemaIdentities = schemas.Rows.Select(row => (row[1]!, row[8]!)).ToHashSet();
        var expectedSchemas = snapshot.Workspace.Schemas.Select(schema => (schema.Identity.SchemaUId.ToString("D"), schema.Identity.PackageLayer.PackageUId!.Value.ToString("D"))).ToHashSet();
        Assert(schemaIdentities.SetEquals(expectedSchemas), "Schema/package identities are not projected 1:1.");
        Assert(schemas.Rows.Count(row => row[0] == "CollisionEntity") == 2 && schemas.Rows.Where(row => row[0] == "CollisionEntity").Select(row => row[8]).Distinct().Count() == 2, "Same-name schemas from different packages collided.");

        var columnIdentities = columns.Rows.Select(row => (row[1]!, row[3]!)).ToHashSet();
        var expectedColumns = snapshot.Workspace.Schemas.SelectMany(schema => schema.Columns.Select(column => (schema.Identity.SchemaUId.ToString("D"), column.ColumnUId.ToString("D")))).ToHashSet();
        Assert(columnIdentities.SetEquals(expectedColumns), "Column identities are not projected 1:1.");
        var primaryColumns = columns.Rows.Where(row => row[1] == SyntheticSnapshot.PrimarySchemaUId.ToString("D")).ToDictionary(row => row[2]!, StringComparer.Ordinal);
        Assert(primaryColumns["Name"][10] == "FALSE" && primaryColumns["Name"][11] == "FALSE", "ActualIndexed=false was inferred from index membership.");
        Assert(primaryColumns["Id"][10] == "TRUE" && primaryColumns["Id"][11] == "TRUE", "ActualIndexed=true was lost when independently sourced.");
        Assert(primaryColumns["InheritedContext"][4] == "Inherited" && primaryColumns["InheritedContext"][8] == string.Empty && primaryColumns["InheritedContext"][9] == "True" && primaryColumns["InheritedContext"][10] == string.Empty && primaryColumns["InheritedContext"][11] == "TRUE", "Inherited column editable fields or actual identity semantics are incorrect.");
        Assert(!WorkbookContract.EditableColumns("Columns", primaryColumns["InheritedContext"]).Any(), "Inherited column exposed an editable workbook cell.");
        Assert(indexes.Rows.Count(row => row[2] == SyntheticSnapshot.CompositeIndexUId.ToString("D")) == 2, "Composite index was not projected as one row per member.");
        Assert(indexes.Rows.Any(row => row[2] == SyntheticSnapshot.CompositeIndexUId.ToString("D") && row[6] == SyntheticSnapshot.NameColumnUId.ToString("D") && row[7] == "1"), "Composite member identity/ordinal was not projected.");
        Assert(indexes.Rows.Where(row => row[2] == SyntheticSnapshot.CompositeIndexUId.ToString("D")).Select(row => row[8]).Distinct().Count() == 1, "One composite index received inconsistent actual fingerprints across members.");

        Assert(registry.Rows.Select(row => (row[1]!, row[2]!)).ToHashSet().SetEquals(snapshot.Lookups.Registry.Select(item => (item.SysEntitySchemaUId.ToString("D"), item.LookupRecordId.ToString("D")))), "Lookup registry identities are not projected 1:1.");
        var valueIdentities = values.Rows.Select(row => (row[1]!, row[2]!, row[8]!)).ToHashSet();
        var expectedValueIdentities = snapshot.Lookups.Collections.SelectMany(collection => collection.Rows.SelectMany(row => row.Values.Select(value => (collection.RegistryRecord.SysEntitySchemaUId.ToString("D"), row.RecordId.ToString("D"), value.ColumnName)))).ToHashSet();
        Assert(valueIdentities.SetEquals(expectedValueIdentities), "Lookup value identities are not projected 1:1.");
        var normalized = values.Rows.Where(row => row[1] == SyntheticSnapshot.PrimarySchemaUId.ToString("D")).ToDictionary(row => row[8]!, StringComparer.Ordinal);
        Assert(normalized["Name"][9] == "Value" && normalized["Name"][10] == SyntheticSnapshot.RawLookupValue && normalized["Name"][14] == SyntheticSnapshot.RawLookupValue, "Text lookup normalization changed.");
        Assert(normalized["NullableText"][9] == "Null" && normalized["NullableText"][10] == string.Empty && normalized["NullableText"][14] == string.Empty, "Null was collapsed or omitted.");
        Assert(normalized["EmptyText"][9] == "EmptyString" && normalized["EmptyText"][10] == string.Empty && normalized["EmptyText"][14] == string.Empty, "EmptyString was collapsed or omitted.");
        Assert(Enum.GetValues<LookupValueKind>().Select(kind => kind == LookupValueKind.Reference ? "LookupReference" : kind.ToString()).ToHashSet(StringComparer.Ordinal).SetEquals(normalized.Values.Where(row => row[9] == "Value").Select(row => row[11]!).ToHashSet(StringComparer.Ordinal)), "Supported typed lookup kinds are not all materialized.");
        Assert(normalized["ReferenceValue"][12] == SyntheticSnapshot.SecondaryRecordId.ToString("D"), "Lookup reference identity was not preserved.");

        var manifest = pair.Model.Sheet("Manifest").Rows.ToDictionary(row => row[0]!, row => row[1]!, StringComparer.Ordinal);
        Assert(DateTimeOffset.Parse(manifest["PullStartedUtc"]) == SyntheticSnapshot.PullStartedUtc && DateTimeOffset.Parse(manifest["PullCompletedUtc"]) == SyntheticSnapshot.PullCompletedUtc, "Canonical pull timestamps were not carried from the accepted snapshot.");
        Assert(manifest["PullStartedUtc"] != "not-recorded" && manifest["PullCompletedUtc"] != "not-recorded", "Invented timestamp placeholders remain in the manifest.");
        Assert(manifest["SourceIdentity"] == WorkbookContract.ClosedSourceIdentity(snapshot.SourceIdentity) && manifest["SourceIdentity"].StartsWith("sha256:", StringComparison.Ordinal) && manifest["SourceIdentity"].Length == 71 && !manifest["SourceIdentity"].Contains("fake:", StringComparison.Ordinal), "SourceIdentity was not closed to a safe digest token.");

        foreach (var schema in snapshot.Workspace.Schemas)
        {
            var expectedIdentity = $"schema:{schema.Identity.SchemaUId:D}:layer:{schema.Identity.PackageLayer.PackageId:D}:{schema.Identity.PackageLayer.PackageUId:D}:{schema.Identity.PackageLayer.LayerKind}";
            var expected = snapshot.ComponentDigests.Single(item => item.ComponentKind == "schema" && item.StableIdentity == expectedIdentity).Digest;
            Assert(schemas.Rows.Single(row => row[1] == schema.Identity.SchemaUId.ToString("D"))[11] == expected, "Schema ActualFingerprint is not the exact qualified component digest.");
        }

        var primary = snapshot.Workspace.Schemas.Single(schema => schema.Identity.SchemaUId == SyntheticSnapshot.PrimarySchemaUId);
        var mutatedColumns = primary.Columns.Select(column => column.ColumnUId == SyntheticSnapshot.NameColumnUId ? column with { RequirementType = 0 } : column).ToArray();
        var mutatedIndexes = primary.Indexes.Select(index => index.IndexUId == SyntheticSnapshot.CompositeIndexUId ? index with { AutoName = true } : index).ToArray();
        var mutatedPrimary = primary with { Columns = mutatedColumns, Indexes = mutatedIndexes };
        var mutatedWorkspace = snapshot.Workspace with { Schemas = snapshot.Workspace.Schemas.Select(schema => schema.Identity.SchemaUId == SyntheticSnapshot.PrimarySchemaUId ? mutatedPrimary : schema).ToArray() };
        var mutatedPair = WorkbookPairProjection.Create(snapshot with { Workspace = mutatedWorkspace });
        Assert(mutatedPair.Model.Sheet("Columns").Rows.Single(row => row[3] == SyntheticSnapshot.NameColumnUId.ToString("D"))[14] != primaryColumns["Name"][14], "Column ActualFingerprint omitted requirement semantics.");
        Assert(mutatedPair.Model.Sheet("Indexes").Rows.Single(row => row[2] == SyntheticSnapshot.CompositeIndexUId.ToString("D") && row[7] == "0")[8] != indexes.Rows.Single(row => row[2] == SyntheticSnapshot.CompositeIndexUId.ToString("D") && row[7] == "0")[8], "Index ActualFingerprint omitted AutoName/composite semantics.");
    }

    private static void AssertHeaders(WorkbookProjection workbook, IReadOnlyDictionary<string, string[]> expected)
    {
        foreach (var sheet in workbook.Sheets)
            Assert(sheet.Headers.SequenceEqual(expected[sheet.Name]), $"Headers differ for {sheet.Name}.");
    }

    internal static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
