# Спецификация функции: read-only full-catalog qualification и Excel-пара BPMSoft

**Feature**: `001-read-only-catalog-qualification`  
**Статус**: Черновик для реализации; scope согласован owner decision в `HANDOFF.md`.  
**Неизменяемые источники**: общее видение `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`; MVP intent `implementation-prompts/MVP-live-full-catalog-excel.md`; проекция книг `preparation/docs/WORKBOOK_CONTRACT_VISION.md`.

## Clarifications

### Session 2026-09-08

- Q: Что делать при расхождении полного Pass A и Pass B? → A: Завершать run `TARGET_STATE_CHANGED_DURING_QUALIFICATION`; новый run возможен только после нового manual invocation или прямой текущей просьбы пользователя.

### Session 2026-09-13

- Q: Нужен ли `AuthorizationReference` перед live read-only test? → A: Нет. После offline PASS admission создаёт manual `catalog qualify --manual` или прямая текущая просьба пользователя; credentials вводятся только в terminal и не сохраняются.

### Session 2026-09-14

- Q: Какой materialized result владеет MVP Feature 001? → A: Только после полного совпадения двух full reads Feature 001 создаёт новую атомарную пару `BPMSoft.ModelCatalog.xlsx` и `BPMSoft.LookupCatalog.xlsx` под уникальным `RunId`. Это output adapter, а не Compare/Plan/Apply и не замена будущей Feature 002.

## Пользовательские сценарии

### US1 — безопасно получить полный каталог (P1)

Оператор вручную запускает read-only qualification полного scope. Клиент интерактивно получает URL/login/password, создаёт ephemeral session, читает каждый workspace item, подтверждённые schemas и все доступные lookup registry/collections; никакие credentials, response bodies или raw lookup values не становятся durable.

**Приёмка**: два независимых full passes одного sealed scope вызывают только exact allowlisted requests и завершаются либо совпадающим `QualifiedCatalogSnapshot/v1`, либо одним terminal blocker без retry/Pass C/частичного output.

### US2 — получить проверяемую Excel-пару (P1)

После успешной reconciliation оператор получает новую пару книг в `<output-root>/runs/yyyy/MM/dd/<RunId>/output/`; existing templates и предыдущие runs не перезаписываются.

**Приёмка**: workbook adapter получает values только в памяти из B, создаёт и проверяет обе книги до atomic publication, а read-back подтверждает exact sheet/header inventory, pair/manifest binding, 1:1 source projection и отсутствие external links, VBA, connections и запрещённых formulas.

### US3 — проверить безопасное evidence и diagnostics (P2)

Reviewer видит только schema-validated, append-only metadata, hashes, counts, duration/size buckets, statuses, blockers и относительные output paths; `catalog diagnose --run <RunId>` не читает cells и не раскрывает sensitive data.

**Приёмка**: scanner отклоняет secret/raw-value canaries до durable write/seal; каждый blocker содержит safe reason, scope, recovery и next permitted action.

## Границы и terminal conditions

- Scope включает полный доступный `WorkspaceInventory`, supported EntitySchema metadata и все lookup registry/lookup collections; он не включает ordinary business EntitySchema rows.
- `AUTH_LOGIN`, `WORKSPACE_ITEMS`, `SCHEMA_GET`, `SELECT_QUERY` — единственные request IDs; any method/path/body/origin/query/fragment/redirect mismatch останавливается до send.
- Pass A seals `ScopeDescriptor`; Pass B повторно читает весь scope теми же contracts/limits без reuse A rows/pages/schemas/values. Для materialization разрешено использовать B только после полного equality A/B.
- Paging/shape error даёт scoped terminal blocker. A/B mismatch даёт `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, `RetryCount = 0`; нет Pass C, auto retry или automatic rerun.
- До S07 применяются только fixtures/fake `HttpMessageHandler`. S07 разрешён ровно после accepted S06 и явного текущего ответа пользователя «стенд запущен»; credentials вводит пользователь в terminal.
- `FULL_CATALOG_NOT_QUALIFIED` закрывается только successful S07 run и human decision. `INDEX_SYNC_UNRESOLVED` остаётся; Write/Manage/Compare/Apply/browser/Git/index mutation не входят.

## Зависимости и квалифицированные source IDs

- Feature 001 не требует реализованного predecessor; `preparation/PrototypeReadOnlyPull` и `preparation/WorkbookDeliveryTool` — immutable characterization/technical references, не runtime dependencies.
- Общий immutable source: `local-bpmsoft-synchronizer/spec.md:FR-002`, `FR-003`, `FR-005`, `FR-010`, `FR-011`, `FR-019`, `FR-022`, `NFR-001`, `NFR-003`, `NFR-008`, `NFR-009`.
- Этапные qualified IDs: `01-read-only-catalog-spec.md:READ-001…READ-012` и `05-verification-operations-spec.md:OPS-001…OPS-003`, `OPS-012`, `OPS-013`.
- Future Feature 002 may reuse the output adapter or split it only after implementation evidence; it must not duplicate reader, identity, qualification or snapshot logic.

## Функциональные требования

- **FR-001 — session и secrets**: URL/login/password принимаются только interactive; password/cookie/CSRF/auth headers живут только в памяти и не входят в args, config, code fixtures, logs, audit или evidence.
- **FR-002 — exact deny-by-default transport**: разрешены только `ReadEndpointAllowlist/v1` request IDs `AUTH_LOGIN`, `WORKSPACE_ITEMS`, `SCHEMA_GET`, `SELECT_QUERY`, exact origin и canonical request shapes; write-call count всегда 0.
- **FR-003 — ordered full reader**: каждый registry/lookup collection имеет declared stable order, canonical `SelectQuery` with explicit columns/`allColumns=false`, bounded page/row/byte limits, terminal empty page и guards for duplicate/overlap/gap/loop/empty-middle/nonempty-after-terminal.
- **FR-004 — sealed exact-two-pass qualification**: Pass A и Pass B — два полных independent reads одного scope; reconciliation сравнивает scope/version evidence, ordered identities, counts, manifests, component/value hashes, support/unsupported sets и `TargetFingerprint/v1`.
- **FR-005 — lossless workspace/object model**: сохраняются typed workspace/package-layer/schema/column/index identities, own/inherited columns, references, indexes and member ordinal order; member relation строится только через `schema.indexes[].columns[].columnUId`.
- **FR-006 — complete lookup data**: registry records и все discovered lookup schemas/rows/supported columns сохраняют `Null|EmptyString|Value`, value kind, canonical value, reference record ID и row/source fingerprints; unsupported shapes становятся lossless diagnostic или named blocker, не silent omission.
- **FR-007 — qualified snapshot boundary**: `QualifiedCatalogSnapshot/v1` возникает только после equality A/B and keeps structured catalog, normalized lookup values, run/pair identities, fingerprints, diagnostics, counts and scale telemetry without HTTP/console/Excel/browser/Git types.
- **FR-008 — Excel pair**: adapter creates a fresh atomic Model/Lookup pair with required sheet order and headers from workbook contract; pair is 1:1 projected, read-back/OOXML closed and has no external link/connection/VBA/cross-workbook or forbidden formula.
- **FR-009 — data versus evidence**: raw lookup values are permitted only in local workbook output. `audit/`, `evidence/`, `run-journal.json`, CLI and diagnostics permit only schema-approved safe metadata and are scanned before each durable write and seal.
- **FR-010 — append-only run and diagnostics**: one unique `RunId` root has `audit/`, `evidence/`, `output/` and journal; collision/overwrite/partial pair fails closed. `catalog diagnose --run` displays only safe blocker/recovery/next action.
- **FR-011 — production CLI and offline E2E**: fixture and fake HTTP paths use the same production composition root; old `catalog validate-offline`, `catalog qualify` and `catalog diagnose` remain compatible; offline test path never prompts credentials, sends live HTTP or claims live qualification.
- **FR-012 — opt-in live integration**: after S06, a user-confirmed manual one-time `--live --manual` run performs exactly the production full workflow, creates/verifies a new pair and emits safe aggregates; unavailable/login/paging/target-change blocker stops without retry.
- **FR-013 — documentation and handoff**: runbook describes prerequisites, interactive command, target policy, output root, output/evidence separation and all stop/recovery rules; S08 records facts, not inferred PASS.
- **FR-014 — architectural and source boundary**: domain/application do not depend on BPMSoft/Excel/browser/Git adapters; `preparation/**` remains immutable reference and every reused legacy semantic is classified `reuse-semantics|rewrite|drop|defer`.

## Ключевые сущности

`ScopeDescriptor`, `CatalogPass`, `PageManifest`, `WorkspaceInventoryItem`, `LosslessShapeEnvelope`, `LookupRecord`, `NormalizedLookupValue`, `TargetFingerprint`, `QualifiedCatalogSnapshot`, `WorkbookPair`, `Run`, `AuditEvent`, `EvidenceEnvelope`, `Blocker`.

## Измеримые критерии успеха

- **SC-001**: capture proves 100% allowlisted calls and zero writes on all fake/live paths.
- **SC-002**: full fake catalog proves exactly two independent complete reads and equality/reconciliation, or terminal `TARGET_STATE_CHANGED_DURING_QUALIFICATION` with no third read/retry.
- **SC-003**: 100% accessible workspace and lookup scope is represented or blocked explicitly; no identity/value/unknown-shape is silently omitted.
- **SC-004**: all adversarial paging/endpoint/envelope fixtures fail closed with named blocker and no hang.
- **SC-005**: generated pair passes exact 1:1 projection, read-back, OOXML closure and atomic-publication fault tests; no partial successful pair exists.
- **SC-006**: raw lookup/credential/session canaries are absent from 100% audit/evidence/journal/CLI/diagnostic outputs while permitted `LookupValues` remain in output workbook.
- **SC-007**: S06 Release build and all custom offline executables produce fresh safe report with fixture IDs/SHA-256, commands/exit codes and output-tree hashes.
- **SC-008**: S07 runs exactly once after current-chat confirmation, prompts credentials only in terminal, publishes a distinct real-data pair or one safe blocker, and leaves `FULL_CATALOG_NOT_QUALIFIED` open unless human decision follows success.

## Допущения и anti-scope

`L2 / l2-pilot` remains bounded. Existing code/tests are characterization evidence only; no unchecked task is accepted. Feature 001 may materialize read-only output but does not create a canonical workbook baseline, Compare, Plan, Apply, Write/Manage, browser action, Git action, deletion or index mutation.
