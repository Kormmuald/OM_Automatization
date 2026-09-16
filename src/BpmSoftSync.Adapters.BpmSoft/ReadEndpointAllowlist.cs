using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

public static class ReadEndpointAllowlist
{
    private static readonly IReadOnlyDictionary<string, Entry> Entries =
        new Dictionary<string, Entry>(StringComparer.Ordinal)
        {
            ["AUTH_LOGIN"] = new(HttpMethod.Post, "/ServiceModel/AuthService.svc/Login", RequestBodyShape.AuthenticationHandshake),
            ["WORKSPACE_ITEMS"] = new(HttpMethod.Post, "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems", RequestBodyShape.EmptyObject),
            ["SCHEMA_GET"] = new(HttpMethod.Post, "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema", RequestBodyShape.SchemaUIdOnly),
            ["SELECT_QUERY"] = new(HttpMethod.Post, "/DataService/json/SyncReply/SelectQuery", RequestBodyShape.CanonicalSelectQuery)
        };

    public static IReadOnlyList<string> EndpointIds { get; } = Array.AsReadOnly(Entries.Keys.ToArray());

    public static SafeResult TryClassify(EndpointCandidate candidate, out EndpointClassification? classification)
    {
        classification = null;
        if (!Entries.TryGetValue(candidate.EndpointId, out var expected) ||
            !string.Equals(candidate.Method, expected.Method.Method, StringComparison.Ordinal) ||
            !string.Equals(candidate.Path, expected.Path, StringComparison.Ordinal) ||
            candidate.BodyShape != expected.Shape ||
            HasPathEscape(candidate.Path) ||
            (candidate.TargetUri is not null && !IsAllowedTarget(candidate.TargetUri)))
        {
            return SafeResult.Blocked(EndpointBlocker());
        }

        classification = new EndpointClassification(candidate.EndpointId, candidate.Method, candidate.Path, candidate.BodyShape, EndpointClassification.ExactAllowlistVersion, candidate.TargetUri);
        return SafeResult.SuccessForHumanReview("offline endpoint classification");
    }

    internal static bool TryClassify(BpmSoftTargetOrigin origin, BpmSoftReadRequestCandidate candidate, out RequestBodyShape shape)
    {
        shape = default;
        if (!Entries.TryGetValue(candidate.RequestId, out var expected) ||
            candidate.Method != expected.Method ||
            !origin.IsExactOrigin(candidate.RequestUri) ||
            !string.Equals(candidate.RequestUri.AbsolutePath, expected.Path, StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(candidate.RequestUri.Query) ||
            !string.IsNullOrEmpty(candidate.RequestUri.Fragment) ||
            HasPathEscape(candidate.RequestUri.OriginalString) ||
            !HasExpectedBody(expected.Shape, candidate.Body))
        {
            return false;
        }

        shape = expected.Shape;
        return true;
    }

    internal static string PathFor(string requestId) => Entries.TryGetValue(requestId, out var entry)
        ? entry.Path
        : throw new BpmSoftTransportException(BpmSoftTransportError.EndpointNotAllowlisted, "ENDPOINT_NOT_ALLOWLISTED: unknown request identifier.");

    internal static bool HasCanonicalSelectQueryBody(ReadOnlyMemory<byte> json) => HasExpectedBody(RequestBodyShape.CanonicalSelectQuery, json);

    private static bool HasExpectedBody(RequestBodyShape shape, ReadOnlyMemory<byte> json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return shape switch
            {
                RequestBodyShape.EmptyObject => IsObjectWithExactProperties(root, Array.Empty<string>()),
                RequestBodyShape.AuthenticationHandshake => HasLoginBody(root),
                RequestBodyShape.SchemaUIdOnly => HasSchemaBody(root),
                // Temporary legacy mode: retain the strict paged form and admit only the
                // fixed historical one-shot form used by LookupCatalogSource.
                RequestBodyShape.CanonicalSelectQuery => HasSelectQueryBody(root) || HasLegacySingleSelectQueryBody(root),
                _ => false
            };
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool HasLoginBody(JsonElement root)
    {
        if (!IsObjectWithExactProperties(root, ["UserName", "UserPassword", "TimeZoneOffset"])) return false;
        return root.GetProperty("UserName").ValueKind == JsonValueKind.String &&
               !string.IsNullOrWhiteSpace(root.GetProperty("UserName").GetString()) &&
               root.GetProperty("UserPassword").ValueKind == JsonValueKind.String &&
               root.GetProperty("TimeZoneOffset").TryGetInt32(out _);
    }

    private static bool HasSchemaBody(JsonElement root)
    {
        if (!IsObjectWithExactProperties(root, ["schemaUId"])) return false;
        var value = root.GetProperty("schemaUId");
        return value.ValueKind == JsonValueKind.String && Guid.TryParseExact(value.GetString(), "D", out var parsed) && parsed != Guid.Empty;
    }

    private static bool HasSelectQueryBody(JsonElement root)
    {
        if (!IsObjectWithExactProperties(root, ["rootSchemaName", "rowCount", "rowsOffset", "isPageable", "allColumns", "useLocalization", "columns"])) return false;
        if (root.GetProperty("rootSchemaName").ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(root.GetProperty("rootSchemaName").GetString()) ||
            !root.GetProperty("rowCount").TryGetInt32(out var rowCount) || rowCount <= 0 ||
            !root.GetProperty("rowsOffset").TryGetInt32(out var rowsOffset) || rowsOffset < 0 ||
            root.GetProperty("isPageable").ValueKind != JsonValueKind.True ||
            root.GetProperty("allColumns").ValueKind != JsonValueKind.False ||
            root.GetProperty("useLocalization").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;

        var columns = root.GetProperty("columns");
        if (!IsObjectWithExactProperties(columns, ["items"])) return false;
        var items = columns.GetProperty("items");
        if (items.ValueKind != JsonValueKind.Object || !items.EnumerateObject().Any()) return false;

        var hasStableIdOrder = false;
        foreach (var item in items.EnumerateObject())
        {
            var column = item.Value;
            if (!IsObjectWithExactProperties(column, ["caption", "orderDirection", "orderPosition", "isVisible", "expression"]) ||
                column.GetProperty("caption").ValueKind != JsonValueKind.String ||
                !column.GetProperty("orderDirection").TryGetInt32(out var direction) ||
                !column.GetProperty("orderPosition").TryGetInt32(out var position) ||
                column.GetProperty("isVisible").ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
            var expression = column.GetProperty("expression");
            if (!IsObjectWithExactProperties(expression, ["expressionType", "columnPath"]) ||
                !expression.GetProperty("expressionType").TryGetInt32(out var expressionType) || expressionType != 0 ||
                expression.GetProperty("columnPath").ValueKind != JsonValueKind.String ||
                !string.Equals(item.Name, expression.GetProperty("columnPath").GetString(), StringComparison.Ordinal)) return false;
            if (item.NameEquals("Id") && direction is 1 or 2 && position == 0) hasStableIdOrder = true;
            else if (direction != 0 || position != -1) return false;
        }
        return hasStableIdOrder;
    }

    private static bool HasLegacySingleSelectQueryBody(JsonElement root) =>
        IsObjectWithExactProperties(root, ["rootSchemaName", "rowCount", "allColumns", "useLocalization"]) &&
        root.GetProperty("rootSchemaName").ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(root.GetProperty("rootSchemaName").GetString()) &&
        root.GetProperty("rowCount").TryGetInt32(out var rowCount) && rowCount == 3000 &&
        root.GetProperty("allColumns").ValueKind == JsonValueKind.True &&
        root.GetProperty("useLocalization").ValueKind is JsonValueKind.True or JsonValueKind.False;

    private static bool IsObjectWithExactProperties(JsonElement element, IReadOnlyCollection<string> names)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;
        var actual = element.EnumerateObject().Select(property => property.Name).ToArray();
        return actual.Length == names.Count && actual.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(names.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
    }

    private static bool IsAllowedTarget(Uri target)
    {
        try { _ = BpmSoftTargetOrigin.ParseInteractive(target.OriginalString); return true; }
        catch (BpmSoftTransportException) { return false; }
    }

    private static bool HasPathEscape(string value) =>
        value.Contains("?", StringComparison.Ordinal) || value.Contains("#", StringComparison.Ordinal) || value.Contains("..", StringComparison.Ordinal) ||
        value.Contains("%2e", StringComparison.OrdinalIgnoreCase) || value.Contains("%2f", StringComparison.OrdinalIgnoreCase) || value.Contains("%5c", StringComparison.OrdinalIgnoreCase);

    private static Blocker EndpointBlocker() => new(BlockerCode.EndpointNotAllowlisted, "endpoint", "ENDPOINT_NOT_ALLOWLISTED", "Use an exact ReadEndpointAllowlist/v1 request.", "Stop the run before send.");

    private sealed record Entry(HttpMethod Method, string Path, RequestBodyShape Shape);
}
