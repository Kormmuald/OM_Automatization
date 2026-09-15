# Research decisions: Feature 001 full catalog MVP

Immutable inputs are the common vision, monolithic MVP and workbook contract. This is planning only, not live research.

## Decision 1 — `ReadEndpointAllowlist/v1`

Only these canonical POST requests are valid after normalized relative-path/origin/body validation: `AUTH_LOGIN` `/ServiceModel/AuthService.svc/Login`; `WORKSPACE_ITEMS` `/ServiceModel/WorkspaceExplorerService.svc/GetWorkspaceItems`; `SCHEMA_GET` `/ServiceModel/EntitySchemaDesignerService.svc/GetSchema` with exactly one validated `schemaUId`; `SELECT_QUERY` `/DataService/json/SyncReply/SelectQuery` with reader-generated payload. Redirects, query/fragment/traversal/alternate host, heuristic paths and `GET_PACKAGES` fail before send.

## Decision 2 — `TargetFingerprint/v1`

Lowercase SHA-256 of UTF-8 canonical JSON binds schema tag, allowlist version, scope hash, observed safe target version evidence, sorted workspace/package-layer identity/status, structured schema metadata hashes, ordered-collection manifests and unsupported-shape digests. It never contains raw lookup values, URL credentials or secrets. NFC text, lower dashed GUIDs, invariant numbers, sorted object keys and domain-defined list order are testable vectors.

## Decision 3 — full ordered reader and exact two passes

Every registry/lookup collection declares order key, canonical query and limits. Its manifest has ordinal, progress-token hash, count, first/last identity, identity digest and response-size bucket. It detects duplicate, overlap, gap, loop, empty-middle, nonempty-after-terminal and limits. Pass A seals full scope; B freshly repeats it without cache reuse. Equality covers all scope/manifests/content hashes/statuses/fingerprint components. Mismatch is `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, `RetryCount=0`; no Pass C/retry.

## Decision 4 — full but lossless data model

All workspace items retain typed identity, package layer and one support status. Supported EntitySchema retains own/inherited columns, references and indexes from `indexes[].columns[].columnUId`. Lookup scope is registry plus each discovered lookup schema, with normalized values preserving `Null|EmptyString|Value`, value kind and reference ID. Unknown shapes are structural envelopes (names, JSON kinds, structure, scalar class/length/hash) or named blocker, never default/drop.

## Decision 5 — snapshot and Excel boundary

`QualifiedCatalogSnapshot/v1` is created only after equality and carries B values in memory to one Excel adapter. It has no HTTP/console/OOXML types. The adapter projects the required Model sheets (`Readme`, `Manifest`, `WorkspaceInventory`, `Schemas`, `Columns`, `Indexes`, `ValidationLists`, `PullConflicts`) and Lookup sheets (`Readme`, `Manifest`, `LookupRegistry`, `LookupValues`, `ValidationLists`, `PullConflicts`), then validates pair binding, 1:1 source rows, headers/order, OOXML closure and forbidden external/VBA/formula parts before atomic pair publication.

## Decision 6 — output is not evidence

Only local `output/*.xlsx` may contain lookup values. Journal, audit, evidence, CLI and diagnostics use allow-by-schema safe metadata: IDs, aliases, counts, hashes, status, blocker, duration/size buckets, versions and relative paths. Scanner/validator run before every durable write and seal. Each fresh `RunId` root is append-only and collision is terminal.

## Decision 7 — staged proof and live admission

S01–S06 use fixtures/fake `HttpMessageHandler`; S06 proves the production root and emits fresh safe offline report. S07 is an isolated opt-in `--live --manual` harness only after accepted S06 and the user's current «стенд запущен». Credentials are terminal-only. One real run either produces reviewed safe output or one blocker; no automatic retry. S08 documents facts and leaves final acceptance to the human.

## Preserved gates

`FULL_CATALOG_NOT_QUALIFIED` remains until successful S07 plus human decision. `INDEX_SYNC_UNRESOLVED` remains. No write/manage/compare/apply/browser/Git scope is inferred from any decision.
