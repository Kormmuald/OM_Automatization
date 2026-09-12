using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

public static class ReadEndpointAllowlist
{
    private static readonly IReadOnlyDictionary<string, (string Method, string Path, RequestBodyShape Shape)> Entries =
        new Dictionary<string, (string, string, RequestBodyShape)>(StringComparer.Ordinal)
        {
            ["AUTH_LOGIN"] = ("POST", "/ServiceModel/AuthService.svc/Login", RequestBodyShape.AuthenticationHandshake),
            ["GET_PACKAGES"] = ("POST", "/ServiceModel/PackageService.svc/GetPackages", RequestBodyShape.EmptyObject),
            ["WORKSPACE_ITEMS"] = ("POST", "/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems", RequestBodyShape.EmptyObject),
            ["SCHEMA_GET"] = ("POST", "/ServiceModel/EntitySchemaDesignerService.svc/GetSchema", RequestBodyShape.SchemaUIdOnly),
            ["SELECT_QUERY"] = ("POST", "/DataService/json/SyncReply/SelectQuery", RequestBodyShape.CanonicalSelectQuery)
        };

    public static SafeResult TryClassify(EndpointCandidate candidate, out EndpointClassification? classification)
    {
        classification = null;
        if (!Entries.TryGetValue(candidate.EndpointId, out var expected) ||
            !string.Equals(candidate.Method, expected.Method, StringComparison.Ordinal) ||
            !string.Equals(candidate.Path, expected.Path, StringComparison.Ordinal) ||
            candidate.BodyShape != expected.Shape)
        {
            return SafeResult.Blocked(new Blocker(BlockerCode.EndpointNotAllowlisted, "endpoint", "ENDPOINT_NOT_ALLOWLISTED", "Use an exact ReadEndpointAllowlist/v1 method, path, and body shape.", "Stop the offline run."));
        }

        classification = new EndpointClassification(candidate.EndpointId, candidate.Method, candidate.Path, candidate.BodyShape, EndpointClassification.ExactAllowlistVersion);
        return SafeResult.SuccessForHumanReview("offline endpoint classification");
    }
}
