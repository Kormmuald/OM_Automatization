# Phase 0 Research: read-only catalog qualification

## Контекст и метод

Источники: immutable common vision `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`, active `spec.md`, constitution и статическое чтение legacy prototypes. Этапные source drafts не использовались. Это не live research: endpoint paths ниже выводятся из статических prototypes и должны подтверждаться fixtures/capture tests до использования. Никакой пункт не разрешает автоматический live run.

## Decision 1 — Exact `ReadEndpointAllowlist/v1`

**Decision**: classifier разрешает только следующие canonical `POST` requests, после validation normalized relative path (no query, fragment, path traversal or alternate host):

| ID | Method + exact path | Назначение | Разрешённый request shape |
|---|---|---|---|
| `AUTH_LOGIN` | `POST /ServiceModel/AuthService.svc/Login` | interactive session handshake | username, in-memory password, local time-zone offset |
| `WORKSPACE_ITEMS` | `POST /ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems` | workspace inventory | empty JSON object |
| `SCHEMA_GET` | `POST /ServiceModel/EntitySchemaDesignerService.svc/GetSchema` | schema detail | exactly one validated `schemaUId` |
| `SELECT_QUERY` | `POST /DataService/json/SyncReply/SelectQuery` | ordered collection pages | canonical reader-generated select query only |

`AUTH_LOGIN` is separately classified as session establishment, not catalog read; it remains in the narrow transport allowlist because the authenticated read scope cannot start otherwise. Every other method, canonical path, query, redirect target, request body shape or request generated outside adapter is `ENDPOINT_NOT_ALLOWLISTED` before network I/O. Redirects are disabled; only an explicitly supplied local loopback HTTP origin is accepted by the initial implementation.

**Verifiable criteria**: parameterized contract tests enumerate every allowed pair/shape; capture handler records zero sends for reject cases; allowed fixtures prove all four IDs; static/IL or request-factory tests demonstrate no secondary HTTP entry point.

**Rationale**: legacy prototypes use these four paths while declaring a read-only probe. An exact small set is reviewable and supports SC-001.

**Alternatives considered**: verb/name heuristics (rejected: endpoint names are not an authorization model); broad `/ServiceModel/*` prefix (rejected: cannot prove absence of write/manage calls); allowlisting a future version endpoint (rejected: not evidenced and would expand scope).

## Decision 2 — `TargetFingerprint/v1`

**Decision**: fingerprint value is lowercase SHA-256 over UTF-8 canonical JSON with schema tag `TargetFingerprint/v1`. Canonicalization uses NFC strings, lowercase dashed GUIDs, invariant numbers, lexicographically ordered object keys and list order only when defined by the domain contract. The preimage contains no credentials or raw lookup values:

```text
{ schema, allowlistVersion, scopeDescriptorHash,
  observedTargetVersionEvidence[],
  workspace: sorted[(WorkspaceItemUId, PackageLayerId, itemType, supportStatus)],
  structuredSchemas: sorted[(SchemaUId, PackageLayerId, canonicalMetadataHash)],
  collections: sorted[(collectionId, orderKeyId, count, orderedIdentityDigest, pageManifestDigest)],
  unsupported: sorted[(stableIdentity, typeTag, losslessShapeDigest, supportStatus)] }
```

`observedTargetVersionEvidence` may contain only version/build metadata actually present in an allowlisted response or a versioned local assembly file independently verified for the selected local target. It must contain source, observation timestamp and SHA-256 of its canonical safe representation. Missing target version evidence is recorded as `TARGET_VERSION_METADATA_UNAVAILABLE`; it is never invented. `scopeDescriptorHash` binds declared scope and reader rules without storing sensitive config.

**Verifiable criteria**: golden-vector tests exercise key ordering, GUID normalization and Unicode; metamorphic tests vary response property ordering without changing a hash; a single identity/page/support-status/version change changes it; canary scans prove no raw `Name`/`Description`/lookup value and no secret input influence the stored preimage/evidence. Collision resistance and staleness remain documented assumptions, not proof of production equivalence.

**Rationale**: it binds target-observed inventory, package layer and deterministic page results without treating display names or bounded evidence as full-catalog proof.

**Alternatives considered**: URL plus timestamp (rejected: not state proof); raw response hash (rejected: secret/value leakage); one aggregate count (rejected: misses permutation/identity changes).

## Decision 3 — deterministic reader and double qualification

**Decision**: each registered collection declares a stable `orderKeyId`, query builder and `MaxPages`. Reader emits a page manifest `(pageOrdinal, cursor/offset token hash, count, firstIdentity, lastIdentity, pageIdentityDigest, response-size bucket)` and checks: nonempty intermediate termination, duplicate identity, overlap, cursor/offset progress, gap according to declared contract, max pages and explicit terminal condition. It returns `CATALOG_ORDER_OR_PAGING_UNQUALIFIED` on any violation.

Qualification seals a scope at Pass A, runs Pass B once, then compares scope/fingerprint components, ordered identity digests, counts, page manifests and unsupported set. A difference attributable to target state emits `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; a paging contract violation emits `CATALOG_ORDER_OR_PAGING_UNQUALIFIED`. Both are terminal run outcomes. Automatic new double pass, retry loop or silent downgrade is prohibited; a new run needs a new manual invocation or a new direct current user request.

**Verifiable criteria**: adversarial fixtures cover duplicate, overlap, skip, empty-middle, nonempty-after-terminal, loop, max-page and target-change cases; process test proves no third pass after either terminal blocker.

**Alternatives considered**: retry until equal (rejected: masks target change and violates clarification); unordered set comparison (rejected: misses paging/order defect); treating target change as PASS-with-warning (rejected: fails closed).

## Decision 4 — lossless inventory and package-layer identity

**Decision**: inventory starts from every returned workspace item. Each has `Structured`, `InventoryOnly`, `Unreadable` or `Unsupported`, a typed package-layer identity and a safe reason. For unknown property/type/shape, `LosslessShapeEnvelope` preserves the canonical structural tree (property names, JSON kinds, array/object structure, safe scalar class/length/hash) and an original-response SHA-256 when safe; it never replaces data by defaults or joins by display name. Raw lookup scalar strings are neither written nor exposed.

**Verifiable criteria**: exhaustive inventory count equals source count; same display names in different layers remain separate; unknown-shape fixtures produce envelopes or a named blocker; no envelope has raw-value canary.

**Alternatives considered**: dropping unknown fields (rejected: lossy); arbitrary JSON persistence (rejected: may store secrets/values); `Name`/`Code` fallback join (rejected: identity ambiguity).

## Decision 5 — evidence redaction and run storage

**Decision**: `EvidenceEnvelope/v1` is allow-by-type/schema, not redact-after-write. Permitted values are typed run IDs, timestamps, enum status/blocker IDs, endpoint IDs/methods, relative evidence paths, validated version strings, GUID identities, counts/durations/size buckets and SHA-256 digests. Envelope subtypes have explicit fields; unrecognized fields fail schema validation. Scanner runs before every durable write and before success seal; case-insensitive forbidden key/value detectors include password, secret, token, cookie, CSRF, authorization, `Set-Cookie`, `UserPassword`, login response/body and raw lookup value canaries. Scanner reports only location/category/digest, never matched value.

Run directory creation uses date hierarchy plus fresh `RunId`; pre-existing root is a hard failure. Journal/audit/evidence writes are append-only and named with timestamp + stable kind. Failure evidence is subject to the same scan.

**Verifiable criteria**: schema negative tests and injected key/value canaries reject 100%; two same-time runs create distinct roots; overwrite attempt fails; PASS seal impossible after scanner/schema failure; artifacts enumerate hashes and relative paths only.

**Alternatives considered**: generic JSON plus regex cleanup (rejected: bypassable and loses auditability); log-first/redact-later (rejected: secret already durable); timestamp-only roots (rejected: collision risk).

## Decision 6 — CLI/domain/adapter boundary

**Decision**: `Domain` owns identity, canonicalization, reader state validation, qualification, blocker and evidence models. `Application` owns use cases and ports. BPMSoft/FileSystem adapters implement ports; CLI only prompts, invokes an application command and maps safe result to exit code. Skills only orchestrate documented CLI commands and explain declared blocker recovery.

**Verifiable criteria**: domain references no HTTP/Excel/browser/Git assemblies; architecture test prevents forbidden project references; a fake adapter runs every qualification fixture; skill contract test verifies no business-rule implementation or secret argument.

**Alternatives considered**: one console program modelled on the prototype (rejected: coupled, untestable and duplicates rules); moving checks into skills (rejected: violates constitution and creates divergent logic).

## Preserved human gates and unresolved risks

- `FULL_CATALOG_NOT_QUALIFIED` remains until a live double pass yields reviewable evidence. Development/offline tests do not close it, but it is not a preflight block for a manual live invocation.
- No persisted authorization reference exists for a live read-only run. An operator starts `catalog qualify` manually, or an agent acts only on a direct current user request; neither path permits automatic execution.
- `INDEX_SYNC_UNRESOLVED` remains; index data may be inventory/read-only evidence only. No index plan/load/apply exists.
- Write/Manage rights are neither requested nor used. `WRITE_ALLOWLIST_UNAPPROVED` and `WRITE_SEMANTICS_UNPROVEN` remain out of scope.
