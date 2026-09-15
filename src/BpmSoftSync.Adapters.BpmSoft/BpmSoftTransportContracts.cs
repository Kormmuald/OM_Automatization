using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BpmSoftSync.Adapters.BpmSoft;

public enum BpmSoftTransportError
{
    TargetOriginNotAllowed,
    EndpointNotAllowlisted,
    SessionRequired,
    SessionDisposed,
    LoginFailed,
    CsrfCookieMissing,
    RedirectOrAlternateOrigin,
    HttpRequestFailed,
    ResponseTooLarge,
    ResponseLimitInvalid,
    ResponseInvalidJson,
    ResponseEnvelopeInvalid,
    RequestTimedOut,
    InteractiveTerminalRequired
}

public sealed class BpmSoftTransportException(BpmSoftTransportError code, string safeMessage, Exception? innerException = null)
    : Exception(safeMessage, innerException)
{
    public BpmSoftTransportError Code { get; } = code;
}

public sealed record BpmSoftHttpOptions(TimeSpan RequestTimeout, int MaxResponseBytes)
{
    public const int MaximumSupportedResponseBytes = int.MaxValue - 1;
    public static BpmSoftHttpOptions Default { get; } = new(TimeSpan.FromSeconds(30), 16 * 1024 * 1024);
}

public sealed record BpmSoftLoginResponse(bool Authenticated);

public abstract class BpmSoftJsonResponse : IDisposable
{
    private JsonDocument? _document;

    protected BpmSoftJsonResponse(string requestId, JsonDocument document, HttpStatusCode statusCode, int responseBytes)
    {
        RequestId = requestId;
        _document = document;
        StatusCode = statusCode;
        ResponseBytes = responseBytes;
    }

    public string RequestId { get; }
    public HttpStatusCode StatusCode { get; }
    public int ResponseBytes { get; }
    [JsonIgnore]
    public JsonElement Root => (_document ?? throw new ObjectDisposedException(GetType().Name)).RootElement;
    public void Dispose() => Interlocked.Exchange(ref _document, null)?.Dispose();
}

public sealed class WorkspaceItemsResponse(JsonDocument document, HttpStatusCode statusCode, int responseBytes)
    : BpmSoftJsonResponse("WORKSPACE_ITEMS", document, statusCode, responseBytes);

public sealed class SchemaResponse(JsonDocument document, HttpStatusCode statusCode, int responseBytes)
    : BpmSoftJsonResponse("SCHEMA_GET", document, statusCode, responseBytes);

public sealed class SelectQueryResponse(JsonDocument document, HttpStatusCode statusCode, int responseBytes)
    : BpmSoftJsonResponse("SELECT_QUERY", document, statusCode, responseBytes);
