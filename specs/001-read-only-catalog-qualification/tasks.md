---
description: "Исполнимый, dependency-ordered task list для Feature 001"
---

# Tasks: безопасное чтение и qualification каталога BPMSoft

**Input**: design artifacts in `specs/001-read-only-catalog-qualification/`; immutable common vision: `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md` (не изменяется).

**Prerequisites**: approved `implementation-slices.md` (S01–S03), `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/cli-contract.md`, `quickstart.md`, `test-plan.md`, current Critique `critiques/critique-20260909-100855.md`.

**Scope guard**: каждая задача ниже принадлежит approved S01, S02, S03 или corrective S04 active Feature 001. Разрешены только sanitized fixtures и fake transport/fake `HttpMessageHandler` до T045. `FULL_CATALOG_NOT_QUALIFIED`, `INDEX_SYNC_UNRESOLVED`, отсутствие Write/Manage и запрет Excel/compare/Apply/browser/Git сохраняются. `TARGET_STATE_CHANGED_DURING_QUALIFICATION` terminal: Pass C, retry loop и automatic new double pass запрещены.

## Формат и правила исполнения

Каждая checklist-задача явно фиксирует `Slice`, requirements, путь/компонент, зависимость, test/safe evidence и stop condition. Автоматические тесты пишутся до реализации и должны первоначально падать. Не запускать live BPMSoft, credential prompt, browser, Excel, Git или write action при выполнении любой offline-задачи.

## Phase 1: Setup — shared offline foundation (S01)

**Purpose**: создать строго offline solution skeleton, test infrastructure и безопасные fixture rules.

- [X] T001 Create `BpmSoftSync.sln` and project skeletons in `src/BpmSoftSync.Cli/`, `src/BpmSoftSync.Domain/`, `src/BpmSoftSync.Application/`, `src/BpmSoftSync.Adapters.BpmSoft/`, `src/BpmSoftSync.Adapters.FileSystem/`, and `tests/*`; Slice: S01; Reqs: FR-019; Depends: none; Evidence: `dotnet build BpmSoftSync.sln` succeeds with `net10.0`, nullable and warnings-as-errors; Stop: any runtime dependency on `preparation/*`, Excel, browser or Git types.
- [X] T002 [P] Establish `Directory.Build.props`, `Directory.Packages.props`, and `tests/README.md` with BCL-only/fake-transport test rules; Slice: S01; Reqs: FR-001, FR-014, FR-019; Depends: T001; Evidence: build settings and test guide prohibit real targets, credentials, raw lookup values and external BPMSoft client libraries; Stop: an added package or setting permits a live target or persisted secret.
- [X] T003 [P] Create sanitized fixture catalog and canary policy in `tests/fixtures/read-only/README.md` and `tests/fixtures/read-only/fixture-manifest.json`; Slice: S01; Reqs: FR-001, FR-002, FR-014; Depends: T001; Evidence: every fixture has ID/digest/classification and no credential, cookie, CSRF, login body or raw lookup value; Stop: a fixture contains an unsanitized value or real endpoint authorization.

---

## Phase 2: Foundational — typed ports and shared safety contracts (S01)

**Purpose**: blocking contracts that all user stories require. No user-story implementation starts before this phase is complete.

- [X] T004 Create typed identities, `Blocker`, `Run`, `AuditEvent`, `EvidenceEnvelope`, `EndpointClassification`, and safe result models in `src/BpmSoftSync.Domain/`; Slice: S01; Reqs: FR-001, FR-005, FR-018, FR-019; Depends: T001; Evidence: `tests/BpmSoftSync.Domain.Tests/DomainContractTests.cs` proves no `Name`/`Code` reconciliation key and safe reason/scope/recovery/next-action fields; Stop: a domain model accepts secret/session/raw-value data or adapter types.
- [X] T005 Create application ports `IReadOnlyTransport`, `ICatalogSource`, `IRunStore`, `ICatalogQualificationService`, and the initial `IAuthorizationGate` in `src/BpmSoftSync.Application/`; Slice: S01; Reqs: FR-002, FR-010, FR-012, FR-019; Depends: T004; Evidence: compile-time contracts accept only validated endpoint classifications and typed safe envelopes; Stop: a port exposes `HttpClient`, Excel, browser, Git, a Write/Manage operation, or a password argument. **Supersession:** S04-009–S04-010 replace this initial authorization gate with the manual live-invocation policy; no `AuthorizationReference` remains in the current contract.
- [X] T006 [P] Implement offline test doubles `FakeReadOnlyTransport` and `RequestCapture` in `tests/BpmSoftSync.Testing/FakeReadOnlyTransport.cs`; Slice: S01; Reqs: FR-002, FR-018, SC-001; Depends: T005; Evidence: capture can assert endpoint ID/method/path, zero rejected sends and zero write calls without network I/O; Stop: the fake transport opens a socket, persists raw body, or masks a rejected send.

**Checkpoint**: the project builds; shared contracts are typed, fail-closed and offline-only.

---

## Phase 3: User Story 1 — получить безопасный каталог (P1) 🎯 MVP

**Goal**: S01→S02→S03 provide a deterministically read, lossless, offline-qualified catalog outcome or named terminal blocker without HTTP writes.

**Independent Test**: with only fixtures and `FakeReadOnlyTransport`, clean input reaches `HUMAN_REVIEW_REQUIRED`; each malformed endpoint/paging/shape/target-change input gives its named safe blocker, zero writes and no retry.

### S01 tests and implementation — safe offline entry

- [X] T007 [P] [US1] Write allowlist capture tests in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/ReadEndpointAllowlistTests.cs`; Slice: S01; Reqs: FR-002, SC-001; Depends: T004–T006; Evidence: exact `ReadEndpointAllowlist/v1` matrix rejects malformed method/path/body before send and records zero rejected sends/zero writes; Stop: any unclassified endpoint can reach transport.
- [X] T008 [P] [US1] Write in-memory session tests in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/SessionTests.cs`; Slice: S01; Reqs: FR-001; Depends: T004–T006; Evidence: passwords/cookies/CSRF stay ephemeral and are absent from arguments, captures and serialized artifacts; Stop: a terminal prompt is exercised by an offline test or a secret reaches durable storage.
- [X] T009 [P] [US1] Write offline CLI contract tests in `tests/BpmSoftSync.Cli.Tests/CatalogValidateOfflineTests.cs`; Slice: S01; Reqs: FR-002, FR-018, SC-001; Depends: T004–T006; Evidence: `catalog validate-offline --fixture <sanitized-fixture>` returns only safe reason/scope/recovery/next action and uses no network; Stop: the command accepts credentials, a live URI, Excel/Git options or produces a write call.
- [X] T010 [US1] Implement exact endpoint classifier and capture-only transport adapter in `src/BpmSoftSync.Adapters.BpmSoft/ReadEndpointAllowlist.cs` and `src/BpmSoftSync.Adapters.BpmSoft/CapturedReadOnlyTransport.cs`; Slice: S01; Reqs: FR-002, FR-018; Depends: T007, T006; Evidence: T007 passes with `ENDPOINT_NOT_ALLOWLISTED` emitted before send; Stop: prefix/wildcard allowlisting, response-body capture or any HTTP write method.
- [X] T011 [US1] Implement ephemeral session boundary in `src/BpmSoftSync.Adapters.BpmSoft/InMemoryReadOnlySession.cs`; Slice: S01; Reqs: FR-001; Depends: T008, T010; Evidence: T008 passes and disposal clears session state without serializing credential material; Stop: session state enters `IRunStore`, CLI arguments, evidence or skills.
- [X] T012 [US1] Implement fixture-only `catalog validate-offline` command and safe exit mapping in `src/BpmSoftSync.Cli/Commands/CatalogValidateOfflineCommand.cs` and `src/BpmSoftSync.Cli/Program.cs`; Slice: S01; Reqs: FR-002, FR-018, FR-019; Depends: T009–T011; Evidence: T009 passes using only `FakeReadOnlyTransport`; Stop: command performs login, prompts credentials, sends HTTP, or claims live qualification.
- [X] T013 [US1] Verify S01 with `tests/BpmSoftSync.Adapters.BpmSoft.Tests/ReadEndpointAllowlistTests.cs`, `SessionTests.cs`, and `tests/BpmSoftSync.Cli.Tests/CatalogValidateOfflineTests.cs`; Slice: S01; Reqs: FR-001, FR-002, FR-018, FR-019, SC-001; Depends: T010–T012; Evidence: saved offline test output and capture summary show 100% allowlist and zero writes; Stop: any failed test, secret/value leakage, prompt or HTTP send.

### S02 tests and implementation — ordered, lossless inventory

- [X] T014 [P] [US1] Add adversarial paging and sanitized workspace fixtures under `tests/fixtures/read-only/paging-*.json`, `workspace-inventory.json`, and `unknown-shape-*.json`; Slice: S02; Reqs: FR-003, FR-006–FR-008, SC-003, SC-004; Depends: T003, T013; Evidence: fixture manifest records duplicate/overlap/gap/empty-middle/nonempty-after-terminal/loop/max-page and unknown-shape cases; Stop: fixture contains raw lookup data or implies a real target.
- [X] T015 [P] [US1] Write reader paging tests in `tests/BpmSoftSync.Domain.Tests/CatalogReaderPagingTests.cs`; Slice: S02; Reqs: FR-003, FR-004, SC-004; Depends: T014, T004–T006; Evidence: every negative fixture terminates `CATALOG_ORDER_OR_PAGING_UNQUALIFIED` with canonical page-manifest fields and no hang; Stop: false PASS, unbounded loop, missing duplicate/skip/overlap guard or any retry.
- [X] T016 [P] [US1] Write identity and inventory tests in `tests/BpmSoftSync.Domain.Tests/WorkspaceInventoryTests.cs`; Slice: S02; Reqs: FR-005, FR-006, SC-003; Depends: T014, T004; Evidence: same display name in different package layers remains distinct and 100% source items have exactly one support status; Stop: `Name`/`Code` merge or an unclassified item.
- [X] T017 [P] [US1] Write lossless unknown-shape and read-only-index tests in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/UnknownShapeTests.cs`; Slice: S02; Reqs: FR-007–FR-009, SC-003; Depends: T014, T004–T006; Evidence: structural envelope has only safe class/length/hash metadata or a named blocker; indexes derive only from `schema.indexes[].columns[].columnUId`; Stop: default/drop/raw scalar persistence or index mutation intent.
- [X] T018 [P] [US1] Write fingerprint golden and metamorphic tests in `tests/BpmSoftSync.Domain.Tests/TargetFingerprintTests.cs`; Slice: S02; Reqs: FR-004, FR-011, SC-002; Depends: T014, T004; Evidence: reordered JSON properties preserve canonical digest while identity/page/status/version changes alter it; Stop: unproved version default, nondeterministic hash or target mutation treated as PASS.
- [X] T019 [P] [US1] Write legacy disposition validation in `tests/BpmSoftSync.Domain.Tests/LegacyDispositionTests.cs` using `tests/fixtures/read-only/legacy-disposition.json`; Slice: S02; Reqs: FR-017; Depends: T003, T004; Evidence: every adopted legacy semantic is `reuse-semantics|rewrite|drop|defer`; Stop: port-by-copy claim or an unclassified semantic.
- [X] T020 [US1] Implement collection definition, ordered reader, page manifest and termination guards in `src/BpmSoftSync.Domain/CatalogReader.cs` and `src/BpmSoftSync.Domain/PageManifest.cs`; Slice: S02; Reqs: FR-003, FR-004; Depends: T015; Evidence: T015 passes; manifests contain only canonical safe fields; Stop: automatic retry, Pass C concept, unbounded paging or hidden omission.
- [X] T021 [US1] Implement package-layer identity, inventory status and lossless structural envelope adapters in `src/BpmSoftSync.Domain/WorkspaceInventory.cs` and `src/BpmSoftSync.Adapters.BpmSoft/WorkspaceInventoryAdapter.cs`; Slice: S02; Reqs: FR-005–FR-009; Depends: T016, T017, T020; Evidence: T016–T017 pass with no raw lookup scalar and `INDEX_SYNC_UNRESOLVED` retained; Stop: Name/Code join, unknown-shape default/drop, Excel/compare/index plan/load/apply code.
- [X] T022 [US1] Implement canonical `TargetFingerprint/v1` in `src/BpmSoftSync.Domain/TargetFingerprint.cs`; Slice: S02; Reqs: FR-004, FR-011; Depends: T018, T020–T021; Evidence: T018 golden/metamorphic vectors pass; Stop: digest depends on JSON enumeration order or undocumented live-state assumption.
- [X] T023 [US1] Implement validated legacy disposition manifest reader in `src/BpmSoftSync.Domain/LegacyDispositionManifest.cs`; Slice: S02; Reqs: FR-017; Depends: T019, T021; Evidence: T019 passes and no `preparation/*` source is a runtime input; Stop: source-code copying or an unclassified behavior.
- [X] T024 [US1] Verify S02 with `tests/BpmSoftSync.Domain.Tests/CatalogReaderPagingTests.cs`, `WorkspaceInventoryTests.cs`, `TargetFingerprintTests.cs`, `LegacyDispositionTests.cs`, and `tests/BpmSoftSync.Adapters.BpmSoft.Tests/UnknownShapeTests.cs`; Slice: S02; Reqs: FR-003–FR-009, FR-011, FR-017, SC-002–SC-004; Depends: T020–T023; Evidence: offline test report records named blockers, full status coverage and deterministic digests; Stop: false PASS, nonterminal paging, raw-value evidence or any index mutation.

---

## Phase 3: Corrective Slice S04 — production workflow readiness (US1–US3)

**Purpose**: reconcile incomplete S01–S03 code into one fixture/fake-HTTP-proven reader → exact-two-pass qualification/reconciliation → per-`RunId` safe-evidence workflow. These are the only implementation tasks for the mapped corrective work; T025–T042 below remain their original canonical acceptance records and are not duplicate implementation instructions.

**Independent Test**: the CLI composition root processes a clean sanitized fixture and fake-handler HTTP through the same workflow, performs exactly Pass A and Pass B, produces validated/scanned append-only evidence and returns `HUMAN_REVIEW_REQUIRED`; every adversarial input returns its named blocker with zero writes, no live target and no retry.

- [ ] S04-001 [P] [US1] Reconcile `ReadEndpointAllowlist/v1` in `specs/001-read-only-catalog-qualification/research.md`, `src/BpmSoftSync.Adapters.BpmSoft/ReadEndpointAllowlist.cs`, `specs/001-read-only-catalog-qualification/contracts/cli-contract.md`, `tests/fixtures/read-only/`, and `tests/BpmSoftSync.Adapters.BpmSoft.Tests/ReadEndpointAllowlistTests.cs`; Slice: S04; Reqs: FR-002, FR-018, SC-001; Depends: T024; Evidence: Decision 1 exact four-endpoint matrix removes undocumented `GET_PACKAGES` and fake capture proves zero sends for host/query/fragment/traversal/method/path/body mismatch; Stop: heuristic/wildcard admission, redirect or alternate host.
- [ ] S04-002 [P] [US1] Replace capture-only contracts with classified request/typed response and typed catalog-pass input in `src/BpmSoftSync.Application/Ports.cs`, `src/BpmSoftSync.Adapters.BpmSoft/`, `tests/BpmSoftSync.Adapters.BpmSoft.Tests/`, and `tests/BpmSoftSync.Cli.Tests/ArchitectureTests.cs`; Slice: S04; Reqs: FR-001–FR-005, FR-018–FR-019; Depends: S04-001; Evidence: fixture and fake-HTTP sources execute one application use case while Domain remains free of HTTP/console/Excel/browser/Git types; Stop: second HTTP path or reader bypass.
- [ ] S04-003 [P] [US1] Implement Decision 2 canonical JSON `TargetFingerprint/v1` in `src/BpmSoftSync.Domain/TargetFingerprint.cs` and `tests/BpmSoftSync.Domain.Tests/TargetFingerprintTests.cs`; Slice: S04; Reqs: FR-004, FR-011, SC-002; Depends: T024; Evidence: golden/metamorphic vectors prove NFC strings, lowercase dashed GUIDs, invariant numbers, sorted keys and domain list order; Stop: delimiter preimage, enumeration-order digest or raw-value preimage/evidence.
- [ ] S04-004 [P] [US1] Complete declared-order reader, page semantics and lossless unknown-shape validation in `src/BpmSoftSync.Domain/CatalogReader.cs`, `src/BpmSoftSync.Domain/PageManifest.cs`, `src/BpmSoftSync.Domain/WorkspaceInventory.cs`, `src/BpmSoftSync.Adapters.BpmSoft/WorkspaceInventoryAdapter.cs`, and paging/shape tests; Slice: S04; Reqs: FR-003–FR-009, SC-002–SC-004; Depends: S04-002; Evidence: registered order/query and bounded matrix cover progress, duplicate/overlap/gap, empty-middle, nonempty-after-terminal, loop/max pages and safe envelope/blocker; Stop: unordered reconciliation, hang or default/drop.
- [ ] S04-005 [P] [US1] Complete fixture-manifest verification in `tests/fixtures/read-only/fixture-manifest.json`, `tests/fixtures/read-only/`, and `tests/BpmSoftSync.Domain.Tests/FixtureManifestTests.cs`; Slice: S04; Reqs: FR-001–FR-004, FR-012–FR-014; Depends: T024; Evidence: SHA-256 inventory rejects unregistered, missing, changed or duplicate fixtures and covers target change, endpoint, paging, malformed response, schema and leakage; Stop: validation of pre-listed hashes only.
- [ ] S04-006 [US1] Implement the real two-pass use case in `src/BpmSoftSync.Application/CatalogQualificationService.cs`, `src/BpmSoftSync.Domain/CatalogQualification.cs`, and `tests/BpmSoftSync.Domain.Tests/CatalogQualificationTests.cs`; Slice: S04; Reqs: FR-004–FR-011, SC-002; Depends: S04-002–S04-005; Evidence: source-read counter proves exactly two independently inspected reader/inventory/manifests/fingerprint passes; Stop: synthetic-pass delegation, Pass C, retry or reader bypass.
- [ ] S04-007 [US1] Expand terminal reconciliation in `src/BpmSoftSync.Domain/CatalogQualification.cs` and `tests/BpmSoftSync.Domain.Tests/CatalogQualificationTests.cs`; Slice: S04; Reqs: FR-004, FR-010–FR-011, SC-002; Depends: S04-006; Evidence: scope, fingerprint components, ordered identities, counts, manifests and unsupported set produce named terminal mismatch with `RetryCount = 0`; Stop: target change warning or third read.
- [ ] S04-008 [P] [US1] Complete diagnostic-only `WorkbookScaleForecast/v1` in `src/BpmSoftSync.Domain/WorkbookScaleForecast.cs` and `tests/BpmSoftSync.Domain.Tests/WorkbookScaleForecastTests.cs`; Slice: S04; Reqs: FR-010; Depends: S04-006; Evidence: table vectors cover boundary/negative/incomplete telemetry and return `WORKBOOK_SCALE_DECISION_REQUIRED` without Excel I/O; Stop: guessed input, workbook mutation or qualification claim.
- [ ] S04-009 [P] [US1] Define and test settled manual live-invocation admission in `tests/BpmSoftSync.Cli.Tests/ManualLiveInvocationTests.cs`, `tests/BpmSoftSync.Cli.Tests/AuthorizationGateTests.cs`, and `src/BpmSoftSync.Application/AuthorizationGate.cs`; Slice: S04; Reqs: FR-010, FR-018, SC-002; Depends: S04-001, S04-006; Evidence: offline/automatic prompt and send spies are zero; only manual interactive `catalog qualify` or direct current request reaches a terminal prompt, without `AuthorizationReference`; Stop: autonomous start, offline prompt/send or secret persistence.
- [ ] S04-010 [US1] Implement safe `InvocationSource` policy and remove authorization-reference verifier from `src/BpmSoftSync.Cli/Commands/CatalogQualifyCommand.cs`, `src/BpmSoftSync.Cli/Program.cs`, `src/BpmSoftSync.Application/Ports.cs`, and CLI tests; Slice: S04; Reqs: FR-010, FR-018; Depends: S04-009; Evidence: `ManualTerminal`/`DirectCurrentChatRequest` are the only recorded safe values and automatic/offline paths reject admission; Stop: persisted approval token, chat text or credentials.
- [ ] S04-011 [US2] Refactor one-`RunId` append-only lifecycle in `src/BpmSoftSync.Adapters.FileSystem/AppendOnlyRunStore.cs`, `RunJournalWriter.cs`, and `tests/BpmSoftSync.Adapters.FileSystem.Tests/RunStoreTests.cs`; Slice: S04; Reqs: FR-012, FR-019, SC-006; Depends: S04-006; Evidence: collision/concurrency/journal-order tests prove one root under `runs/yyyy/MM/dd/<RunId>/` and no overwrite; Stop: deletion, replacement or root-per-envelope.
- [ ] S04-012 [US2] Implement typed `EvidenceEnvelope/v1` validation and scan-before-write/seal in `src/BpmSoftSync.Adapters.FileSystem/EvidenceEnvelopeValidator.cs`, `SecretValueScanner.cs`, and evidence/scanner/metadata tests; Slice: S04; Reqs: FR-012–FR-014, SC-005–SC-006; Depends: S04-011; Evidence: malformed/canary envelopes yield safe blocked records and no PASS seal; scanner reports only category/location/digest; Stop: generic JSON, bypass, redact-after-write or raw match.
- [ ] S04-013 [US2] Persist a single safe qualification outcome package in `src/BpmSoftSync.Application/`, `src/BpmSoftSync.Adapters.FileSystem/`, and filesystem/application tests; Slice: S04; Reqs: FR-010, FR-012–FR-014, FR-019; Depends: S04-007–S04-012; Evidence: every clean/failure fixture has RunId-linked validated/scanned input digests, pass summaries, inventory/forecast/gates and terminal seal; Stop: raw target/response, missing failure evidence or treating `HUMAN_REVIEW_REQUIRED` as approval.
- [ ] S04-014 [US3] Implement bounded safe `catalog diagnose --run <RunId>` in `src/BpmSoftSync.Cli/Commands/CatalogDiagnoseCommand.cs`, `src/BpmSoftSync.Cli/Diagnostics/SafeDiagnosticRenderer.cs`, safe run reader, and CLI tests; Slice: S04; Reqs: FR-015, FR-018–FR-019; Depends: S04-013; Evidence: clean/paging/admission/scan/missing-run process cases render only validated blocker/scope/recovery/next action; Stop: raw-file output, mutation or unbounded listing.
- [ ] S04-015 [US1] Implement fake-handler-proven `HttpCatalogSource` and canonical request factory in `src/BpmSoftSync.Adapters.BpmSoft/`, with tests in `tests/BpmSoftSync.Adapters.BpmSoft.Tests/`; Slice: S04; Reqs: FR-001–FR-004, FR-018; Depends: S04-001–S04-004; Evidence: local fake handler uses declared endpoint metadata and same parser/reader; redirects and non-loopback origins reject; Stop: direct arbitrary `HttpClient`, URL/query or real target.
- [ ] S04-016 [US1] Implement in-memory interactive session after permitted invocation in `src/BpmSoftSync.Adapters.BpmSoft/InMemoryReadOnlySession.cs` and session/CLI tests; Slice: S04; Reqs: FR-001, FR-010, FR-018; Depends: S04-009–S04-010, S04-015; Evidence: prompt/send counters remain zero offline/automatic and disposal clears ephemeral state; Stop: secret in args/config/evidence/logs or real BPMSoft use.
- [ ] S04-017 [US1] Wire fixture and HTTP adapters through one CLI/application composition root in `src/BpmSoftSync.Cli/Program.cs`, `CatalogValidateOfflineCommand.cs`, `CatalogQualifyCommand.cs`, and process tests; Slice: S04; Reqs: FR-001–FR-019; Depends: S04-006–S04-016; Evidence: `Program.Main` covers fixture, automatic rejection, bad command and diagnosis through full qualification/evidence lifecycle; Stop: command-specific helper flow or success without lifecycle.
- [ ] S04-018 [US3] Update executable offline and conditional-manual handoff materials under `docs/read-only-handoff/` with `tests/BpmSoftSync.Cli.Tests/HandoffPackageTests.cs`; Slice: S04; Reqs: FR-015–FR-016, FR-018–FR-019, SC-007; Depends: S04-010, S04-014, S04-017; Evidence: fixture dry-run and document tests prove commands/no absolute paths/secrets/false live claim and T046 remains conditional; Stop: skill credentials or browser/Excel/Apply/Git instruction.
- [ ] S04-019 [US3] Add CLI, architecture and skill-boundary regression in `tests/BpmSoftSync.Cli.Tests/CliContractTests.cs`, `ArchitectureTests.cs`, and relevant docs/skill scans; Slice: S04; Reqs: FR-001–FR-002, FR-015–FR-019, SC-001, SC-007; Depends: S04-014, S04-017–S04-018; Evidence: dispatch/exit/reference/secret-argument scans and fixture dry-run have safe fields and zero writes; Stop: Domain adapter dependency, hidden credential channel or untested document command.
- [ ] S04-020 [US1] Implement composition-root offline E2E in `tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationE2ETests.cs`; Slice: S04; Reqs: FR-001–FR-019, SC-001–SC-007; Depends: S04-001–S04-019; Evidence: independently calculated manifests/hashes and single run tree prove two passes, deterministic fingerprint, scanned append-only evidence, `HUMAN_REVIEW_REQUIRED` and zero writes; Stop: helper-only test, false PASS or unscanned artifact.
- [ ] S04-021 [US1] Implement adversarial, metamorphic and leakage composition-root regression in `tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationSecurityRegressionTests.cs`; Slice: S04; Reqs: FR-002–FR-004, FR-010–FR-014, FR-017–FR-019, SC-001–SC-006; Depends: S04-005, S04-007, S04-012, S04-015–S04-020; Evidence: full matrix gives named blockers or deterministic hashes, <=2 reads, no raw-value leak and zero writes; Stop: hang, retry/Pass C, scanner bypass or method-only classification.
- [ ] S04-022 [US1] Re-evaluate original T025–T042 individually against their unchanged original text and stop conditions in `specs/001-read-only-catalog-qualification/tasks.md` and a safe review table under `specs/001-read-only-catalog-qualification/verification/`; Slice: S04; Reqs: FR-001–FR-019, SC-001–SC-007; Depends: S04-001–S04-021; Evidence: each row identifies original task, supporting S04 task(s), changed source/test paths, fixture digest, command/exit code, artifact and reviewer result; Stop: bulk status update or test filename/PASS line as completion.
- [ ] S04-023 [US1] Prepare and control the fresh-evidence protocol for canonical T043–T045 in `tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationE2ETests.cs`, `ReadOnlyQualificationSecurityRegressionTests.cs`, and `specs/001-read-only-catalog-qualification/verification/offline-validation.md`; Slice: S04; Reqs: FR-012–FR-019, SC-001–SC-007; Depends: S04-022; Evidence: a control checkpoint identifies the completed composition root, fixture IDs/SHA-256, commands, result destinations and failure-recording rules for the later one-time T043/T044 reruns and sole T045 package; Stop: running/closing T043 or T044 as S04-023, creating a duplicate evidence package, historical PASS reuse, omitted failure or live/acceptance/Apply claim.

**Checkpoint**: S04 planning and S04-023 create no T043–T045 completion evidence. Mark original T025–T042 only after S04-022 and their original criteria are actually evidenced; after the S04-023 control checkpoint, run T043 and T044 once each, then create the sole fresh T045 package. T046 remains excluded from S04 automated work and human-gated.

---

### S03 tests and implementation — exact-two-pass qualification

**S04 execution mapping.** The unchanged T025–T042 text below is the canonical
acceptance ledger, not a second implementation stream. Its historic `Depends` fields
preserve the original S03 plan; for S04 the following map is authoritative and makes
the corrective work dependency-ordered. None of these original tasks may be checked
until S04-022 records its individual evidence review.

| Original task(s) | Corrective implementation and permitted closure |
|---|---|
| T025 | S04-001…S04-007, S04-013, S04-017 → S04-022 |
| T026, T029 | S04-006, S04-008, S04-017 → S04-022 |
| T027, T030 | S04-001, S04-009…S04-010, S04-015…S04-017 → S04-022 |
| T028, T031 | S04-002…S04-007, S04-013, S04-017 → S04-022 |
| T032, T035 | S04-011…S04-013 → S04-022 |
| T033, T034, T036 | S04-005, S04-011…S04-013 → S04-022 |
| T037 | S04-011…S04-013, S04-020…S04-021 → S04-022 |
| T038, T040 | S04-001…S04-002, S04-014…S04-019 → S04-022 |
| T039, T041 | S04-009…S04-010, S04-017…S04-019 → S04-022 |
| T042 | S04-014, S04-017…S04-021 → S04-022 |

**Original-task status rule:** preserve every `[ ]` shown below until the row-level
evidence specified by S04-022 exists. Existing source, a class/test filename, an old
offline report, planning text, or this mapping is insufficient evidence.

- [ ] T025 [P] [US1] Write reconciliation and target-change process tests in `tests/BpmSoftSync.Domain.Tests/CatalogQualificationTests.cs` with `tests/fixtures/read-only/target-state-change.json`; Slice: S03; Reqs: FR-004, FR-010, FR-011, SC-002; Depends: T024; Evidence: clean fixture makes exactly Pass A and Pass B, while target change seals `TARGET_STATE_CHANGED_DURING_QUALIFICATION` with no Pass C/retry; Stop: third pass, automatic new double pass, retry loop or downgraded warning.
- [ ] T026 [P] [US1] Define and test safe `WorkbookScaleForecast/v1` in `tests/BpmSoftSync.Domain.Tests/WorkbookScaleForecastTests.cs`; Slice: S03; Reqs: FR-010; Depends: T024; Evidence: deterministic fixture vectors verify diagnostic buckets/status, `WORKBOOK_SCALE_DECISION_REQUIRED` for incomplete/over-limit evidence, and zero Excel I/O; Stop: forecast reads/writes Excel, becomes full-catalog acceptance, or guesses an unavailable value.
- [ ] T027 [P] [US1] Write manual live-invocation boundary tests in `tests/BpmSoftSync.Cli.Tests/ManualLiveInvocationTests.cs`; Slice: S03; Reqs: FR-010, FR-018, SC-002; Depends: T013, T024; Evidence: offline and automatic paths have zero terminal credential prompts and zero HTTP sends; a manually started interactive `catalog qualify` reaches the terminal prompt without `AuthorizationReference`; Stop: an agent starts live work without a direct current user request, an offline path prompts/sends, or a test uses real credentials/target.
- [ ] T028 [US1] Implement exact-two-pass qualification orchestrator and terminal reconciliation in `src/BpmSoftSync.Application/CatalogQualificationService.cs` and `src/BpmSoftSync.Domain/CatalogQualification.cs`; Slice: S03; Reqs: FR-004, FR-010, FR-011; Depends: T025, T020–T022; Evidence: T025 passes for matching and target-change fixtures; Stop: Pass C, retry, automatic requalification, automatic live start or Apply permission.
- [ ] T029 [US1] Implement diagnostic-only `WorkbookScaleForecast/v1` in `src/BpmSoftSync.Domain/WorkbookScaleForecast.cs`; Slice: S03; Reqs: FR-010; Depends: T026, T028; Evidence: T026 passes and component consumes only counts/telemetry/declared limits; Stop: any Excel I/O, workbook mutation or interpretation as a qualified catalog.
- [ ] T030 [US1] Implement the manual live-invocation policy in `src/BpmSoftSync.Cli/Commands/CatalogQualifyCommand.cs`; Slice: S03; Reqs: FR-010, FR-018; Depends: T027, T028; Evidence: T027 passes; only a manual interactive CLI invocation or a direct current user request can enter the terminal credential prompt, with no `AuthorizationReference`; Stop: accepting a secret argument/config value, autonomous agent start, or a live transport send before terminal interaction.
- [ ] T031 [US1] Verify the US1 S03 qualification subset with `tests/BpmSoftSync.Domain.Tests/CatalogQualificationTests.cs`, `WorkbookScaleForecastTests.cs`, and `tests/BpmSoftSync.Cli.Tests/ManualLiveInvocationTests.cs`; Slice: S03; Reqs: FR-004, FR-010, FR-011, SC-002; Depends: T028–T030; Evidence: offline report shows exact-two-pass only, `retryCount = 0`, safe forecast and offline/automatic counts at zero prompt/send; Stop: any terminal blocker is retried or test opens a live connection.

**Checkpoint**: US1 is independently demonstrable on sanitized fixtures only; `HUMAN_REVIEW_REQUIRED` is not Apply permission or an automatic live-start instruction.

---

## Phase 4: User Story 2 — проверить доказательства запуска (P1)

**Goal**: reviewer can inspect append-only, schema-validated and secret-free audit/evidence for an offline run.

**Independent Test**: two same-time fixture runs produce distinct roots; forbidden fields/canaries reject 100% and prevent a PASS seal.

- [ ] T032 [P] [US2] Write append-only root and journal tests in `tests/BpmSoftSync.Adapters.FileSystem.Tests/RunStoreTests.cs`; Slice: S03; Reqs: FR-012, SC-006; Depends: T004–T006; Evidence: same-time runs use distinct `RunId` roots, pre-existing root/overwrite fail, and paths follow `runs/yyyy/MM/dd/<RunId>/`; Stop: overwrite, root collision or mutable previous run.
- [ ] T033 [P] [US2] Write evidence schema and scanner canary tests in `tests/BpmSoftSync.Adapters.FileSystem.Tests/EvidenceEnvelopeTests.cs` and `tests/BpmSoftSync.Adapters.FileSystem.Tests/SecretValueScannerTests.cs`; Slice: S03; Reqs: FR-014, SC-005, SC-006; Depends: T004–T006; Evidence: forbidden key/value canaries are 100% rejected and scanner emits only category/location/digest; Stop: redact-after-write, raw matched value in report, or PASS after schema/scan failure.
- [ ] T034 [P] [US2] Write allowed audit metadata tests in `tests/BpmSoftSync.Adapters.FileSystem.Tests/AuditMetadataTests.cs`; Slice: S03; Reqs: FR-013, FR-014; Depends: T004–T006; Evidence: allowlisted application/template/skills/BPMSoft versions, Excel table modification times, target alias, optional plan hash, safe `InvocationSource` and gate outcomes pass; unknown fields fail; Stop: a secret/raw value is allowed or metadata invents a persisted approval reference.
- [ ] T035 [US2] Implement date-based append-only run store and journal writer in `src/BpmSoftSync.Adapters.FileSystem/AppendOnlyRunStore.cs` and `src/BpmSoftSync.Adapters.FileSystem/RunJournalWriter.cs`; Slice: S03; Reqs: FR-012, SC-006; Depends: T032; Evidence: T032 passes with separate audit/evidence roots and no overwrite; Stop: deletions, replacement writes, tracked-workbook storage or a nonunique root.
- [ ] T036 [US2] Implement `EvidenceEnvelope/v1` allow-by-schema validation, metadata allowlist and pre-write/pre-seal scanner in `src/BpmSoftSync.Adapters.FileSystem/EvidenceEnvelopeValidator.cs` and `src/BpmSoftSync.Adapters.FileSystem/SecretValueScanner.cs`; Slice: S03; Reqs: FR-013, FR-014, SC-005; Depends: T033–T035; Evidence: T033–T034 pass and failure artifacts remain scanned/append-only; Stop: generic JSON, unrecognized field, secret/value persistence or successful seal after failure.
- [ ] T037 [US2] Verify US2 with `tests/BpmSoftSync.Adapters.FileSystem.Tests/RunStoreTests.cs`, `EvidenceEnvelopeTests.cs`, `SecretValueScannerTests.cs`, and `AuditMetadataTests.cs`; Slice: S03; Reqs: FR-012–FR-014, SC-005, SC-006; Depends: T035–T036; Evidence: saved offline security report proves distinct roots, schema/hash validation and no PASS artifact after canaries; Stop: any scan/schema failure leaks a value or permits PASS.

**Checkpoint**: US2 produces only safe, append-only offline evidence; it does not close any human gate.

---

## Phase 5: User Story 3 — безопасная диагностика и ранний handoff (P2)

**Goal**: operator receives safe blocker guidance and a new operator can reach the offline qualification decision point without secrets or hidden paths.

**Independent Test**: fake CLI and documented handoff on a clean fixture reach `HUMAN_REVIEW_REQUIRED`, while skills/CLI neither take approvals nor contain business rules.

- [ ] T038 [P] [US3] Write CLI safe-diagnostic and architecture boundary tests in `tests/BpmSoftSync.Cli.Tests/CliContractTests.cs` and `tests/BpmSoftSync.Cli.Tests/ArchitectureTests.cs`; Slice: S01; Reqs: FR-015, FR-018, FR-019, SC-007; Depends: T012, T024, T037; Evidence: output contains only safe reason/scope/recovery/next action and architecture forbids Domain/Application references to HTTP/Excel/browser/Git; Stop: a skill/CLI accepts approval or secret, or domain depends on an adapter.
- [ ] T039 [P] [US3] Write handoff package tests in `tests/BpmSoftSync.Cli.Tests/HandoffPackageTests.cs`; Slice: S03; Reqs: FR-016, FR-018, SC-007; Depends: T013, T031, T037; Evidence: tests require a secret-free configuration schema, prerequisites, early runbook, failure path, no hidden local paths and fixture-only `HUMAN_REVIEW_REQUIRED`; Stop: docs require credentials in skill context or suggest a live/browser/write action.
- [ ] T040 [US3] Implement safe blocker diagnosis and exit mapping in `src/BpmSoftSync.Cli/Diagnostics/SafeDiagnosticRenderer.cs` and `src/BpmSoftSync.Cli/Commands/CatalogDiagnoseCommand.cs`; Slice: S03; Reqs: FR-018; Depends: T038, T030, T036; Evidence: T038 passes for paging, authorization and scan blockers without raw response/value output; Stop: diagnosis reveals raw data/secrets, changes state or authorizes a next forbidden action.
- [ ] T041 [US3] Create the early handoff package at `docs/read-only-handoff/configuration-schema.md`, `docs/read-only-handoff/install-update-prerequisites.md`, `docs/read-only-handoff/operator-runbook.md`, and `docs/read-only-handoff/failure-path.md`; Slice: S03; Reqs: FR-015, FR-016, FR-018, SC-007; Depends: T039–T040; Evidence: T039 passes and documents fixture/offline setup, safe CLI commands, blockers and the manual live-invocation boundary; Stop: documentation contains secret, hidden local path, automatic agent start, browser/Git/Excel/Apply step or live credential instruction outside terminal prompt.
- [ ] T042 [US3] Verify US3 with `tests/BpmSoftSync.Cli.Tests/CliContractTests.cs`, `ArchitectureTests.cs`, and `HandoffPackageTests.cs`; Slice: S01+S03; Reqs: FR-015, FR-016, FR-018, FR-019, SC-007; Depends: T040–T041; Evidence: clean fake-transport dry run reaches only `HUMAN_REVIEW_REQUIRED` and test output contains no secret/value; Stop: test invokes live BPMSoft, terminal credential prompt, write HTTP, browser, Excel or Git.

**Checkpoint**: all stories are independently testable offline. `HUMAN_REVIEW_REQUIRED` remains an evidence decision point, not acceptance, Apply permission or an automatic live-start instruction.

---

## Phase 6: Final solution validation and controlled handoff (S03)

**Purpose**: validate the approved Feature 001 offline scope end-to-end, preserve evidence, and describe—but do not autonomously start—the separately authorized manual live path.

- [ ] T043 Run clean offline end-to-end qualification in `tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationE2ETests.cs`; Slice: S03; Reqs: FR-001–FR-019, SC-001–SC-007; Depends: T031, T037, T042; Evidence: fake transport yields exactly two reconciled passes, complete inventory, deterministic fingerprint, safe append-only evidence, `HUMAN_REVIEW_REQUIRED` and zero write calls; Stop: real target/credential use, false PASS, or any gate closure.
- [ ] T044 [P] Run adversarial, metamorphic and leakage regression in `tests/BpmSoftSync.Cli.Tests/ReadOnlyQualificationSecurityRegressionTests.cs`; Slice: S03; Reqs: FR-002–FR-004, FR-010–FR-014, FR-017–FR-019, SC-001–SC-006; Depends: T031, T037, T042; Evidence: all paging/mutation/unknown-shape/endpoint/schema/canary fixtures return named blocker, no hang, no Pass C/retry and no raw-value leakage; Stop: any automatic repeat double pass, scanner bypass or write send.
- [ ] T045 Update only safe offline verification evidence in `specs/001-read-only-catalog-qualification/verification/offline-validation.md`; Slice: S03; Reqs: FR-012–FR-019, SC-001–SC-007; Depends: T043–T044; Evidence: document links fixture IDs/digests, command outputs, schema/scan summaries and test results without secrets or real target data; Stop: an artifact claims live qualification, acceptance, Apply permission or persists a forbidden value.
- [ ] T046 Perform the conditional manual read-only verification procedure defined in `docs/read-only-handoff/operator-runbook.md` only after T043–T045 pass: the operator manually starts `catalog qualify` for one exact target and declared read scope, or an agent acts only on a direct current user request in an available chat; Slice: S03; Reqs: FR-010, FR-018, SC-002; Depends: T027, T043–T045; Evidence: safe RunId-linked evidence under `runs/yyyy/MM/dd/<RunId>/`, exactly two passes, zero write calls and terminal-only credential entry by the operator; Stop: autonomous agent start, secret persistence, target change, or any write/browser/Excel/Git action. After `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, do not Pass C/retry/re-run without a new manual invocation or direct current user request.

**S04 final-evidence mapping:** S04-023 is only the preparatory/control checkpoint for
the fresh-evidence protocol. T043 and T044 themselves each run once against the completed
composition root after that checkpoint; T045 is the sole package created after both
successful reruns. S04-023 neither runs nor closes T043/T044 and does not create a second
completion cycle.
T046 is deliberately not mapped to a S04 checklist task: it stays `[ ]`, requires fresh
T043–T045 evidence plus a separately authorized `ManualTerminal` or
`DirectCurrentChatRequest`, and is never performed by automated implementation/tests.

---

## Dependencies & execution order

```text
T001–T003 → T004–T006
  → US1/S01 T007–T013
  → US1/S02 T014–T024
  → S04-001…S04-005 → S04-006…S04-014 → S04-015…S04-021
  → S04-022 (individual T025–T042 review)
  → S04-023 (control only) → T043 + T044 (actual reruns) → T045 (sole package)
  → T046 (conditional human/manual gate only)
```

### Requirement → slice → task → check

| Requirement group | Approved slice | Tasks | Planned check / safe evidence |
|---|---|---|---|
| FR-001–FR-002; FR-018–FR-019; SC-001 | S01 | T001–T013, T038, T042 | allowlist/session/CLI/architecture tests; fake capture with zero writes |
| FR-003–FR-009; FR-011; FR-017; SC-002–SC-004 | S02 | T014–T024 | paging, identity, inventory, unknown-shape, fingerprint and legacy-disposition fixtures |
| FR-004; FR-010–FR-011; SC-002 | S03+S04 | S04-001…S04-010; T025–T031; T046 | exact-two-pass, no-Pass-C/retry, forecast and manual live-invocation-boundary tests; T046 conditional/manual only |
| FR-012–FR-014; SC-005–SC-006 | S03+S04 | S04-011…S04-013; T032–T037 | one-RunId append-only roots, envelope schema, scanner and audit metadata tests |
| FR-015–FR-016; FR-018–FR-019; SC-007 | S01+S03+S04 | S04-014…S04-023; T038–T045 | CLI/skills boundary, executable handoff and fresh composition-root E2E/security evidence |

### Parallel opportunities

- T002–T003 after T001; T006 after T005.
- Within US1/S01: T007–T009; within US1/S02: T014–T019; within S04 Phase A: S04-001…S04-005 where their paths do not overlap.
- S04-008 and S04-011 may proceed once their stated inputs are ready; the actual T043 and T044 reruns may run in parallel only after the S04-023 control checkpoint and all common prerequisites are complete.

## Implementation strategy

1. **MVP correction**: T001–T024 plus S04-001…S04-010 deliver the actual fixture-only, exact-two-pass workflow and manual-admission boundary.
2. **Increment 2**: S04-011…S04-014 adds one-RunId safe evidence and diagnosis.
3. **Increment 3**: S04-015…S04-021 proves both adapters and the composition root offline; S04-022 reconciles original task evidence without bulk closure.
4. **Validation**: S04-023 only controls the fresh-evidence protocol; actual T043/T044 reruns occur once each and T045 is the sole resulting package. T046 is a separate conditional human-controlled procedure, never an automated continuation.

## Format validation

- T001–T046 retain their established `- [ ] T### [P?] [US?]` format and statuses; S04-001–S04-023 use the same strict checklist format with stable corrective IDs so the approved supplemental work is traceable without renumbering existing canonical IDs.
- Every story task has `[US1]`, `[US2]`, or `[US3]`; setup, foundational and final tasks have no story label.
- Every task declares Slice, requirement IDs, exact path/component, dependency, safe evidence and stop condition.
