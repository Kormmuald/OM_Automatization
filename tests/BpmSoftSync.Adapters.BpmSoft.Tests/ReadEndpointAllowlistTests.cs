using System.Net;
using System.Text;
using BpmSoftSync.Adapters.BpmSoft;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class ReadEndpointAllowlistTests
{
    public static async Task ExactMatrixRejectsBeforeSendAsync()
    {
        var handler = new CaptureHandler();
        using var transport = new BpmSoftReadTransport(
            BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"),
            handler,
            new BpmSoftHttpOptions(TimeSpan.FromSeconds(2), 16 * 1024));

        foreach (var candidate in new[]
        {
            BpmSoftReadRequestCandidate.ForContractTest("GET_PACKAGES", HttpMethod.Post, new Uri("https://bpm.example.test/ServiceModel/PackageService.svc/GetPackages"), "{}"),
            BpmSoftReadRequestCandidate.ForContractTest("WORKSPACE_ITEMS", HttpMethod.Get, new Uri("https://bpm.example.test/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems"), "{}"),
            BpmSoftReadRequestCandidate.ForContractTest("WORKSPACE_ITEMS", HttpMethod.Post, new Uri("https://bpm.example.test/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems?x=1"), "{}"),
            BpmSoftReadRequestCandidate.ForContractTest("WORKSPACE_ITEMS", HttpMethod.Post, new Uri("https://bpm.example.test/ServiceModel/WorkspaceExplorerService.svc/../GetWorkspaceItems"), "{}"),
            BpmSoftReadRequestCandidate.ForContractTest("WORKSPACE_ITEMS", HttpMethod.Post, new Uri("https://alternate.example.test/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems"), "{}"),
            BpmSoftReadRequestCandidate.ForContractTest("WORKSPACE_ITEMS", HttpMethod.Post, new Uri("https://bpm.example.test/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems"), "{\"unexpected\":true}"),
            BpmSoftReadRequestCandidate.ForContractTest("SCHEMA_GET", HttpMethod.Post, new Uri("https://bpm.example.test/ServiceModel/EntitySchemaDesignerService.svc/GetSchema"), "{\"schemaUId\":\"not-a-guid\"}"),
            BpmSoftReadRequestCandidate.ForContractTest("SELECT_QUERY", HttpMethod.Post, new Uri("https://bpm.example.test/DataService/json/SyncReply/SelectQuery"), "{}")
        })
        {
            await AssertCodeAsync(
                () => transport.SendForContractTestAsync(candidate),
                BpmSoftTransportError.EndpointNotAllowlisted);
        }

        Assert(handler.SendCount == 0, "Rejected request reached HttpMessageHandler.");
        Assert(ReadEndpointAllowlist.EndpointIds.SequenceEqual(new[] { "AUTH_LOGIN", "WORKSPACE_ITEMS", "SCHEMA_GET", "SELECT_QUERY" }, StringComparer.Ordinal), "Allowlist is not the exact four-request matrix.");
    }

    public static void TargetPolicyAllowsExplicitHttpsAndLoopbackOnly()
    {
        foreach (var allowed in new[] { "http://localhost:8002", "https://localhost:8002/", "https://bpm.example.test" })
        {
            var origin = BpmSoftTargetOrigin.ParseInteractive(allowed);
            Assert(origin.Uri.AbsolutePath == "/" && string.IsNullOrEmpty(origin.Uri.Query), "Origin was not normalized.");
        }

        foreach (var denied in new[]
        {
            "http://bpm.example.test", "ftp://bpm.example.test", "https://user:password@bpm.example.test",
            "https://bpm.example.test/path", "https://bpm.example.test/?q=1", "https://bpm.example.test/#fragment"
        })
        {
            AssertThrows(() => BpmSoftTargetOrigin.ParseInteractive(denied), "Unsafe target origin was admitted.");
        }
    }

    public static async Task RedirectAndAlternateResponseOriginAreRejectedAsync()
    {
        foreach (var response in new[]
        {
            new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("https://alternate.example.test/") } },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://alternate.example.test/ServiceModel/AuthService.svc/Login"),
                Content = new StringContent("{\"Code\":0}")
            }
        })
        {
            var handler = new CaptureHandler(response);
            using var transport = new BpmSoftReadTransport(BpmSoftTargetOrigin.ParseInteractive("https://bpm.example.test"), handler);
            using var credentials = InteractiveCredentials.CreateForTesting("https://bpm.example.test", "operator", "secret".AsSpan());
            await AssertCodeAsync(() => transport.LoginAsync(credentials), BpmSoftTransportError.RedirectOrAlternateOrigin);
            Assert(handler.SendCount == 1, "Redirect test did not issue exactly the login request.");
        }
    }

    private static async Task AssertCodeAsync(Func<Task> action, BpmSoftTransportError code)
    {
        try { await action(); }
        catch (BpmSoftTransportException error) when (error.Code == code) { return; }
        throw new InvalidOperationException($"Expected blocker {code} was not produced.");
    }

    private static void AssertThrows(Action action, string message)
    {
        try { action(); }
        catch (BpmSoftTransportException) { return; }
        throw new InvalidOperationException(message);
    }

    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class CaptureHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(_responses.Count == 0
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") }
                : _responses.Dequeue());
        }
    }
}
