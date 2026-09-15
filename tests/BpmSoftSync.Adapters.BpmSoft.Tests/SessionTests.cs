using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Adapters.BpmSoft;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class SessionTests
{
    private const string SecretCanary = "S01-password-canary";
    private const string LoginCanary = "S01-login-canary";

    public static async Task LoginSuccessCreatesCookieAndCsrfSessionAsync()
    {
        var handler = new ScriptedHandler(
            Response(HttpStatusCode.OK, "{\"Code\":0}", "BPMCSRF=csrf-canary; Path=/; Secure; HttpOnly"),
            Response(HttpStatusCode.OK, "{\"success\":true,\"items\":[]}"),
            Response(HttpStatusCode.OK, "{\"success\":true,\"schema\":{}}"),
            Response(HttpStatusCode.OK, "{\"success\":true,\"rows\":[]}"));
        using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan());

        var login = await transport.LoginAsync(credentials);
        Assert(!credentials.HasEphemeralState, "Login did not consume and clear credentials.");
        using var workspace = await transport.GetWorkspaceItemsAsync();
        using var schema = await transport.GetSchemaAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        using var selectBody = CanonicalSelectQueryBody.Parse("""
            {"rootSchemaName":"Lookup","rowCount":50,"rowsOffset":0,"isPageable":true,"allColumns":false,"useLocalization":true,"columns":{"items":{"Id":{"caption":"Id","orderDirection":1,"orderPosition":0,"isVisible":true,"expression":{"expressionType":0,"columnPath":"Id"}}}}}
            """);
        using var select = await transport.SelectQueryAsync(selectBody);

        Assert(login.Authenticated && transport.HasEphemeralSession, "Successful login did not establish an ephemeral session.");
        Assert(handler.Requests.Count == 4, "Unexpected request count.");
        Assert(handler.Requests[0].Path == "/ServiceModel/AuthService.svc/Login", "Login path mismatch.");
        Assert(handler.Requests.All(request => request.Method == "POST" && request.Accept == "application/json"), "Method or Accept header mismatch.");
        Assert(handler.Requests[0].Accept == "application/json", "Accept header missing.");
        Assert(handler.Requests[1].Path == "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems", "Workspace path mismatch.");
        Assert(handler.Requests[1].Body == "{}", "Workspace body is not canonical.");
        Assert(handler.Requests[2].Path == "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema" && handler.Requests[2].Body == "{\"schemaUId\":\"11111111-1111-1111-1111-111111111111\"}", "GetSchema request contract mismatch.");
        Assert(handler.Requests[3].Path == "/DataService/json/SyncReply/SelectQuery" && handler.Requests[3].Body.Contains("\"allColumns\":false", StringComparison.Ordinal), "SelectQuery request contract mismatch.");
        Assert(handler.Requests[1].Cookie.Contains("BPMCSRF=csrf-canary", StringComparison.Ordinal), "Session cookie was not sent.");
        Assert(handler.Requests[1].Csrf == "csrf-canary", "CSRF header was not sent.");
        Assert(workspace.Root.GetProperty("success").GetBoolean(), "Typed workspace response is unavailable.");
        Assert(schema.RequestId == "SCHEMA_GET" && select.RequestId == "SELECT_QUERY", "Typed response identity mismatch.");
    }

    public static async Task LoginFailureAndMissingCsrfFailClosedWithoutSecretLeakAsync()
    {
        foreach (var test in new[]
        {
            (Response: Response(HttpStatusCode.OK, $"{{\"Code\":1,\"Message\":\"{SecretCanary}\"}}"), Error: BpmSoftTransportError.LoginFailed),
            (Response: Response(HttpStatusCode.OK, "{\"Code\":0}"), Error: BpmSoftTransportError.CsrfCookieMissing)
        })
        {
            var handler = new ScriptedHandler(test.Response);
            using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
            using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan());
            var error = await CaptureAsync(() => transport.LoginAsync(credentials), test.Error);
            Assert(!credentials.HasEphemeralState, "Failed login did not clear credentials.");
            Assert(!transport.HasEphemeralSession, "Failed login retained session state.");
            Assert(!error.Message.Contains(SecretCanary, StringComparison.Ordinal) && !error.Message.Contains(LoginCanary, StringComparison.Ordinal), "Safe blocker leaked credentials or response body.");
        }
    }

    public static async Task ParseHttpAndEnvelopeFailuresAreSafelyTypedAsync()
    {
        foreach (var test in new[]
        {
            (Response: Response(HttpStatusCode.OK, "not-json"), Error: BpmSoftTransportError.ResponseInvalidJson),
            (Response: Response(HttpStatusCode.Unauthorized, SecretCanary), Error: BpmSoftTransportError.HttpRequestFailed)
        })
        {
            var handler = new ScriptedHandler(test.Response);
            using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
            using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan());
            var error = await CaptureAsync(() => transport.LoginAsync(credentials), test.Error);
            Assert(!error.Message.Contains(SecretCanary, StringComparison.Ordinal), "HTTP/parse blocker leaked response data.");
        }

        var envelopeHandler = new ScriptedHandler(
            Response(HttpStatusCode.OK, "{\"Code\":0}", "BPMCSRF=csrf-canary; Path=/"),
            Response(HttpStatusCode.OK, $"{{\"success\":false,\"message\":\"{SecretCanary}\"}}"));
        using var envelopeTransport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), envelopeHandler);
        using var envelopeCredentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan());
        await envelopeTransport.LoginAsync(envelopeCredentials);
        var envelopeError = await CaptureAsync(() => envelopeTransport.GetWorkspaceItemsAsync(), BpmSoftTransportError.ResponseEnvelopeInvalid);
        Assert(!envelopeError.Message.Contains(SecretCanary, StringComparison.Ordinal), "Envelope blocker leaked response data.");
    }

    public static async Task MissingSessionAndFailedReloginCannotReuseStateAsync()
    {
        var unsent = new ScriptedHandler();
        using (var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), unsent))
        {
            await CaptureAsync(() => transport.GetWorkspaceItemsAsync(), BpmSoftTransportError.SessionRequired);
            await CaptureAsync(() => transport.GetSchemaAsync(Guid.Empty), BpmSoftTransportError.EndpointNotAllowlisted);
            Assert(unsent.Requests.Count == 0, "Missing-session or invalid-schema request reached the handler.");
        }

        var relogin = new ScriptedHandler(
            Response(HttpStatusCode.OK, "{\"Code\":0}", "BPMCSRF=csrf-canary; Path=/"),
            Response(HttpStatusCode.OK, "{\"Code\":1}"));
        using var reloginTransport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), relogin);
        using var firstCredentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan());
        await reloginTransport.LoginAsync(firstCredentials);
        Assert(reloginTransport.HasEphemeralSession, "Initial session was not established.");
        using var secondCredentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan());
        await CaptureAsync(() => reloginTransport.LoginAsync(secondCredentials), BpmSoftTransportError.LoginFailed);
        Assert(!reloginTransport.HasEphemeralSession, "Failed re-login left the old cookie/CSRF session reusable.");
    }

    public static async Task BoundedReadTimeoutCancellationAndDisposalFailClosedAsync()
    {
        var oversized = new ScriptedHandler(Response(HttpStatusCode.OK, new string('x', 65)));
        using (var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), oversized, new BpmSoftHttpOptions(TimeSpan.FromSeconds(2), 64)))
        using (var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan()))
        {
            await CaptureAsync(() => transport.LoginAsync(credentials), BpmSoftTransportError.ResponseTooLarge);
        }

        var delayed = new ScriptedHandler { DelayUntilCancellation = true };
        using (var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), delayed, new BpmSoftHttpOptions(TimeSpan.FromMilliseconds(25), 1024)))
        using (var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan()))
        {
            await CaptureAsync(() => transport.LoginAsync(credentials), BpmSoftTransportError.RequestTimedOut);
        }

        var cancelled = new ScriptedHandler { DelayUntilCancellation = true };
        using (var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), cancelled, new BpmSoftHttpOptions(TimeSpan.FromSeconds(2), 1024)))
        using (var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan()))
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            await AssertCancelledAsync(() => transport.LoginAsync(credentials, cancellation.Token));
        }

        var sessionHandler = new ScriptedHandler(Response(HttpStatusCode.OK, "{\"Code\":0}", "BPMCSRF=csrf-canary; Path=/"));
        var disposable = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), sessionHandler);
        using (var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan()))
        {
            await disposable.LoginAsync(credentials);
        }
        disposable.Dispose();
        Assert(!disposable.HasEphemeralSession, "Session material survived transport disposal.");
        await CaptureAsync(() => disposable.GetWorkspaceItemsAsync(), BpmSoftTransportError.SessionDisposed);
    }

    public static async Task DelayedResponseStreamTimeoutIsTypedAsync()
    {
        var handler = new ScriptedHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new DelayedStreamContent()
        });
        using var transport = new BpmSoftReadTransport(
            BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"),
            handler,
            new BpmSoftHttpOptions(TimeSpan.FromMilliseconds(25), 1024));
        using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", LoginCanary, SecretCanary.AsSpan());

        await CaptureAsync(() => transport.LoginAsync(credentials), BpmSoftTransportError.RequestTimedOut);
        Assert(handler.Requests.Count == 1, "Delayed response-stream test did not pass the HTTP handler boundary exactly once.");
    }

    public static void MaximumResponseBoundRejectsOverflowBeforeSend()
    {
        var handler = new ScriptedHandler();
        try
        {
            using var transport = new BpmSoftReadTransport(
                BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"),
                handler,
                new BpmSoftHttpOptions(TimeSpan.FromSeconds(1), int.MaxValue));
        }
        catch (BpmSoftTransportException error) when (error.Code == BpmSoftTransportError.ResponseLimitInvalid)
        {
            Assert(handler.Requests.Count == 0, "Unsafe response bound reached the handler.");
            return;
        }
        throw new InvalidOperationException("int.MaxValue response bound was not rejected with a safe typed blocker.");
    }

    public static void TerminalPromptIsInteractiveOnlyAndDoesNotEchoOrSerializeSecrets()
    {
        var terminal = new FakeTerminal(
            lines: new[] { "https://bpm.example.test", LoginCanary },
            keys: SecretCanary.Select(character => new ConsoleKeyInfo(character, ConsoleKey.A, false, false, false))
                .Append(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)));
        using var credentials = new TerminalCredentialPrompt(terminal).Read();

        Assert(credentials.HasEphemeralState, "Interactive credentials were not held in memory.");
        Assert(!terminal.Output.Contains(SecretCanary, StringComparison.Ordinal) && !terminal.Output.Contains(LoginCanary, StringComparison.Ordinal), "Terminal echoed credentials.");
        Assert(!credentials.ToString().Contains(SecretCanary, StringComparison.Ordinal) && !credentials.ToString().Contains(LoginCanary, StringComparison.Ordinal), "Credential serialization leaked a secret.");
        var serialized = JsonSerializer.Serialize(credentials) + JsonSerializer.Serialize(credentials.TargetOrigin);
        Assert(!serialized.Contains(SecretCanary, StringComparison.Ordinal) && !serialized.Contains(LoginCanary, StringComparison.Ordinal) && !serialized.Contains("bpm.example.test", StringComparison.Ordinal), "JSON serialization exposed credential or target material.");
        Assert(typeof(TerminalCredentialPrompt).GetMethods().Where(method => method.Name == nameof(TerminalCredentialPrompt.Read)).All(method => method.GetParameters().Length == 0), "Credential prompt accepts arguments/configuration.");

        var redirected = new FakeTerminal(Array.Empty<string>(), Array.Empty<ConsoleKeyInfo>()) { IsInteractive = false };
        AssertThrows(() => new TerminalCredentialPrompt(redirected).Read(), "Non-interactive credential read was admitted.");
    }

    private static HttpResponseMessage Response(HttpStatusCode code, string body, string? cookie = null)
    {
        var response = new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (cookie is not null) response.Headers.TryAddWithoutValidation("Set-Cookie", cookie);
        return response;
    }

    private static async Task<BpmSoftTransportException> CaptureAsync(Func<Task> action, BpmSoftTransportError expected)
    {
        try { await action(); }
        catch (BpmSoftTransportException error) when (error.Code == expected) { return error; }
        throw new InvalidOperationException($"Expected blocker {expected} was not produced.");
    }

    private static async Task AssertCancelledAsync(Func<Task> action)
    {
        try { await action(); }
        catch (OperationCanceledException) { return; }
        throw new InvalidOperationException("Caller cancellation was converted or ignored.");
    }

    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (BpmSoftTransportException) { return; }
        throw new InvalidOperationException(message);
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class ScriptedHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<CapturedRequest> Requests { get; } = new();
        public bool DelayUntilCancellation { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (DelayUntilCancellation)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(
                request.Method.Method,
                request.RequestUri?.AbsolutePath ?? string.Empty,
                request.Headers.Accept.SingleOrDefault()?.MediaType ?? string.Empty,
                request.Headers.TryGetValues("Cookie", out var cookies) ? string.Join(";", cookies) : string.Empty,
                request.Headers.TryGetValues("BPMCSRF", out var csrf) ? csrf.Single() : string.Empty,
                body));
            return _responses.Dequeue();
        }
    }

    private sealed record CapturedRequest(string Method, string Path, string Accept, string Cookie, string Csrf, string Body);

    private sealed class DelayedStreamContent : HttpContent
    {
        public DelayedStreamContent() => Headers.ContentType = new MediaTypeHeaderValue("application/json");
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => Task.CompletedTask;
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override async Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Stream.Null;
        }
    }

    private sealed class FakeTerminal(IEnumerable<string> lines, IEnumerable<ConsoleKeyInfo> keys) : ICredentialTerminal
    {
        private readonly Queue<string> _lines = new(lines);
        private readonly Queue<ConsoleKeyInfo> _keys = new(keys);
        private readonly StringBuilder _output = new();
        public bool IsInteractive { get; init; } = true;
        public string Output => _output.ToString();
        public string? ReadLine() => _lines.Count == 0 ? null : _lines.Dequeue();
        public ConsoleKeyInfo ReadKey(bool intercept) => _keys.Dequeue();
        public void Write(string value) => _output.Append(value);
        public void WriteLine() => _output.AppendLine();
    }
}
