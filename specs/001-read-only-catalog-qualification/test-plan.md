# Test plan: read-only catalog qualification

## Test environment and evidence rules

All automated tests use fake HTTP transport or sanitized captures. Each test saves command output, fixture ID/digest and scan/schema result; network tests save method/path/endpoint ID only. No test contains a live credential, raw lookup value, cookie/CSRF or login body. Human sign-off is evidence only for a separately authorized live run and is not simulated as PASS.

## Increment tests

| Increment | User story / requirements | Test type and planned location | Expected evidence |
|---|---|---|---|
| Safe session + classifier | US1; FR-001, FR-002, FR-018; SC-001 | unit/contract: `BpmSoftSync.Adapters.BpmSoft.Tests/ReadEndpointAllowlistTests.cs` and `SessionTests.cs` | exact allowlist matrix; malformed path/method/body cases report `ENDPOINT_NOT_ALLOWLISTED`; capture records zero rejected sends and zero write calls |
| Ordered reader | US1; FR-003, FR-004; SC-002, SC-004 | unit + fixture integration: `CatalogReaderPagingTests.cs`, `fixtures/read-only/paging-*` | canonical page manifests; duplicate, overlap, gap, empty-middle, nonempty-after-terminal, loop and max-page all terminate `CATALOG_ORDER_OR_PAGING_UNQUALIFIED` |
| Double qualification | US1; FR-004, FR-010, FR-011; SC-002 | domain/process: `CatalogQualificationTests.cs`, `TargetFingerprintTests.cs`, `target-state-change.json` | two matching passes reconcile; golden hash vectors; a changed target yields `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, exactly two passes and no auto retry |
| Identity + inventory | US1; FR-005…FR-009; SC-003 | domain/adapter fixture: `WorkspaceInventoryTests.cs`, `UnknownShapeTests.cs` | same name/different package layer remains distinct; 100% source items statuses; lossless structural envelope/digest; missing envelope fails |
| Run/evidence boundary | US2; FR-012…FR-014; SC-005, SC-006 | filesystem/security: `RunStoreTests.cs`, `EvidenceEnvelopeTests.cs`, `SecretValueScannerTests.cs` | distinct append-only roots; overwrite rejected; schema and secret/raw-value canaries 100% rejected; PASS seal impossible after failed scan |
| CLI + skills boundary | US3; FR-015, FR-016, FR-018, FR-019; SC-007 | CLI process/architecture: `CliContractTests.cs`, `ArchitectureTests.cs`, skill dry-run fixture | safe reason/scope/recovery/next action; no forbidden project references; no credentials in args; clean fixture reaches human qualification decision point |
| Legacy disposition | FR-017 | characterization review test/manifest: `tests/fixtures/read-only/legacy-disposition.json` | each adopted behaviour marked `reuse-semantics|rewrite|drop|defer`; no source-code copy claim |

## Final solution tests

| Final check | Scope | Test type / artifact | Expected evidence |
|---|---|---|---|
| Offline end-to-end clean qualification | FR-001…FR-019, SC-001…SC-007 except manual live invocation | fake transport process test | exactly two reconciled passes, complete inventory, deterministic fingerprint, safe append-only evidence, `HUMAN_REVIEW_REQUIRED`, zero write calls |
| Adversarial qualification suite | paging, mutation, unknown shape, endpoint and evidence failure modes | fixture matrix and process test | each fixture has named blocker and no false PASS/hang; target mutation is terminal `TARGET_STATE_CHANGED_DURING_QUALIFICATION` with no third pass |
| Determinism/metamorphic regression | canonical ordering, fingerprints, evidence schema | repeat test with reordered JSON properties and runs | semantically identical inputs yield identical canonical hashes; changed identity/page/status/version changes digest; roots remain distinct |
| Secret/value leakage regression | audit/evidence/CLI stderr/stdout | canary scan over produced run trees | all canaries rejected; scanner output contains only category/location/digest; no PASS artifact |
| Граница ручного live invocation | FR-010, SC-002; offline/automatic paths и ручной `catalog qualify` | fixture/process test: `ManualLiveInvocationTests.cs` | offline and automatic paths have zero terminal credential prompts and zero HTTP sends; an interactive manual invocation can reach the terminal credential prompt without `AuthorizationReference`; no write calls |
| Условная ручная verification на реальном стенде | real full catalog после успешных automated offline checks | operator manually runs the procedure from `quickstart.md`, or an agent acts on a direct current user request | safe evidence and human review decision; no automatic run and no write calls |

## Requirement coverage

| Requirement | Coverage |
|---|---|
| FR-001–002 | Safe session + classifier; offline E2E |
| FR-003–004 | Ordered reader; double qualification; adversarial suite |
| FR-005–009 | Identity + inventory; adversarial suite |
| FR-010 | Double qualification; target-change process test; authorization-gate negative scenario; metamorphic regression |
| FR-011 | Double qualification; target-change process test; metamorphic regression |
| FR-012–014 | Run/evidence boundary; secret/value leakage regression |
| FR-015–016 | CLI + skills boundary; offline E2E |
| FR-017 | Legacy disposition manifest/review |
| FR-018–019 | CLI + skills boundary; architecture test |
| SC-001–SC-007 | Rows above; SC-007 uses clean offline machine/fixture path. Real live proof is conditional on successful offline checks and a manual operator invocation or direct current user request. |

## Exit criteria before `/SpecKit Tasks`

- Every listed planned test has a fixture, source location and expected safe evidence.
- `TARGET_STATE_CHANGED_DURING_QUALIFICATION` remains terminal and explicitly testable; no task may introduce automatic retry.
- Test setup contains no real target or credential.
- `FULL_CATALOG_NOT_QUALIFIED`, `INDEX_SYNC_UNRESOLVED` and absent Write/Manage rights are represented as gates, not failing test data to be bypassed. `FULL_CATALOG_NOT_QUALIFIED` does not block a manual live invocation after offline checks.
- Offline and automatic paths have a separate negative check: credential prompt and HTTP send are zero. A manually started interactive `catalog qualify` does not require `AuthorizationReference` and may request credentials only in its terminal prompt.
