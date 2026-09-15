namespace BpmSoftSync.Domain;

public enum BlockerCode
{
    EndpointNotAllowlisted,
    CatalogOrderOrPagingUnqualified,
    UnknownShapeUnqualified,
    LegacyDispositionInvalid,
    FullCatalogNotQualified,
    TargetStateChangedDuringQualification,
    WorkbookScaleDecisionRequired,
    EvidenceSchemaInvalid,
    EvidenceScanFailed,
    IndexSyncUnresolved,
    OfflineFixtureInvalid
}

// These categories are deliberately closed. A failed-shape diagnostic must never carry
// a server property value, display name, identifier, URL or response fragment.
public enum FailedShapePath
{
    SchemaRoot,
    SchemaName,
    SchemaUId,
    SchemaId,
    SchemaPackage,
    SchemaPackageId,
    SchemaPackageUId,
    SchemaPackageName,
    SchemaColumns,
    SchemaInheritedColumns,
    SchemaParent,
    SchemaParentName,
    SchemaParentUId,
    SchemaColumnMember,
    SchemaColumnName,
    SchemaColumnUId,
    SchemaColumnType,
    SchemaColumnRequirementType,
    SchemaColumnIndexed,
    SchemaIndexes,
    SchemaIndexMember,
    SchemaIndexUId,
    SchemaIndexName,
    SchemaIndexIsUnique,
    SchemaIndexColumns,
    SchemaIndexColumnMember,
    SchemaIndexColumnUId
}

public enum ExpectedShapeCategory { Object, OptionalObject, RequiredString, GuidString, Array, Integer, Boolean }
public enum ObservedJsonKind { Missing, Null, Object, Array, String, Number, Boolean, Other }
public enum ArrayCardinalityBucket { NotApplicable, Zero, One, TwoToTen, ElevenOrMore }
// This is a predicate result, not an identifier. It may be emitted only as the
// companion of a SchemaPackageId failure in the bounded diagnostic contract.
public enum GuidStringPredicateStatus { Passed, Failed }

public sealed record FailedShapeDiagnostic(
    FailedShapePath Path,
    ExpectedShapeCategory Expected,
    ObservedJsonKind Observed,
    ArrayCardinalityBucket ArrayCardinality,
    int? Ordinal,
    GuidStringPredicateStatus? CompanionGuidStringStatus = null);

/// <summary>
/// Closed terminal categories for the one-pass schema diagnostic. These labels
/// deliberately carry no server-provided text or identities.
/// </summary>
public enum SchemaDiagnosticTerminalOutcome
{
    UnknownShapeUnqualified,
    SchemaReadUnavailable,
    EndpointNotAllowlisted,
    NoSchemaCandidate,
    CompletedWithoutBlocker
}

/// <summary>
/// The complete durable contract for a bounded schema diagnostic. It is not a
/// qualification journal and intentionally has no catalog, output, URL, or
/// session fields.
/// </summary>
public sealed record SchemaDiagnosticTerminalEvidence(
    string Schema,
    string TargetAlias,
    string Route,
    SchemaDiagnosticTerminalOutcome Outcome,
    FailedShapeDiagnostic? FailedShape)
{
    public const string SchemaVersion = "SchemaDiagnosticTerminalEvidence/v1";
    public const string RouteVersion = "bounded-schema/v1";
}

public sealed record Blocker(
    BlockerCode Code,
    string Scope,
    string Reason,
    string Recovery,
    string NextPermittedAction,
    FailedShapeDiagnostic? FailedShape = null);

public enum InvocationSource { ManualTerminal, DirectCurrentChatRequest }

public sealed record Run(Guid RunId, string RunType, DateTimeOffset StartedAt, string TargetAlias, InvocationSource? InvocationSource, IReadOnlyList<string> GateOutcomes);

public sealed record AuditEvent(DateTimeOffset OccurredAt, string EventId, string Outcome);

public sealed record EvidenceEnvelope(string Schema, string StableKey, string PayloadDigest);

public sealed record EvidenceScopeContract(string CollectionId, string OrderKeyId, string QueryContractId, int PageSize, int MaxPages, int MaxRows, long MaxResponseBytes);

public sealed record QualificationEvidenceMetadata(
    Guid RunId,
    string TargetAlias,
    string Phase,
    string Outcome,
    int RetryCount,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyDictionary<string, string> Digests,
    string DurationBucket,
    string ScaleBucket,
    IReadOnlyList<string> ResponseSizeBuckets,
    IReadOnlyList<EvidenceScopeContract> ScopeContracts,
    IReadOnlyList<string> GateOutcomes,
    FailedShapeDiagnostic? FailedShape = null);

public sealed record QualificationEvidenceEnvelope(
    string Schema,
    string StableKey,
    string PayloadDigest,
    QualificationEvidenceMetadata Metadata)
{
    public const string SchemaVersion = "EvidenceEnvelope/v1";
}

public sealed record AuditMetadata(
    string ApplicationVersion,
    string TemplateVersion,
    string SkillsVersion,
    string BpmSoftVersion,
    IReadOnlyDictionary<string, DateTimeOffset> ExcelTableModificationTimes,
    string TargetAlias,
    string? PlanHash,
    IReadOnlyList<string> GateOutcomes);

public enum RequestBodyShape
{
    EmptyObject,
    SchemaUIdOnly,
    CanonicalSelectQuery,
    AuthenticationHandshake
}

public sealed record EndpointCandidate(string EndpointId, string Method, string Path, RequestBodyShape BodyShape, Uri? TargetUri = null);

public sealed record EndpointClassification(string EndpointId, string Method, string Path, RequestBodyShape BodyShape, string AllowlistVersion, Uri? TargetUri = null)
{
    public const string ExactAllowlistVersion = "ReadEndpointAllowlist/v1";
}

public sealed record SafeResult(bool IsSuccess, Blocker? Blocker, string Reason, string Scope, string Recovery, string NextPermittedAction)
{
    public static SafeResult SuccessForHumanReview(string scope) => new(true, null, "HUMAN_REVIEW_REQUIRED", scope, "Review the safe read-only evidence.", "A human may decide whether to start a separate manual read-only invocation.");

    public static SafeResult Blocked(Blocker blocker) => new(false, blocker, blocker.Reason, blocker.Scope, blocker.Recovery, blocker.NextPermittedAction);
}
