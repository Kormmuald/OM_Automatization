using System.Net;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class LookupCatalogSourceTests
{
    private static readonly Guid AlphaSchemaUId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    private static readonly Guid BetaSchemaUId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");

    public static async Task FullRegistryAndEveryDiscoveredCollectionAreLosslessAsync()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "lookup-full-catalog.json")));
        var handler = new LookupFixtureHandler(fixture.RootElement.GetProperty("responses"));
        using var transport = await LoggedInTransportAsync(handler);

        var result = await LookupCatalogSource.ReadFullAsync(transport, CreateObjectModel(), new LookupReadLimits(2, 8, 32, 1_000_000));

        Assert(result.IsQualified && result.Blocker is null, "Full lookup catalog did not qualify.");
        Assert(result.Registry.Count == 2 && result.Collections.Count == 2, "Registry or discovered collection was silently omitted.");
        var alpha = result.Collections.Single(collection => collection.Schema.Identity.SchemaUId == AlphaSchemaUId);
        Assert(alpha.RegistryRecord.LookupRecordId == Guid.Parse("10000000-0000-0000-0000-000000000001"), "LookupRecordId was not bound to the exact registry row.");
        Assert(alpha.RegistryRecord.SchemaIdentity.PackageLayer.PackageName == "Package Alpha" && alpha.RegistryRecord.BaseSchemaIdentity?.SchemaUId == Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "Registry relation lost schema layer or base schema identity.");
        Assert(alpha.Rows.Count == 3 && alpha.Values.Count == 30, "A multipage lookup row or supported non-Id column value was omitted.");
        Assert(alpha.Rows.Select(row => row.RecordId).Distinct().Count() == alpha.Rows.Count, "Lookup row identity was not preserved.");
        Assert(alpha.Rows.All(row => row.RowFingerprint.Length == 64 && row.SourceFingerprint.Length == 64) && alpha.Values.All(value => value.SourceFingerprint.Length == 64), "Row/value source fingerprints are incomplete.");
        Assert(alpha.Values.Any(value => value.State == LookupValueState.Null) && alpha.Values.Any(value => value.State == LookupValueState.EmptyString) && alpha.Values.Any(value => value.State == LookupValueState.Value), "Null|EmptyString|Value distinction was lost.");
        Assert(alpha.Values.Any(value => value.ValueKind == LookupValueKind.Text && value.CanonicalValue == "Caf\u00e9") && alpha.Values.Any(value => value.ValueKind == LookupValueKind.Decimal && value.CanonicalValue == "1.23"), "Text NFC or invariant decimal canonicalization failed.");
        Assert(alpha.Values.Any(value => value.TypedValue is LookupIntegerValue { Value: 1 }) && alpha.Values.Any(value => value.TypedValue is LookupBooleanValue { Value: true }) && alpha.Values.Any(value => value.TypedValue is LookupGuidValue), "Typed scalar values were reduced to untyped strings.");
        Assert(alpha.Values.Any(value => value.ValueKind == LookupValueKind.DateTime && value.CanonicalValue == "2026-09-14T09:34:56.0000000Z"), "DateTime canonicalization did not normalize to UTC.");
        Assert(alpha.Values.Any(value => value.ValueKind == LookupValueKind.Reference && value.ReferenceRecordId == Guid.Parse("90000000-0000-0000-0000-000000000001")), "ReferenceRecordId was not retained.");
        var beta = result.Collections.Single(collection => collection.Schema.Identity.SchemaUId == BetaSchemaUId);
        Assert(beta.Values.Any(value => value.ColumnName == "Usr_MixedCase42" && value.ValueKind == LookupValueKind.Text), "Nonstandard lookup column was omitted.");
        Assert(result.RegistryManifest!.Telemetry.RowCount == 2 && result.Collections.All(collection => collection.Manifest.Telemetry.PageCount >= 2), "Safe paging telemetry is incomplete.");

        foreach (var request in handler.SelectRequests)
        {
            Assert(request.GetProperty("allColumns").ValueKind == JsonValueKind.False, "SelectQuery used allColumns=true.");
            var items = request.GetProperty("columns").GetProperty("items");
            Assert(items.TryGetProperty("Id", out var id) && id.GetProperty("orderDirection").GetInt32() == 1 && id.GetProperty("orderPosition").GetInt32() == 0, "SelectQuery did not declare stable Id order.");
            Assert(items.EnumerateObject().All(item => item.Value.GetProperty("expression").GetProperty("columnPath").GetString() == item.Name), "SelectQuery did not use explicit canonical columns.");
        }
        Assert(handler.Paths.All(path => path is "/ServiceModel/AuthService.svc/Login" or "/DataService/json/SyncReply/SelectQuery"), "S03 bypassed the accepted S01 transport boundary.");

        var repeatHandler = new LookupFixtureHandler(fixture.RootElement.GetProperty("responses"));
        using var repeatTransport = await LoggedInTransportAsync(repeatHandler);
        var repeat = await LookupCatalogSource.ReadFullAsync(repeatTransport, CreateObjectModel(), new LookupReadLimits(2, 8, 32, 1_000_000));
        Assert(repeat.IsQualified && result.Registry.Select(item => item.SourceFingerprint).SequenceEqual(repeat.Registry.Select(item => item.SourceFingerprint), StringComparer.Ordinal), "Registry source fingerprints are not deterministic.");
        Assert(result.Collections.Select(item => item.SourceFingerprint).SequenceEqual(repeat.Collections.Select(item => item.SourceFingerprint), StringComparer.Ordinal), "Lookup collection source fingerprints are not deterministic.");
    }

    public static async Task EveryPagingAndLimitFailureIsTerminalAsync()
    {
        foreach (var scenario in new[] { "duplicate", "overlap", "gap", "missing-offset", "reordered-intrapage", "reordered-cross-page", "loop", "empty-middle", "nonempty-after-terminal", "max-pages", "max-rows", "max-bytes" })
        {
            var handler = new PagingScenarioHandler(scenario, AlphaSchemaUId);
            using var transport = await LoggedInTransportAsync(handler);
            var limits = scenario switch
            {
                "max-pages" => new LookupReadLimits(2, 1, 32, 1_000_000),
                "max-rows" => new LookupReadLimits(2, 8, 1, 1_000_000),
                "max-bytes" => new LookupReadLimits(2, 8, 32, 32),
                _ => new LookupReadLimits(2, 8, 32, 1_000_000)
            };

            var result = await LookupCatalogSource.ReadFullAsync(transport, CreateObjectModel(alphaOnly: true), limits);

            Assert(!result.IsQualified && result.Blocker?.Code == BlockerCode.CatalogOrderOrPagingUnqualified, $"{scenario} did not terminate with the named paging blocker.");
            if (scenario == "missing-offset")
            {
                Assert(result.Blocker!.Reason == "LOOKUP_PAGE_OFFSET_UNQUALIFIED", "Absent rowsOffset was not rejected at the response boundary.");
                Assert(handler.TargetCollectionRequestCount == 1, "Absent rowsOffset did not stop on the first malformed collection page.");
            }
            if (scenario is "reordered-intrapage" or "reordered-cross-page")
            {
                Assert(result.Blocker!.Reason == "LOOKUP_ID_ORDER_UNQUALIFIED", $"{scenario} was not rejected by strict ascending Id validation.");
                Assert(handler.TargetCollectionRequestCount == (scenario == "reordered-intrapage" ? 1 : 2), $"{scenario} did not stop at the first observable order violation.");
            }
            Assert(handler.SendCount < 12, $"{scenario} did not terminate within the bounded fake HTTP sequence.");
        }
    }

    public static async Task UnsupportedAndMalformedValuesFailClosedWithoutRawValueLeakAsync()
    {
        var unsupportedModel = CreateObjectModel(alphaOnly: true, unsupportedType: true);
        var unsupportedHandler = new PagingScenarioHandler("valid", AlphaSchemaUId);
        using (var transport = await LoggedInTransportAsync(unsupportedHandler))
        {
            var result = await LookupCatalogSource.ReadFullAsync(transport, unsupportedModel, new LookupReadLimits(2, 8, 32, 1_000_000));
            Assert(!result.IsQualified && result.Blocker?.Code == BlockerCode.UnknownShapeUnqualified && result.Blocker.Scope.Contains(AlphaSchemaUId.ToString("D"), StringComparison.Ordinal), "Unsupported column type was skipped or not scoped to exact schema identity.");
            Assert(unsupportedHandler.TargetCollectionRequestCount == 0, "Unsupported column type was detected only after a partial collection read.");
        }

        foreach (var scenario in new[] { "invalid-guid", "invalid-reference", "missing-column", "unexpected-column", "not-found-column", "malformed-select" })
        {
            var handler = new PagingScenarioHandler(scenario, AlphaSchemaUId);
            using var transport = await LoggedInTransportAsync(handler);
            var result = await LookupCatalogSource.ReadFullAsync(transport, CreateObjectModel(alphaOnly: true), new LookupReadLimits(2, 8, 32, 1_000_000));
            Assert(!result.IsQualified && result.Blocker?.Code == BlockerCode.UnknownShapeUnqualified, $"{scenario} value/shape was silently accepted.");
            var safeText = string.Join("|", result.Blocker!.Reason, result.Blocker.Scope, result.Blocker.Recovery, result.Blocker.NextPermittedAction);
            Assert(!safeText.Contains("S03-RAW-CANARY", StringComparison.Ordinal), $"{scenario} leaked a raw lookup value through diagnostics.");
        }
    }

    public static async Task TimeoutAndCancellationRemainBoundedAndSafeAsync()
    {
        var timeoutHandler = new SlowSelectHandler();
        using (var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), timeoutHandler, new BpmSoftHttpOptions(TimeSpan.FromMilliseconds(20), 1_000_000)))
        {
            using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s03-user", "s03-password".AsSpan());
            await transport.LoginAsync(credentials);
            var result = await LookupCatalogSource.ReadFullAsync(transport, CreateObjectModel(alphaOnly: true), new LookupReadLimits(2, 8, 32, 1_000_000));
            Assert(!result.IsQualified && result.Blocker?.Code == BlockerCode.CatalogOrderOrPagingUnqualified && result.Blocker.Reason == "LOOKUP_READ_UNAVAILABLE:RequestTimedOut", "SelectQuery timeout did not become a safe bounded paging blocker.");
        }

        var cancellationHandler = new PagingScenarioHandler("valid", AlphaSchemaUId);
        using (var transport = await LoggedInTransportAsync(cancellationHandler))
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            try
            {
                _ = await LookupCatalogSource.ReadFullAsync(transport, CreateObjectModel(alphaOnly: true), new LookupReadLimits(2, 8, 32, 1_000_000), cancellation.Token);
                throw new InvalidOperationException("Canceled lookup read was silently accepted.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                Assert(cancellationHandler.SendCount == 1, "Canceled lookup read sent a SelectQuery request.");
            }
        }
    }

    private static async Task<BpmSoftReadTransport> LoggedInTransportAsync(HttpMessageHandler handler)
    {
        var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "s03-user", "s03-password".AsSpan());
        await transport.LoginAsync(credentials);
        return transport;
    }

    private static WorkspaceObjectModel CreateObjectModel(bool alphaOnly = false, bool unsupportedType = false)
    {
        var alpha = Schema("LookupAlpha", AlphaSchemaUId, "Package Alpha", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
        [
            Column("Id", 0), Column("Name", 29), Column("Description", 30), Column("SortOrder", 4), Column("Weight", 26), Column("IsActive", 12),
            Column("ChangedOn", 7), Column("EffectiveDate", 8), Column("LocalTime", 9),
            Column("Owner", 10, new SchemaReference("Contact", Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"))),
            Column("ExternalGuid", unsupportedType ? 99 : 0)
        ]);
        var beta = Schema("LookupBeta_Ext", BetaSchemaUId, "Package Beta", Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null,
        [
            Column("Id", 0), Column("Usr_MixedCase42", 1), Column("Amount", 6),
            Column("OptionalReference", 10, new SchemaReference("Account", Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd")))
        ]);
        var schemas = alphaOnly ? new[] { alpha } : new[] { alpha, beta };
        var items = schemas.Select(schema => new WorkspaceInventoryItem(
            new WorkspaceItemIdentity(schema.Identity.SchemaUId, schema.Identity.PackageLayer, "EntitySchema", schema.Identity.SchemaUId),
            SupportStatus.Structured, "BPMSOFT_WORKSPACE_ENTITY_SCHEMA", null, schema.Identity.SchemaName, [])).ToArray();
        var inventory = WorkspaceInventory.Create(items);
        return new WorkspaceObjectModel(true, inventory, schemas, null);
    }

    private static EntitySchemaModel Schema(string name, Guid schemaUId, string packageName, Guid packageId, Guid? parentUId, IReadOnlyList<EntityColumnModel> columns) =>
        new(new SchemaIdentity(name, schemaUId, DeterministicGuid(name + "-server"), parentUId is null ? null : "BaseLookup", parentUId,
            new PackageLayerIdentity(packageId.ToString("D"), packageId, "schema-package", packageName)), columns, [], []);

    private static EntityColumnModel Column(string name, int type, SchemaReference? reference = null) =>
        new(name, DeterministicGuid(name), 0, ColumnOwnership.Own, type, 0, false, reference, []);

    private static Guid DeterministicGuid(string text)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(text));
        return new Guid(bytes);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class LookupFixtureHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, Queue<string>> _responses;
        public List<string> Paths { get; } = [];
        public List<JsonElement> SelectRequests { get; } = [];

        public LookupFixtureHandler(JsonElement responses) => _responses = responses.EnumerateObject()
            .ToDictionary(property => property.Name, property => new Queue<string>(property.Value.EnumerateArray().Select(item => item.GetRawText())), StringComparer.Ordinal);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            if (Paths.Count == 1) return LoginResponse();
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            SelectRequests.Add(document.RootElement.Clone());
            var schema = document.RootElement.GetProperty("rootSchemaName").GetString()!;
            return JsonResponse(_responses[schema].Dequeue());
        }
    }

    private sealed class PagingScenarioHandler(string scenario, Guid schemaUId) : HttpMessageHandler
    {
        private int _lookupCalls;
        private int _targetCalls;
        public int SendCount { get; private set; }
        public int TargetCollectionRequestCount => _targetCalls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            if (SendCount == 1) return LoginResponse();
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var schema = document.RootElement.GetProperty("rootSchemaName").GetString()!;
            var offset = document.RootElement.GetProperty("rowsOffset").GetInt32();
            if (schema == "Lookup")
            {
                _lookupCalls++;
                return JsonResponse(_lookupCalls == 1
                    ? Rows(offset, $"{{\"Id\":\"10000000-0000-0000-0000-000000000001\",\"SysEntitySchemaUId\":\"{schemaUId:D}\"}}")
                    : Rows(offset));
            }

            _targetCalls++;
            var one = ValidRow("20000000-0000-0000-0000-000000000001");
            var two = ValidRow("20000000-0000-0000-0000-000000000002");
            var three = ValidRow("20000000-0000-0000-0000-000000000003");
            return scenario switch
            {
                "duplicate" => JsonResponse(Rows(offset, one, one)),
                "overlap" when _targetCalls == 1 => JsonResponse(Rows(offset, one, two)),
                "overlap" => JsonResponse(Rows(offset, two)),
                "gap" => JsonResponse(Rows(offset + 1, one)),
                "missing-offset" => JsonResponse($"{{\"success\":true,\"notFoundColumns\":[],\"rows\":[{one}]}}"),
                "reordered-intrapage" => JsonResponse(Rows(offset, two, one)),
                "reordered-cross-page" when _targetCalls == 1 => JsonResponse(Rows(offset, one, three)),
                "reordered-cross-page" => JsonResponse(Rows(offset, two)),
                "loop" => JsonResponse(Rows(offset, one, two)),
                "empty-middle" when _targetCalls == 1 => JsonResponse(Rows(offset, one)),
                "empty-middle" when _targetCalls == 2 => JsonResponse(Rows(offset)),
                "empty-middle" => JsonResponse(Rows(offset, two)),
                "nonempty-after-terminal" when _targetCalls == 1 => JsonResponse(Rows(offset)),
                "nonempty-after-terminal" => JsonResponse(Rows(offset, one)),
                "max-pages" or "max-rows" => JsonResponse(Rows(offset, one, two)),
                "max-bytes" => JsonResponse(Rows(offset, one)),
                "invalid-guid" => JsonResponse(Rows(offset, ValidRow("not-a-guid"))),
                "invalid-reference" => JsonResponse(Rows(offset, ValidRow("20000000-0000-0000-0000-000000000001", owner: "{\"value\":\"S03-RAW-CANARY\"}"))),
                "missing-column" => JsonResponse(Rows(offset, ValidRow("20000000-0000-0000-0000-000000000001").Replace(",\"Name\":\"Safe\"", string.Empty, StringComparison.Ordinal))),
                "unexpected-column" => JsonResponse(Rows(offset, ValidRow("20000000-0000-0000-0000-000000000001").TrimEnd('}') + ",\"Unexpected\":\"S03-RAW-CANARY\"}")),
                "not-found-column" => JsonResponse("{\"success\":true,\"notFoundColumns\":[\"Name\"],\"rows\":[]}"),
                "malformed-select" => JsonResponse("{\"success\":true,\"notFoundColumns\":[]}"),
                "valid" when _targetCalls == 1 => JsonResponse(Rows(offset, one)),
                _ => JsonResponse(Rows(offset))
            };
        }

        private static string ValidRow(string id, string owner = "null") =>
            $"{{\"Id\":\"{id}\",\"Name\":\"Safe\",\"Description\":\"\",\"SortOrder\":1,\"Weight\":1.5,\"IsActive\":true,\"ChangedOn\":\"2026-09-14T00:00:00Z\",\"EffectiveDate\":\"2026-09-14\",\"LocalTime\":\"01:02:03\",\"Owner\":{owner},\"ExternalGuid\":\"30000000-0000-0000-0000-000000000001\"}}";
    }

    private sealed class SlowSelectHandler : HttpMessageHandler
    {
        private int _calls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _calls++;
            if (_calls == 1) return LoginResponse();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Unreachable fake handler path.");
        }
    }

    private static HttpResponseMessage LoginResponse()
    {
        var response = JsonResponse("{\"Code\":0}");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "BPMCSRF=s03-csrf; Path=/; Secure; HttpOnly");
        return response;
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private static string Rows(int reportedOffset, params string[] rows) => $"{{\"success\":true,\"notFoundColumns\":[],\"rowsOffset\":{reportedOffset},\"rows\":[{string.Join(',', rows)}]}}";
}
