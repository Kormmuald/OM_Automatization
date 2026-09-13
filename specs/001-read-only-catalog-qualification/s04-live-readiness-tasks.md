# S04 — corrective tasks for closing T025–T045

## Как пользоваться этим списком

Это отдельный implementation task list для Feature 001. Он дополняет, но не редактирует
исторический `tasks.md`: исходные T025–T046 остаются `[ ]`, пока их actual acceptance criteria
и перечисленное evidence не будут повторно проверены. `S04-*` — новые corrective tasks, а не
замена номеров T025–T046.

Все работы используют только local sanitized fixtures, fake transport или fake
`HttpMessageHandler`. В этой задаче запрещены real BPMSoft, credentials, browser, Excel,
compare, Apply, Git и Write/Manage. Нельзя ставить `[X]` по наличию класса, старому harness
PASS либо обновлённому документу без соответствующего task-level evidence.

**Immutable source material:** `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`
и все остальные файлы в `preparation/docs/product-specs/` не изменяются.

## Общие правила completion evidence

Для каждой закрываемой исходной задачи evidence должен включать: изменённые test/source
paths, fixture ID + SHA-256 (если есть fixture), команду и exit code, утверждения, которые
проверяют именно её stop conditions, и путь к безопасному output/run artifact. Все expected
manifests, counts and hashes в E2E вычисляются из входной fixture независимо от production
reader. Никакие raw credential, cookie, CSRF, login response или raw lookup value не попадают
в output, fixtures, assertion message или evidence.

## Phase A — зафиксировать контракты и восстановить S01/S02 prerequisites

- [ ] S04-001 Reconcile the exact `ReadEndpointAllowlist/v1` in `research.md`, adapter code,
  CLI contract and fixtures. Decision 1 is settled: remove undocumented `GET_PACKAGES` as a
  corrective defect; extension is permitted only after an owner-approved amendment to
  `research.md`. Enumerate each permitted ID/method/path/body shape and all rejection cases.
  **Supports:** T025, T027, T038, T043, T044. **Evidence:** parameterized
  allowlist matrix and a capture/fake-handler test prove zero sends for host/query/fragment/
  traversal/method/path/body mismatch. **Stop:** method-based or endpoint-name heuristic,
  undocumented endpoint, redirect or alternate host.

- [ ] S04-002 Replace the capture-only transport contract with a narrow classified-request /
  typed-response boundary and make `ICatalogSource` return a typed catalog-pass input rather
  than only validate a fixture path. Keep the Domain free of HTTP, `HttpClient`, console,
  Excel, browser and Git types. **Supports:** T025, T028, T038, T043. **Evidence:** compile-
  time project-reference/architecture tests plus one fixture source and one fake-HTTP source
  exercising the same application use case. **Stop:** a second HTTP entry point or a helper
  that bypasses the reader.

- [ ] S04-003 Implement canonical JSON serialization for `TargetFingerprint/v1` exactly as
  described by Decision 2: NFC strings, dashed lowercase GUIDs, invariant numbers, sorted
  object keys and domain-defined list ordering. **Supports:** T025, T028, T031, T043, T044.
  **Evidence:** golden and metamorphic vectors cover reordered properties, Unicode, one
  changed identity/page/status/version and secret/raw-value canaries. **Stop:** delimiter-
  joined preimage, JSON enumeration-order dependence or raw value in preimage/evidence.

- [ ] S04-004 Complete reader validation for declared stable ordering and all page semantics:
  progress, duplicate/overlap/gap, empty-middle, nonempty-after-terminal, loop and max-pages.
  Bind every collection to registered order key/query builder; unknown response shape yields a
  lossless safe envelope or a blocker. **Supports:** T025, T028, T031, T043, T044. **Evidence:**
  full named fixture matrix bounded by a termination counter, with no dropped items/defaults.
  **Stop:** unordered-set reconciliation, a hanging test or default/drop of unknown shape.

- [ ] S04-005 Make fixture-manifest validation complete: it must fail for an unregistered,
  missing, changed or duplicate fixture and cover target-change, endpoint, paging, malformed
  response, schema and leakage fixtures. **Supports:** T025, T033, T037, T043, T044, T045.
  **Evidence:** negative manifest tests plus SHA-256 list generated from the full fixture
  directory. **Stop:** checking hashes only for pre-listed entries.

## Phase B — complete actual S03 qualification and evidence lifecycle

- [ ] S04-006 Implement the production qualification use case so one invocation obtains Pass
  A and Pass B via the typed source and common reader, creates complete inventory/manifests,
  computes fingerprints and calls terminal reconciliation. It must not merely delegate two
  synthetic `QualificationPass` values. **Supports:** T025, T028, T031, T043. **Evidence:**
  process tests show exactly two source reads and independently verify each pass result. **Stop:**
  Pass C, retry, a new automatic double pass or a bypass of reader/inventory.

- [ ] S04-007 Expand reconciliation to compare scope binding, fingerprint components, ordered
  identity digests, counts, page manifests and unsupported set, and classify paging versus
  target-state blockers safely. **Supports:** T025, T028, T031, T044. **Evidence:** matching,
  mutation and per-component mismatch tests with `RetryCount = 0`, terminal seal request and
  source-read counter exactly `2`. **Stop:** target change downgraded to warning or a third
  read.

- [ ] S04-008 Complete `WorkbookScaleForecast/v1`: define all count/limit buckets,
  boundary/negative/incomplete inputs and guarantee it consumes only pass telemetry and
  declared limits. **Supports:** T026, T029, T031, T043. **Evidence:** table-driven vectors,
  no-Excel-I/O architecture test and `WORKBOOK_SCALE_DECISION_REQUIRED` for each unknown or
  unsafe input. **Stop:** guessed input, Excel I/O, mutation or catalog qualification claim.

- [ ] S04-009 Define and test the manual live-invocation policy: only a manually started
  interactive `catalog qualify` or a direct current user request may enter the terminal
  credential prompt; no `AuthorizationReference` is accepted or persisted. **Supports:** T027,
  T030, T031, T042, T046. **Evidence:** prompt/send spies are zero on offline and automatic
  paths; manual invocation reaches the prompt without a credential argument/config value.
  **Stop:** autonomous agent start, credential prompt on offline path or secret persistence.

- [ ] S04-010 Implement the policy in the CLI composition root and record safe
  `InvocationSource` (`ManualTerminal` or `DirectCurrentChatRequest`) without storing chat
  content. **Supports:** T027, T030, T046. **Evidence:** process tests cover both permitted
  sources and automatic/offline rejection, with no `AuthorizationReference` verifier port.
  **Stop:** a persisted approval token, agent-selected trust rule or stored credentials.

- [ ] S04-011 Refactor `AppendOnlyRunStore` into an explicit per-run lifecycle: create one
  root, append journal/audit/evidence under that root, reject collision/overwrite/concurrent
  conflict and never create a new root for a record belonging to an existing run. **Supports:**
  T032, T035, T037, T043, T045. **Evidence:** same-time and same-RunId collision tests,
  concurrent write test, journal ordering and one-RunId linkage test. **Stop:** deletion,
  replacement write, root-per-envelope or tracked-workbook storage.

- [ ] S04-012 Define typed `EvidenceEnvelope/v1` subtypes and validate required fields,
  types, nested fields, allowed metadata, hashes and timestamps. Invoke schema validation and
  secret/value scanning before every durable write and before terminal success seal. **Supports:**
  T033, T034, T036, T037, T043, T044. **Evidence:** per-durable-path canaries and malformed
  envelopes result in safe blocked terminal records with no PASS seal; scanner report contains
  category/location/digest only. **Stop:** generic JSON, scanner bypass, redact-after-write or
  raw matched value.

- [ ] S04-013 Persist a single safe qualification outcome package: input identifiers/digests,
  pass manifests/counts/hashes, inventory/unsupported summary, forecast, gate outcomes,
  schema/scan summaries and terminal seal. **Supports:** T031, T037, T043, T045. **Evidence:**
  a clean fixture and each failure fixture have traceable RunId-linked artifacts; fields pass
  the envelope scanner. **Stop:** raw target values/responses, missing failure evidence or
  `HUMAN_REVIEW_REQUIRED` interpreted as approval.

- [ ] S04-014 Implement `catalog diagnose --run <RunId>` in the composition root and a safe
  bounded run reader. Render blocker, scope, recovery and next permitted action from validated
  records only. **Supports:** T038, T040, T042. **Evidence:** CLI process tests cover clean,
  paging, authorization, scan and missing-run cases; output scan has no secret/raw value.
  **Stop:** direct raw-file output, state mutation or an unbounded listing.

## Phase C — S04 live-readiness implementation, proven offline

- [ ] S04-015 Implement `HttpCatalogSource` using a locally hosted fake
  `HttpMessageHandler`/test server, a canonical request factory and a validated local target
  URI. Disable redirects and reject non-loopback test origins. **Supports:** T027, T038, T042,
  T043, T044. **Evidence:** fake handler captures only declared endpoint metadata; all responses
  flow through the same typed parser/reader as fixtures. **Stop:** direct `HttpClient` use,
  arbitrary URL/query, redirect follow or a real network target in test.

- [ ] S04-016 Implement the interactive session boundary after a manual CLI invocation or
  direct current user request:
  masked terminal prompt, in-memory password/cookie/CSRF only, disposal/zeroing and no secret
  serialization. Make prompt and send counters observable in tests. **Supports:** T027, T030,
  T038, T042, T046. **Evidence:** fake handler/session tests prove no prompt/send on offline
  or automatic paths; dispose tests prove ephemeral state is cleared. **Stop:** secret in args/
  config/evidence/logs or session without a permitted invocation. **Human gate:** this is never
  run against BPMSoft during S04 implementation.

- [ ] S04-017 Wire fixture adapter and HTTP adapter through one CLI/application composition
  root. `catalog validate-offline` uses the full shared workflow; `catalog qualify` accepts no
  password/cookie/CSRF argument and requires a manual interactive invocation or direct current
  user request. **Supports:** T028–T031, T038, T040–T043. **Evidence:** process tests invoke
  `Program.Main` for fixture, automatic-path rejection, bad command and diagnose scenarios. **Stop:**
  command-specific helper flow or success without qualification/evidence lifecycle.

- [ ] S04-018 Replace the early handoff package with an executable offline runbook, a distinct
  conditional-manual procedure and failure/recovery guide. State that T046 requires manual
  invocation or a direct current user request and does not perform browser/Excel/Apply/Git. **Supports:** T039, T041, T042,
  T046. **Evidence:** clean-machine dry-run with fixture plus documentation tests for command
  validity, no hidden absolute path, no secret and no false live claim. **Stop:** instructions
  to pass credentials through skills or a claim that `HUMAN_REVIEW_REQUIRED` authorizes work.

- [ ] S04-019 Add CLI, architecture and skill-orchestration boundary tests: exit mappings,
  supported command dispatch, forbidden project references, no business rules in skills and no
  unauthorized secret arguments. **Supports:** T038, T039, T040, T042. **Evidence:** project /
  assembly scan and a fixture dry-run show only safe result fields and zero writes. **Stop:**
  domain-to-adapter dependency, hidden credential channel or untested document command.

- [ ] S04-020 Implement production-workflow offline E2E tests for a clean fixture. Execute
  the CLI composition root, calculate expected inventory/manifests/hashes independently and
  inspect its single run tree. **Supports:** T043. **Evidence:** exactly two reconciled passes,
  complete status coverage, deterministic canonical fingerprint, validated/scanned append-only
  evidence, `HUMAN_REVIEW_REQUIRED`, zero write calls and no live target/credentials. **Stop:**
  direct helper-only test, false PASS or unscanned artifact.

- [ ] S04-021 Implement production-workflow adversarial, metamorphic and leakage regression.
  Cover every fixture from S04-005, reordered response properties, all endpoint bypasses,
  schema/canary failures and bounded termination. **Supports:** T044. **Evidence:** each case
  yields its named blocker or deterministic unchanged hash as appropriate, source reads never
  exceed two, no raw-value leak and zero write calls. **Stop:** any hang, retry/Pass C, scanner
  bypass or method-only write classification.

## Phase D — completion evidence control

- [ ] S04-022 Re-evaluate T025–T042 one by one against the original task text and stop
  conditions. Mark an original task `[X]` only when its source, test and required evidence are
  present; leave incomplete tasks unchecked with a precise gap. **Supports:** T025–T042.
  **Evidence:** review table with original task ID, S04 tasks, commands/exit codes, fixture
  digests, artifact paths and reviewer result. **Stop:** bulk status update or treating a test
  filename/PASS line as completion.

- [ ] S04-023 Prepare and control the fresh-evidence protocol only. It identifies the completed
  composition root, fixture IDs/SHA-256, commands, result destinations and failure-recording
  rules for the later actual T043 and T044 reruns, followed by the sole new T045 evidence
  package in `verification/offline-validation.md`. **Supports:** T043, T044, T045.
  **Evidence:** a dated protocol/control record; it does not execute or close T043/T044 and
  does not create the T045 package. **Stop:** a second/duplicate completion cycle, reuse of
  historical PASS, omitted failure or any statement of live qualification/acceptance/Apply
  permission.

### External historical/supplemental procedure — T046 (not an S04 task)

T046 is a separately human-gated manual read-only procedure, excluded from the 23 canonical
S04 tasks and from S04 implementation completion. Only after fresh T043–T045 evidence, the
human operator may manually start `catalog qualify` for one exact target/scope, or an agent
may act only on a direct current user request; the operator enters credentials in the terminal.
The procedure has exactly Pass A and Pass B and stores safe evidence. It must stop on autonomous
agent start, target/scope change, terminal blocker, any write, browser, Excel or Git action.

## Original-task traceability and execution order

| Original tasks | Corrective prerequisites | Permitted close point |
|---|---|---|
| T025–T031 | S04-001…S04-010, S04-013, S04-017 | After S04-022 verifies exact-two-pass, forecast and admission evidence. |
| T032–T037 | S04-011…S04-013 | After S04-022 verifies per-run storage, schema, scanner and terminal seals. |
| T038–T042 | S04-001, S04-002, S04-014…S04-019 | After S04-022 verifies CLI/architecture/handoff as executable behavior. |
| T043 | S04-001…S04-022, then S04-023 control checkpoint | Actual one-time rerun after all preceding checks pass. |
| T044 | S04-001…S04-022, then S04-023 control checkpoint | Actual one-time rerun; may run parallel to T043 only after common prerequisites. |
| T045 | T043 + T044 | Sole new package only after both successful actual reruns. |
| T046 | External human-gated procedure (not S04-024) | Manual only after fresh T043–T045 evidence and a manual invocation or direct current user request; never closed by offline tests. |

```text
S04-001..005 → S04-006..014 → S04-015..019 → S04-020 + S04-021
  → S04-022 → S04-023 (control only) → T043 + T044 (actual reruns) → T045 (sole package)
  → T046 (external human-gated procedure; not an S04 task)
```
