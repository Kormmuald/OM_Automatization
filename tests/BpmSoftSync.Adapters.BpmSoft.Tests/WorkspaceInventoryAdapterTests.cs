using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class WorkspaceInventoryAdapterTests
{
    public static async Task FullTraversalUsesAcceptedTransportAndPreservesObjectModelAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var handler = new FixtureHandler(
            "{\"Code\":0}",
            fixture.RootElement.GetProperty("workspaceResponse").GetRawText(),
            fixture.RootElement.GetProperty("schemas")[0].GetRawText(),
            fixture.RootElement.GetProperty("schemas")[1].GetRawText());
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s02-user", "s02-password".AsSpan());
        await transport.LoginAsync(credentials);

        var result = await WorkspaceInventoryAdapter.ReadFullAsync(transport);

        Assert(result.IsQualified && result.Blocker is null, "Full typed inventory was not qualified.");
        Assert(result.Inventory.Items.Count == 3 && result.Schemas.Count == 2, "The complete workspace/schema count was not preserved.");
        Assert(result.Inventory.Items.Count(item => item.DisplayName == "Account") == 2, "Display-name collision was not preserved as distinct typed identities.");
        Assert(result.Schemas.Select(schema => schema.Identity.SchemaUId).Distinct().Count() == 2, "Schemas with equal names were merged.");
        var account = result.Schemas.Single(schema => schema.Identity.SchemaUId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Assert(account.Identity.ServerSchemaIdCandidate == Guid.Parse("12121212-1212-1212-1212-121212121212") && account.Identity.PackageLayer.PackageName == "Package Alpha", "Server schema candidate or actual package layer was not retained.");
        Assert(account.Columns.Count == 2 && account.Columns[0].Ownership == ColumnOwnership.Own && account.Columns[1].Ownership == ColumnOwnership.Inherited, "Own/inherited columns were not retained.");
        Assert(account.Columns[1].RequirementType == 1 && account.Columns[1].ActualIndexed && account.Columns[0].Reference?.SchemaUId == account.Identity.SchemaUId, "Column requirement/index/reference evidence was not retained.");
        Assert(account.Indexes.Single().Members.Select(member => member.ColumnUId).SequenceEqual(account.Columns.Select(column => column.ColumnUId)), "Composite index members did not use columns[].columnUId in source order.");
        Assert(account.Indexes.Single().Members[0].UnknownProperties.Single().StructuralTree == "string(36)", "Index-member metadata was treated as its relation key instead of a safe envelope.");
        Assert(account.UnknownProperties.Single().StructuralTree.Contains("sanitizedScalar:string(14)", StringComparison.Ordinal) && !account.UnknownProperties.Single().StructuralTree.Contains("S02-RAW-CANARY", StringComparison.Ordinal), "Unknown schema property was dropped or leaked a scalar.");
        Assert(handler.Paths.SequenceEqual(new[] { "/ServiceModel/AuthService.svc/Login", "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems", "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema", "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema" }), "Traversal bypassed the S01 transport or used a non-allowlisted endpoint.");
    }

    public static async Task MalformedSchemaFailsClosedWithScopedBlockerAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var handler = new FixtureHandler("{\"Code\":0}", fixture.RootElement.GetProperty("workspaceResponse").GetRawText(), fixture.RootElement.GetProperty("malformedSchemaResponse").GetRawText(), fixture.RootElement.GetProperty("schemas")[1].GetRawText());
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s02-user", "s02-password".AsSpan());
        await transport.LoginAsync(credentials);

        var result = await new BpmSoftSchemaDiagnosticSource(transport).ReadUntilFirstBlockerAsync();

        Assert(result?.Code == BlockerCode.UnknownShapeUnqualified && result.Reason == "SCHEMA_INVENTORY_UNQUALIFIED", "Bounded diagnostic did not stop with the first scoped named blocker.");
        Assert(handler.Paths.SequenceEqual(new[] { "/ServiceModel/AuthService.svc/Login", "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems", "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema" }, StringComparer.Ordinal), "Bounded diagnostic did not use exactly AUTH_LOGIN, WORKSPACE_ITEMS and one SCHEMA_GET before the blocker.");
    }

    public static async Task BoundedDiagnosticStopsAfterOneDeterministicSuccessfulSchemaAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var workspace = JsonNode.Parse(fixture.RootElement.GetProperty("workspaceResponse").GetRawText())!.AsObject();
        var items = workspace["items"]!.AsArray();
        var reversed = new JsonArray(items.Reverse().Select(item => item!.DeepClone()).ToArray());
        workspace["items"] = reversed;
        var handler = new FixtureHandler(
            "{\"Code\":0}",
            workspace.ToJsonString(),
            fixture.RootElement.GetProperty("schemas")[0].GetRawText());
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s02-user", "s02-password".AsSpan());
        await transport.LoginAsync(credentials);

        var result = await new BoundedSchemaDiagnosticWorkflow(new BpmSoftSchemaDiagnosticSource(transport)).ExecuteAsync();

        Assert(!result.IsSuccess && result.Reason == "SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER", "A strictly valid first deterministic schema response was not converted to the explicit non-qualifying terminal.");
        Assert(handler.Paths.SequenceEqual(new[] { "/ServiceModel/AuthService.svc/Login", "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems", "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema" }, StringComparer.Ordinal), "A successful first schema response triggered a second schema request or another endpoint.");
        Assert(handler.Bodies.Count == 3 && handler.Bodies[2] == "{\"schemaUId\":\"11111111-1111-1111-1111-111111111111\"}", "The bounded diagnostic did not select the stable lowest typed schema candidate.");
    }

    public static async Task BoundedDiagnosticWithoutSchemaCandidateStopsBeforeSchemaGetAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var workspace = JsonNode.Parse(fixture.RootElement.GetProperty("workspaceResponse").GetRawText())!.AsObject();
        foreach (var item in workspace["items"]!.AsArray()) item!["type"] = 9;
        var handler = new FixtureHandler("{\"Code\":0}", workspace.ToJsonString());
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s02-user", "s02-password".AsSpan());
        await transport.LoginAsync(credentials);

        var blocker = await new BpmSoftSchemaDiagnosticSource(transport).ReadUntilFirstBlockerAsync();

        Assert(blocker?.Reason == "SCHEMA_DIAGNOSTIC_NO_SCHEMA_CANDIDATE" && blocker.Code == BlockerCode.FullCatalogNotQualified, "A workspace without a typed schema candidate did not fail closed with the dedicated terminal blocker.");
        Assert(handler.Paths.SequenceEqual(new[] { "/ServiceModel/AuthService.svc/Login", "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems" }, StringComparer.Ordinal), "A workspace without a schema candidate attempted SCHEMA_GET or another endpoint.");
    }

    public static async Task FailedRequiredSchemaPathsProduceOnlyClosedStructuralDiagnosticsAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var cases = new (string Name, string Response, FailedShapePath Path, ExpectedShapeCategory Expected, ObservedJsonKind Observed, ArrayCardinalityBucket Cardinality, int? Ordinal)[]
        {
            ("root", "{\"success\":true}", FailedShapePath.SchemaRoot, ExpectedShapeCategory.Object, ObservedJsonKind.Missing, ArrayCardinalityBucket.NotApplicable, null),
            ("schema-id", MutateSchema(fixture, schema => schema["id"] = null), FailedShapePath.SchemaId, ExpectedShapeCategory.GuidString, ObservedJsonKind.Null, ArrayCardinalityBucket.NotApplicable, null),
            ("package", MutateSchema(fixture, schema => schema["package"] = false), FailedShapePath.SchemaPackage, ExpectedShapeCategory.Object, ObservedJsonKind.Boolean, ArrayCardinalityBucket.NotApplicable, null),
            ("columns", MutateSchema(fixture, schema => schema["columns"] = null), FailedShapePath.SchemaColumns, ExpectedShapeCategory.Array, ObservedJsonKind.Null, ArrayCardinalityBucket.NotApplicable, null),
            ("inherited-columns", MutateSchema(fixture, schema => schema["inheritedColumns"] = false), FailedShapePath.SchemaInheritedColumns, ExpectedShapeCategory.Array, ObservedJsonKind.Boolean, ArrayCardinalityBucket.NotApplicable, null),
            ("parent", MutateSchema(fixture, schema => schema["parentSchema"] = false), FailedShapePath.SchemaParent, ExpectedShapeCategory.OptionalObject, ObservedJsonKind.Boolean, ArrayCardinalityBucket.NotApplicable, null),
            ("index-member", MutateSchema(fixture, schema => schema["indexes"] = new JsonArray(false)), FailedShapePath.SchemaIndexMember, ExpectedShapeCategory.Object, ObservedJsonKind.Boolean, ArrayCardinalityBucket.One, 0),
            ("index-column-member", MutateSchema(fixture, schema => schema["indexes"]![0]!["columns"] = new JsonArray(new JsonObject())), FailedShapePath.SchemaIndexColumnUId, ExpectedShapeCategory.GuidString, ObservedJsonKind.Missing, ArrayCardinalityBucket.One, 0)
        };

        foreach (var testCase in cases)
        {
            var result = await ReadSingleSchemaAsync(fixture, testCase.Response);
            var diagnostic = result.Blocker?.FailedShape;
            Assert(!result.IsQualified && result.Blocker?.Reason == "SCHEMA_INVENTORY_UNQUALIFIED" && diagnostic is not null, testCase.Name + " did not fail closed with a diagnostic.");
            Assert(diagnostic!.Path == testCase.Path && diagnostic.Expected == testCase.Expected && diagnostic.Observed == testCase.Observed && diagnostic.ArrayCardinality == testCase.Cardinality && diagnostic.Ordinal == testCase.Ordinal, testCase.Name + " diagnostic was not the expected closed structural classification.");
            Assert(!diagnostic.ToString().Contains("Account", StringComparison.Ordinal) && !diagnostic.ToString().Contains("S02-RAW-CANARY", StringComparison.Ordinal), testCase.Name + " diagnostic leaked a scalar or display name.");
        }
    }

    public static async Task SchemaPackageIdIsOpaqueOnlyWithAGuidCompanionAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var accepted = new (string Name, string PackageId)[]
        {
            ("opaque", "opaque-package-key"),
            ("guid-looking-is-still-opaque", "99999999-9999-9999-9999-999999999999")
        };

        foreach (var testCase in accepted)
        {
            var result = await ReadSingleSchemaAsync(fixture, MutateSchema(fixture, schema => schema["package"]!["id"] = testCase.PackageId));
            var package = result.Schemas.Single().Identity.PackageLayer;
            Assert(result.IsQualified && result.Blocker is null && package.OpaquePackageId == testCase.PackageId && package.PackageUId is not null, testCase.Name + " did not retain opaque provenance alongside the GUID identity.");
            Assert(package.PrimaryIdentityKey.StartsWith(package.PackageUId!.Value.ToString("D"), StringComparison.OrdinalIgnoreCase) && !package.PrimaryIdentityKey.Contains(testCase.PackageId, StringComparison.Ordinal), testCase.Name + " allowed opaque provenance into the primary identity key.");
        }

        var rejected = new (string Name, Action<JsonObject> Mutate, FailedShapePath Path, ExpectedShapeCategory Expected, ObservedJsonKind Observed, string? ForbiddenScalar)[]
        {
            ("missing", schema => schema["package"]!.AsObject().Remove("id"), FailedShapePath.SchemaPackageId, ExpectedShapeCategory.RequiredString, ObservedJsonKind.Missing, null),
            ("null", schema => schema["package"]!["id"] = null, FailedShapePath.SchemaPackageId, ExpectedShapeCategory.RequiredString, ObservedJsonKind.Null, null),
            ("whitespace", schema => schema["package"]!["id"] = "   ", FailedShapePath.SchemaPackageId, ExpectedShapeCategory.RequiredString, ObservedJsonKind.String, null),
            ("number", schema => schema["package"]!["id"] = 7, FailedShapePath.SchemaPackageId, ExpectedShapeCategory.RequiredString, ObservedJsonKind.Number, null),
            ("object", schema => schema["package"]!["id"] = new JsonObject { ["unexpected"] = "package-id-object-canary" }, FailedShapePath.SchemaPackageId, ExpectedShapeCategory.RequiredString, ObservedJsonKind.Object, "package-id-object-canary"),
            ("array", schema => schema["package"]!["id"] = new JsonArray("package-id-array-canary"), FailedShapePath.SchemaPackageId, ExpectedShapeCategory.RequiredString, ObservedJsonKind.Array, "package-id-array-canary"),
            ("boolean", schema => schema["package"]!["id"] = true, FailedShapePath.SchemaPackageId, ExpectedShapeCategory.RequiredString, ObservedJsonKind.Boolean, null),
            ("invalid-uid", schema => schema["package"]!["uId"] = "not-a-guid", FailedShapePath.SchemaPackageUId, ExpectedShapeCategory.GuidString, ObservedJsonKind.String, "not-a-guid")
        };

        foreach (var testCase in rejected)
        {
            var result = await ReadSingleSchemaAsync(fixture, MutateSchema(fixture, testCase.Mutate));
            var diagnostic = result.Blocker?.FailedShape;
            Assert(!result.IsQualified && result.Blocker?.Reason == "SCHEMA_INVENTORY_UNQUALIFIED" && diagnostic?.Path == testCase.Path && diagnostic.Expected == testCase.Expected && diagnostic.Observed == testCase.Observed, testCase.Name + " did not fail closed with the expected shape contract.");
            Assert(testCase.ForbiddenScalar is null || !diagnostic!.ToString().Contains(testCase.ForbiddenScalar, StringComparison.Ordinal), testCase.Name + " leaked an opaque or invalid scalar.");
        }
    }

    public static async Task SchemaIdentityMismatchFailsClosedBeforeAnotherTraversalAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var handler = new FixtureHandler("{\"Code\":0}", fixture.RootElement.GetProperty("workspaceResponse").GetRawText(), fixture.RootElement.GetProperty("schemas")[1].GetRawText());
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s02-user", "s02-password".AsSpan());
        await transport.LoginAsync(credentials);

        var result = await WorkspaceInventoryAdapter.ReadFullAsync(transport);

        Assert(!result.IsQualified && result.Blocker?.Reason == "SCHEMA_IDENTITY_MISMATCH", "Schema response was accepted for a different workspace/package identity.");
        Assert(handler.Paths.Count == 3, "Traversal continued after an identity mismatch.");
    }

    public static async Task SchemaTransportFailureMarksRequestedItemUnreadableAndReturnsSafeBlockerAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "s02-full-object-model.json")));
        var handler = new SchemaFailureHandler(fixture.RootElement.GetProperty("workspaceResponse").GetRawText());
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s02-user", "s02-password".AsSpan());
        await transport.LoginAsync(credentials);

        var result = await WorkspaceInventoryAdapter.ReadFullAsync(transport);

        var firstSchemaItem = result.Inventory.Items.Single(item => item.Identity.WorkspaceItemUId == Guid.Parse("11111111-1111-1111-1111-111111111111"));
        Assert(!result.IsQualified && result.Blocker?.Reason == "SCHEMA_READ_UNAVAILABLE" && result.Inventory.Items.Count == 3 && result.Schemas.Count == 0, "Schema transport failure did not return a complete safe terminal result.");
        Assert(firstSchemaItem.SupportStatus == SupportStatus.Unreadable && firstSchemaItem.SafeReason == "SCHEMA_READ_UNAVAILABLE:HttpRequestFailed", "Requested schema item was not honestly marked unreadable.");
        Assert(!result.Blocker!.Recovery.Contains("S02-transport-canary", StringComparison.Ordinal) && handler.Paths.Count == 3, "Schema transport failure leaked response content or continued traversal.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string MutateSchema(JsonDocument fixture, Action<JsonObject> mutate)
    {
        var root = JsonNode.Parse(fixture.RootElement.GetProperty("schemas")[0].GetRawText())!.AsObject();
        mutate(root["schema"]!.AsObject());
        return root.ToJsonString();
    }

    private static async Task<WorkspaceObjectModel> ReadSingleSchemaAsync(JsonDocument fixture, string schemaResponse)
    {
        var workspace = JsonNode.Parse(fixture.RootElement.GetProperty("workspaceResponse").GetRawText())!.AsObject();
        foreach (var item in workspace["items"]!.AsArray().Skip(1)) item!["type"] = 9;
        var handler = new FixtureHandler("{\"Code\":0}", workspace.ToJsonString(), schemaResponse);
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s02-user", "s02-password".AsSpan());
        await transport.LoginAsync(credentials);
        return await WorkspaceInventoryAdapter.ReadFullAsync(transport);
    }

    private sealed class FixtureHandler(params string[] bodies) : HttpMessageHandler
    {
        private readonly Queue<string> _bodies = new(bodies);
        public List<string> Paths { get; } = new();
        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_bodies.Dequeue(), Encoding.UTF8, "application/json") };
            if (Paths.Count == 1) response.Headers.TryAddWithoutValidation("Set-Cookie", "BPMCSRF=s02-csrf; Path=/; Secure; HttpOnly");
            return response;
        }
    }

    private sealed class SchemaFailureHandler(string workspaceBody) : HttpMessageHandler
    {
        public List<string> Paths { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            _ = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Paths.Count == 1)
            {
                var login = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"Code\":0}", Encoding.UTF8, "application/json") };
                login.Headers.TryAddWithoutValidation("Set-Cookie", "BPMCSRF=s02-csrf; Path=/; Secure; HttpOnly");
                return login;
            }
            if (Paths.Count == 2) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(workspaceBody, Encoding.UTF8, "application/json") };
            return new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("S02-transport-canary", Encoding.UTF8, "application/json") };
        }
    }
}
