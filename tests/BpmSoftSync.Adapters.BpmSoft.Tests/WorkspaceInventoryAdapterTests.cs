using System.Net;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Adapters.BpmSoft;
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

        var result = await WorkspaceInventoryAdapter.ReadFullAsync(transport);

        Assert(!result.IsQualified && result.Blocker?.Code == BlockerCode.UnknownShapeUnqualified && result.Blocker.Reason == "SCHEMA_INVENTORY_UNQUALIFIED", "Malformed schema did not stop with a scoped named blocker.");
        Assert(handler.Paths.Count == 3, "Traversal continued after the first malformed schema.");
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

    private sealed class FixtureHandler(params string[] bodies) : HttpMessageHandler
    {
        private readonly Queue<string> _bodies = new(bodies);
        public List<string> Paths { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            _ = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
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
