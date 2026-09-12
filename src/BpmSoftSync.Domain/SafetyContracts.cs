namespace BpmSoftSync.Domain;

public enum BlockerCode
{
    EndpointNotAllowlisted,
    CatalogOrderOrPagingUnqualified,
    UnknownShapeUnqualified,
    LegacyDispositionInvalid,
    FullCatalogNotQualified,
    IndexSyncUnresolved,
    OfflineFixtureInvalid
}

public sealed record Blocker(BlockerCode Code, string Scope, string Reason, string Recovery, string NextPermittedAction);

public sealed record Run(Guid RunId, string RunType, DateTimeOffset StartedAt, string TargetAlias, IReadOnlyList<string> GateOutcomes);

public sealed record AuditEvent(DateTimeOffset OccurredAt, string EventId, string Outcome);

public sealed record EvidenceEnvelope(string Schema, string StableKey, string PayloadDigest);

public enum RequestBodyShape
{
    EmptyObject,
    SchemaUIdOnly,
    CanonicalSelectQuery,
    AuthenticationHandshake
}

public sealed record EndpointCandidate(string EndpointId, string Method, string Path, RequestBodyShape BodyShape);

public sealed record EndpointClassification(string EndpointId, string Method, string Path, RequestBodyShape BodyShape, string AllowlistVersion)
{
    public const string ExactAllowlistVersion = "ReadEndpointAllowlist/v1";
}

public sealed record SafeResult(bool IsSuccess, Blocker? Blocker, string Reason, string Scope, string Recovery, string NextPermittedAction)
{
    public static SafeResult SuccessForHumanReview(string scope) => new(true, null, "HUMAN_REVIEW_REQUIRED", scope, "Review the offline fixture evidence.", "Request separate authorization before any live read-only run.");

    public static SafeResult Blocked(Blocker blocker) => new(false, blocker, blocker.Reason, blocker.Scope, blocker.Recovery, blocker.NextPermittedAction);
}
