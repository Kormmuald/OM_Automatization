using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class ReadEndpointAllowlistTests
{
    public static async Task ExactMatrixRejectsBeforeSendAsync()
    {
        var capture = new RequestCapture();
        var transport = new CapturedReadOnlyTransport(capture);
        foreach (var candidate in new[]
        {
            new EndpointCandidate("AUTH_LOGIN", "POST", "/ServiceModel/AuthService.svc/Login", RequestBodyShape.AuthenticationHandshake),
            new EndpointCandidate("GET_PACKAGES", "POST", "/ServiceModel/PackageService.svc/GetPackages", RequestBodyShape.EmptyObject),
            new EndpointCandidate("WORKSPACE_ITEMS", "POST", "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems", RequestBodyShape.EmptyObject),
            new EndpointCandidate("SCHEMA_GET", "POST", "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema", RequestBodyShape.SchemaUIdOnly),
            new EndpointCandidate("SELECT_QUERY", "POST", "/DataService/json/SyncReply/SelectQuery", RequestBodyShape.CanonicalSelectQuery)
        })
        {
            var classification = ReadEndpointAllowlist.TryClassify(candidate, out var endpoint);
            Assert(classification.IsSuccess && endpoint is not null, "Allowed endpoint was rejected.");
            await transport.SendAsync(endpoint!);
        }

        foreach (var rejectedCandidate in new[]
        {
            new EndpointCandidate("GET_PACKAGES", "GET", "/ServiceModel/PackageService.svc/GetPackages", RequestBodyShape.EmptyObject),
            new EndpointCandidate("GET_PACKAGES", "POST", "/ServiceModel/PackageService.svc/GetPackages/extra", RequestBodyShape.EmptyObject),
            new EndpointCandidate("GET_PACKAGES", "POST", "/ServiceModel/PackageService.svc/GetPackages", RequestBodyShape.SchemaUIdOnly)
        })
        {
            var rejected = ReadEndpointAllowlist.TryClassify(rejectedCandidate, out _);
            Assert(!rejected.IsSuccess && rejected.Reason == "ENDPOINT_NOT_ALLOWLISTED", "Malformed method, path, or body shape was not rejected.");
        }
        Assert(capture.Requests.Count == 5 && capture.RejectedSendCount == 0 && capture.WriteCallCount == 0, "Rejected endpoint reached transport or a write was captured.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
