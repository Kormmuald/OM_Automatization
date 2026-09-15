using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BpmSoftSync.Adapters.BpmSoft;

public sealed class BpmSoftReadTransport : IDisposable
{
    private readonly BpmSoftTargetOrigin _origin;
    private readonly BpmSoftHttpOptions _options;
    private readonly HttpClient _client;
    private InMemoryReadOnlySession? _session;
    private bool _disposed;

    public BpmSoftReadTransport(BpmSoftTargetOrigin origin, HttpMessageHandler handler, BpmSoftHttpOptions? options = null)
        : this(origin, handler, options, disposeHandler: false) { }

    private BpmSoftReadTransport(BpmSoftTargetOrigin origin, HttpMessageHandler handler, BpmSoftHttpOptions? options, bool disposeHandler)
    {
        _origin = origin;
        _options = options ?? BpmSoftHttpOptions.Default;
        if (_options.RequestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "HTTP timeout must be positive.");
        if (_options.MaxResponseBytes <= 0 || _options.MaxResponseBytes > BpmSoftHttpOptions.MaximumSupportedResponseBytes)
            throw new BpmSoftTransportException(BpmSoftTransportError.ResponseLimitInvalid, $"RESPONSE_LIMIT_INVALID: response bound must be between 1 and {BpmSoftHttpOptions.MaximumSupportedResponseBytes} bytes.");
        _client = new HttpClient(handler, disposeHandler) { Timeout = Timeout.InfiniteTimeSpan };
    }

    public bool HasEphemeralSession => !_disposed && _session?.HasEphemeralState == true;

    public static BpmSoftReadTransport CreateProduction(BpmSoftTargetOrigin origin, BpmSoftHttpOptions? options = null)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        return new BpmSoftReadTransport(origin, handler, options, disposeHandler: true);
    }

    public async Task<BpmSoftLoginResponse> LoginAsync(InteractiveCredentials credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureNotDisposed();
            if (!SameOrigin(credentials.TargetOrigin.Uri, _origin.Uri))
                throw new BpmSoftTransportException(BpmSoftTransportError.TargetOriginNotAllowed, "TARGET_ORIGIN_NOT_ALLOWED: credentials and transport origins differ.");

            _session?.Dispose();
            _session = null;
            using var candidate = BpmSoftReadRequestCandidate.Create(_origin, "AUTH_LOGIN", credentials.CreateLoginBody(-(int)TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now).TotalMinutes));
            using var raw = await SendCoreAsync(candidate, requiresSession: false, cancellationToken);
            if (!raw.Document.RootElement.TryGetProperty("Code", out var code) || !code.TryGetInt32(out var numericCode) || numericCode != 0)
                throw new BpmSoftTransportException(BpmSoftTransportError.LoginFailed, "LOGIN_FAILED: BPMSoft did not confirm authentication.");

            _session = InMemoryReadOnlySession.Create(_origin, raw.SetCookieHeaders);
            return new BpmSoftLoginResponse(true);
        }
        finally
        {
            credentials.Dispose();
        }
    }

    public async Task<WorkspaceItemsResponse> GetWorkspaceItemsAsync(CancellationToken cancellationToken = default)
    {
        using var candidate = BpmSoftReadRequestCandidate.Create(_origin, "WORKSPACE_ITEMS", "{}"u8.ToArray());
        using var raw = await SendCoreAsync(candidate, requiresSession: true, cancellationToken);
        EnsureSuccessfulEnvelope(raw.Document.RootElement, "WORKSPACE_ITEMS");
        return new WorkspaceItemsResponse(raw.DetachDocument(), raw.StatusCode, raw.ResponseBytes);
    }

    public async Task<SchemaResponse> GetSchemaAsync(Guid schemaUId, CancellationToken cancellationToken = default)
    {
        if (schemaUId == Guid.Empty)
            throw new BpmSoftTransportException(BpmSoftTransportError.EndpointNotAllowlisted, "ENDPOINT_NOT_ALLOWLISTED: schemaUId must be a non-empty GUID.");
        var body = Encoding.UTF8.GetBytes($"{{\"schemaUId\":\"{schemaUId:D}\"}}");
        using var candidate = BpmSoftReadRequestCandidate.Create(_origin, "SCHEMA_GET", body);
        using var raw = await SendCoreAsync(candidate, requiresSession: true, cancellationToken);
        EnsureSuccessfulEnvelope(raw.Document.RootElement, "SCHEMA_GET");
        return new SchemaResponse(raw.DetachDocument(), raw.StatusCode, raw.ResponseBytes);
    }

    public async Task<SelectQueryResponse> SelectQueryAsync(CanonicalSelectQueryBody body, CancellationToken cancellationToken = default)
    {
        using var candidate = BpmSoftReadRequestCandidate.Create(_origin, "SELECT_QUERY", body.DetachBody());
        using var raw = await SendCoreAsync(candidate, requiresSession: true, cancellationToken);
        EnsureSuccessfulEnvelope(raw.Document.RootElement, "SELECT_QUERY");
        return new SelectQueryResponse(raw.DetachDocument(), raw.StatusCode, raw.ResponseBytes);
    }

    internal async Task SendForContractTestAsync(BpmSoftReadRequestCandidate candidate, CancellationToken cancellationToken = default)
    {
        using var raw = await SendCoreAsync(candidate, requiresSession: candidate.RequestId != "AUTH_LOGIN", cancellationToken);
    }

    private async Task<RawResponse> SendCoreAsync(BpmSoftReadRequestCandidate candidate, bool requiresSession, CancellationToken cancellationToken)
    {
        EnsureNotDisposed();
        if (!ReadEndpointAllowlist.TryClassify(_origin, candidate, out _))
            throw new BpmSoftTransportException(BpmSoftTransportError.EndpointNotAllowlisted, "ENDPOINT_NOT_ALLOWLISTED: request method, path, body, or origin does not match ReadEndpointAllowlist/v1.");
        if (requiresSession && _session is null)
            throw new BpmSoftTransportException(BpmSoftTransportError.SessionRequired, "SESSION_REQUIRED: login with BPMCSRF is required before read requests.");

        using var request = new HttpRequestMessage(candidate.Method, candidate.RequestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new EphemeralJsonContent(candidate.DetachBody());
        if (requiresSession) _session!.Apply(request, _origin);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.RequestTimeout);
        HttpResponseMessage response;
        try
        {
            response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new BpmSoftTransportException(BpmSoftTransportError.RequestTimedOut, "REQUEST_TIMED_OUT: bounded BPMSoft read exceeded its timeout.");
        }
        catch (HttpRequestException)
        {
            throw new BpmSoftTransportException(BpmSoftTransportError.HttpRequestFailed, "HTTP_REQUEST_FAILED: BPMSoft read did not complete.");
        }

        using (response)
        {
            if (response.StatusCode is >= HttpStatusCode.MultipleChoices and <= (HttpStatusCode)399 ||
                response.RequestMessage?.RequestUri is { } responseUri && !_origin.IsExactOrigin(responseUri))
                throw new BpmSoftTransportException(BpmSoftTransportError.RedirectOrAlternateOrigin, "REDIRECT_OR_ALTERNATE_ORIGIN: redirects and alternate hosts are denied.");
            if (!response.IsSuccessStatusCode)
                throw new BpmSoftTransportException(BpmSoftTransportError.HttpRequestFailed, $"HTTP_REQUEST_FAILED: read endpoint returned status {(int)response.StatusCode}.");

            var bytes = await ReadBoundedAsync(response.Content, timeout.Token, cancellationToken);
            try
            {
                using var parseStream = new MemoryStream(bytes, writable: false);
                var document = JsonDocument.Parse(parseStream);
                var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : Array.Empty<string>();
                return new RawResponse(document, response.StatusCode, setCookies, bytes.Length);
            }
            catch (JsonException)
            {
                throw new BpmSoftTransportException(BpmSoftTransportError.ResponseInvalidJson, "RESPONSE_INVALID_JSON: BPMSoft response is not a valid JSON document.");
            }
            finally
            {
                Array.Clear(bytes);
            }
        }
    }

    private async Task<byte[]> ReadBoundedAsync(HttpContent content, CancellationToken boundedToken, CancellationToken callerToken)
    {
        Stream stream;
        try
        {
            stream = await content.ReadAsStreamAsync(boundedToken);
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            throw new BpmSoftTransportException(BpmSoftTransportError.RequestTimedOut, "REQUEST_TIMED_OUT: opening the bounded BPMSoft response stream exceeded its timeout.");
        }
        await using var ownedStream = stream;
        using var output = new MemoryStream(Math.Min(_options.MaxResponseBytes, 81920));
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(_options.MaxResponseBytes, 81920));
        try
        {
            while (true)
            {
                int read;
                try { read = await ownedStream.ReadAsync(buffer.AsMemory(0, buffer.Length), boundedToken); }
                catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
                {
                    throw new BpmSoftTransportException(BpmSoftTransportError.RequestTimedOut, "REQUEST_TIMED_OUT: bounded BPMSoft response read exceeded its timeout.");
                }
                if (read == 0) break;
                if (output.Length + read > _options.MaxResponseBytes)
                    throw new BpmSoftTransportException(BpmSoftTransportError.ResponseTooLarge, "RESPONSE_TOO_LARGE: BPMSoft response exceeded the configured byte bound.");
                await output.WriteAsync(buffer.AsMemory(0, read), boundedToken);
            }
            return output.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed) throw new BpmSoftTransportException(BpmSoftTransportError.SessionDisposed, "SESSION_DISPOSED: transport and session are unavailable.");
    }

    private static void EnsureSuccessfulEnvelope(JsonElement root, string requestId)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
            throw new BpmSoftTransportException(BpmSoftTransportError.ResponseEnvelopeInvalid, $"RESPONSE_ENVELOPE_INVALID: {requestId} did not return success=true.");
    }

    private static bool SameOrigin(Uri left, Uri right) => string.Equals(left.GetLeftPart(UriPartial.Authority), right.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session?.Dispose();
        _session = null;
        _client.Dispose();
    }

    private sealed class EphemeralJsonContent : HttpContent
    {
        private byte[]? _body;
        public EphemeralJsonContent(byte[] body)
        {
            _body = body;
            Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync((_body ?? throw new ObjectDisposedException(nameof(EphemeralJsonContent))).AsMemory()).AsTask();
        protected override bool TryComputeLength(out long length) { length = _body?.Length ?? 0; return true; }
        protected override void Dispose(bool disposing)
        {
            var body = Interlocked.Exchange(ref _body, null);
            if (body is not null) Array.Clear(body);
            base.Dispose(disposing);
        }
    }

    private sealed class RawResponse(JsonDocument document, HttpStatusCode statusCode, IReadOnlyList<string> setCookieHeaders, int responseBytes) : IDisposable
    {
        private JsonDocument? _document = document;
        public JsonDocument Document => _document ?? throw new ObjectDisposedException(nameof(RawResponse));
        public HttpStatusCode StatusCode { get; } = statusCode;
        public IReadOnlyList<string> SetCookieHeaders { get; } = setCookieHeaders;
        public int ResponseBytes { get; } = responseBytes;
        public JsonDocument DetachDocument() => Interlocked.Exchange(ref _document, null) ?? throw new ObjectDisposedException(nameof(RawResponse));
        public void Dispose() => Interlocked.Exchange(ref _document, null)?.Dispose();
    }
}
