using System.Net;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Application;

namespace BpmSoftSync.Adapters.BpmSoft;

/// <summary>Sanitized offline-only fake-handler session used to exercise the production read composition.</summary>
public sealed class FixtureBpmSoftCatalogSession : IDisposable
{
    private readonly BpmSoftReadTransport _transport;

    private FixtureBpmSoftCatalogSession(BpmSoftReadTransport transport, string targetAlias, BpmSoftTargetOrigin origin)
    {
        _transport = transport;
        TargetAlias = targetAlias;
        TargetOrigin = origin;
        Source = new BpmSoftFullCatalogSource(transport, origin);
    }

    public string TargetAlias { get; }
    public BpmSoftTargetOrigin TargetOrigin { get; }
    public IFullCatalogSource Source { get; }

    public static async Task<FixtureBpmSoftCatalogSession> OpenAsync(string fixturePath, CancellationToken cancellationToken = default)
    {
        FixtureCatalogSource.VerifyManifest(fixturePath);
        var handler = FixtureHandler.Create(fixturePath, out var targetAlias);
        var origin = BpmSoftTargetOrigin.ParseInteractive("https://fixture.invalid");
        var transport = new BpmSoftReadTransport(origin, handler);
        try
        {
            using var credentials = InteractiveCredentials.Create(origin, "fixture".AsSpan(), "fixture".AsSpan());
            await transport.LoginAsync(credentials, cancellationToken);
            return new FixtureBpmSoftCatalogSession(transport, targetAlias, origin);
        }
        catch
        {
            transport.Dispose();
            throw;
        }
    }

    public void Dispose() => _transport.Dispose();

    private sealed class FixtureHandler : HttpMessageHandler
    {
        private readonly string _workspace;
        private readonly IReadOnlyDictionary<string, string> _schemas;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> _selectPages;

        private FixtureHandler(string workspace, IReadOnlyDictionary<string, string> schemas, IReadOnlyDictionary<string, IReadOnlyDictionary<int, string>> selectPages)
        {
            _workspace = workspace;
            _schemas = schemas;
            _selectPages = selectPages;
        }

        internal static FixtureHandler Create(string fixturePath, out string targetAlias)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));
            var root = document.RootElement;
            if (root.GetProperty("classification").GetString() != "sanitized" || !root.TryGetProperty("targetAlias", out var target) || target.ValueKind != JsonValueKind.String || !IsAlias(target.GetString())) throw new InvalidDataException("OFFLINE_FIXTURE_INVALID");
            targetAlias = target.GetString()!;
            var workspace = root.GetProperty("workspaceResponse").GetRawText();
            var schemas = root.GetProperty("schemas").EnumerateArray().ToDictionary(item => item.GetProperty("schema").GetProperty("uId").GetString()!, item => item.GetRawText(), StringComparer.Ordinal);
            var pages = root.GetProperty("selectResponses").EnumerateObject().ToDictionary(
                schema => schema.Name,
                schema => (IReadOnlyDictionary<int, string>)schema.Value.EnumerateObject().ToDictionary(page => int.Parse(page.Name, System.Globalization.CultureInfo.InvariantCulture), page => page.Value.GetRawText()),
                StringComparer.Ordinal);
            return new FixtureHandler(workspace, schemas, pages);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath;
            if (path == "/ServiceModel/AuthService.svc/Login") return Json("{\"Code\":0}", csrf: true);
            if (path == "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems") return Json(_workspace);
            var body = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            if (path == "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema")
            {
                var key = json.RootElement.GetProperty("schemaUId").GetString() ?? string.Empty;
                return _schemas.TryGetValue(key, out var response) ? Json(response) : Json("{\"success\":false}");
            }
            if (path == "/DataService/json/SyncReply/SelectQuery")
            {
                var schema = json.RootElement.GetProperty("rootSchemaName").GetString() ?? string.Empty;
                var offset = json.RootElement.GetProperty("rowsOffset").GetInt32();
                return _selectPages.TryGetValue(schema, out var pages) && pages.TryGetValue(offset, out var response) ? Json(response) : Json("{\"success\":false}");
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage Json(string body, bool csrf = false)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            if (csrf) response.Headers.TryAddWithoutValidation("Set-Cookie", "BPMCSRF=fixture-csrf; Path=/; Secure; HttpOnly");
            return response;
        }

        private static bool IsAlias(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    }
}
