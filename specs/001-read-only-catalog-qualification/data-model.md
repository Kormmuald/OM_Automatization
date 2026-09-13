# Data model: read-only catalog qualification

All IDs below are distinct value types. Display text is diagnostic only and never a reconciliation key.

| Entity | Required fields | Rules / relations |
|---|---|---|
| `Run` | `RunId`, `RunType`, timestamps, `TargetAlias`, `InvocationSource`, `GateOutcomes[]` | Creates one immutable root. `InvocationSource` is only `ManualTerminal` or `DirectCurrentChatRequest`; it stores no chat content or credentials. No `AuthorizationReference` is required for live read-only qualification. |
| `Session` | local target URI, in-memory cookie container, CSRF header | Ephemeral; not serializable and absent from audit/evidence. |
| `EndpointClassification` | `EndpointId`, method, exact path, request-shape validator | Only `ReadEndpointAllowlist/v1`; reject before send. |
| `PackageLayerIdentity` | `PackageId`, `PackageUId?`, `PackageName?`, `LayerKind?` | Part of all schema/workspace identities; no name-only merge. |
| `WorkspaceInventoryItem` | stable workspace identity, `PackageLayerIdentity`, item type, `SupportStatus`, safe reason, `LosslessShapeEnvelope?` | Exactly one status: `Structured`, `InventoryOnly`, `Unreadable`, `Unsupported`. |
| `LosslessShapeEnvelope` | stable identity, type tag, canonical structural tree, scalar class/length/hash, source shape digest | No raw lookup scalar values. Missing envelope for unknown shape is a blocker. |
| `CollectionDefinition` | ID, order key, query builder ID, page policy, `MaxPages` | Registered/domain-defined; no arbitrary CLI query. |
| `CatalogPass` | pass ordinal, collection manifests, inventory digest, target fingerprint, telemetry | Terminal only after explicit page completion. |
| `PageManifest` | ordinal, progress-token hash, count, first/last identity, identity digest, size bucket | Validated for progress, duplicates, overlap, skips and terminal semantics. |
| `CatalogQualification` | Pass A, Pass B, reconciliation result, `Blocker?` | Target mutation produces `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; it never invokes pass C. |
| `TargetFingerprint` | schema/version, digest, component digests, observed version evidence | Exact canonical preimage in `research.md`; version absence is explicit, never guessed. |
| `AuditEvent` / `EvidenceEnvelope` | timestamp/version, stable key, safe payload, payload digest | Append-only, allow-by-schema and scanned before durable write. |
| `Blocker` | code, scope, safe reason, recovery action, next permitted action | Includes `ENDPOINT_NOT_ALLOWLISTED`, `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`, `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, `FULL_CATALOG_NOT_QUALIFIED`, `INDEX_SYNC_UNRESOLVED`. |

## State transitions

```text
Created -> OfflineValidated
Created -> ManualInvocation | DirectCurrentChatRequest -> SessionEstablished -> PassA -> PassB
PassA/PassB -> PagingBlocked | InventoryBlocked | TargetChanged | QualifiedForHumanReview
TargetChanged -> SealedBlocked (no automatic retry)
QualifiedForHumanReview -> SealedEvidence (does not grant later Apply)
```

`QualifiedForHumanReview` is evidence availability, not acceptance and not a permission grant.
