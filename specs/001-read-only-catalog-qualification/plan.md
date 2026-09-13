# План реализации: безопасное чтение и qualification каталога BPMSoft

**Ветка**: `001-read-only-catalog-qualification` | **Дата**: 2026-09-14 | **Spec**: [spec.md](spec.md)

## Summary

Feature 001 создаёт первый reusable слой локального .NET 10/C# CLI: интерактивную read-only сессию, deny-by-default HTTP boundary, полный inventory и два последовательных детерминированных прохода каталога. Результат — append-only `run/audit/evidence`, безопасная диагностика и human decision point; он не создаёт Excel, compare-plan, Apply, browser/Git workflow или index mutation.

Технические решения Phase 0 намеренно отделены от решений владельца. В частности, endpoint allowlist, `TargetFingerprint/v1` и `EvidenceEnvelope/v1` — проверяемые Plan/Research contracts, а не разрешение live run или Write/Manage.

S04 — corrective reconciliation этого же feature, а не новая feature или второй reader. Он
восстанавливает недостающий production composition: один typed workflow
`reader → qualification/reconciliation → evidence` должен обслуживать и sanitized fixture,
и fake-handler-proven HTTP adapter. `s04-live-readiness-slice.md`,
`clarify-s04-live-readiness.md` и repo-native pack `slice-packs/S04-live-readiness/` —
входы планирования; завершённая после этого Plan стадия `/SpecKit Tasks` уже внесла
S04-001…S04-023 в canonical `tasks.md`, который остаётся единственным formal task tracker.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`, nullable, warnings as errors).

**Primary Dependencies**: BCL `HttpClient`, `System.Text.Json`, `System.Security.Cryptography`; test project должен использовать стандартный .NET test runner и изолированный fake HTTP handler. Внешняя BPMSoft client library не допускается в domain core.

**Storage**: append-only локальные файлы в user-selected run root: `runs/yyyy/MM/dd/<RunId>/{audit,evidence}/` и `run-journal.json`. Секреты, response bodies и raw lookup values — не storage.

**Testing**: unit/domain, HTTP contract/capture, fixture integration и production-composition CLI/process tests; HTTP tests используют только fake `HttpMessageHandler`/transport, реальных target/credentials в tests нет. Отдельная matrix покрывает exact allowlist, malformed response, paging mutation, target change, schema и secret/raw-value canaries. На старте production tests отсутствуют; legacy `preparation/PrototypeReadOnlyPull` и `preparation/WorkbookDeliveryTool` — только characterization/reference material.

**Target Platform**: Windows local BPMSoft, guided local CLI. Никаких browser actions, Excel write, cloud target или Git operation в этой feature.

**Project Type**: локальное CLI-приложение с typed domain core и adapter ports.

**Performance Goals**: закончить или fail-closed по каждой collection до `MaxPages`; сохранённые telemetry включают pages, counts, duration и response-size buckets. Числовые limits и full-catalog scale остаются измеряемыми конфигурационными constraints, не выдуманными acceptance promises.

**Constraints**: интерактивная аутентификация только в памяти; HTTP разрешён только exact `ReadEndpointAllowlist/v1` из Decision 1 в `research.md` (`AUTH_LOGIN`, `WORKSPACE_ITEMS`, `SCHEMA_GET`, `SELECT_QUERY`; `GET_PACKAGES` должен быть удалён как S04-001 defect); deterministic order + paging; ровно Pass A и Pass B; no Pass C, retry или automatic requalification; lossless unsupported diagnostics; один `RunId` с append-only evidence и schema/scanner до каждого durable write и success seal.

**Scale/Scope**: до успешных offline checks допускаются только offline fixtures и автономные checks; `FULL_CATALOG_NOT_QUALIFIED` остаётся активным как состояние qualification. После них feature предусматривает отдельный ручной read-only run: оператор вручную запускает `catalog qualify` для конкретных target/scope и вводит credentials в terminal, либо агент действует по прямой текущей просьбе пользователя в доступном чате. `AuthorizationReference`, trusted issuer и reference verifier отсутствуют и не требуются; автоматический запуск запрещён. Неизвестные login/CSRF/response shapes не предполагаются: они остаются fail-closed границей sanitized fixture/fake-handler evidence.

## Constitution Check

### До Phase 0 — PASS с сохранёнными gates

| Проверка | Плановое соблюдение |
|---|---|
| Read-only boundary, credential handling | CLI получает URL/login/password интерактивно; password, cookies и CSRF удаляются из памяти при выходе и исключены из CLI args, skills, logs/evidence. Capture тесты запрещают любой HTTP method/path вне contract. |
| Identity и package layer | Domain types разделяют `ServerId`, `SchemaUId`, `ColumnUId`, `RegistryId`, `PackageLayerId`; display `Name`/`Code` — diagnostic only, never join key. |
| Paging и lossless handling | Каждая collection имеет declared order key, cursor/offset state, duplicate/overlap/gap/termination guards; любой unknown shape создаёт immutable envelope либо blocker, никогда default/drop. |
| Plan/apply/index scope | В feature отсутствуют workbook write, compare, Apply, browser write, Git и index mutation. `INDEX_SYNC_UNRESOLVED` сохраняется. |
| Audit/security | Run root append-only; `EvidenceEnvelope/v1` allow-by-schema + pre-success scanner не допускают credentials, session data и raw lookup values. |
| Human authority | Manual `catalog qualify` или прямая текущая просьба пользователя в доступном чате достаточны для live read-only test; отдельный owner decision и `AuthorizationReference` не требуются. Агент не запускает live run без такой просьбы. Это не закрывает `FULL_CATALOG_NOT_QUALIFIED` до evidence и не даёт Write/Manage permissions. |

**Результат**: PASS для проектирования. Никакой PASS здесь не является разрешением live BPMSoft access.

### После Phase 1 — PASS с открытыми рисками

Design contracts сохраняют все перечисленные safeguards. Неустранимые в Plan human gates и доказательные риски вынесены в [research.md](research.md), [test-plan.md](test-plan.md) и [quickstart.md](quickstart.md); они не преобразованы в assumptions.

## Project Structure

```text
src/
  BpmSoftSync.Cli/                 # composition root, prompts, exit mapping
  BpmSoftSync.Domain/              # identity, inventory, qualification, blockers, evidence contracts
  BpmSoftSync.Application/         # use cases and ports; no HTTP/Excel/browser/Git references
  BpmSoftSync.Adapters.BpmSoft/    # session, endpoint classifier, HTTP capture, response adapters
  BpmSoftSync.Adapters.FileSystem/ # append-only run store, schema validation, secret/value scan
tests/
  BpmSoftSync.Domain.Tests/
  BpmSoftSync.Adapters.BpmSoft.Tests/
  BpmSoftSync.Cli.Tests/
  fixtures/read-only/              # sanitized captures and adversarial paging/shape cases
specs/001-read-only-catalog-qualification/
  contracts/cli-contract.md
```

**Structure Decision**: новый solution размещается в `src/`/`tests/`; существующие `preparation/*` не становятся runtime dependency. Skills могут only call documented CLI commands and render safe diagnostics; они не содержат parser, paging, fingerprint или policy implementation.

## Delivery Sequence

1. Создать shared typed domain contracts: identities, `WorkspaceInventory`, support statuses, `Blocker`, `CatalogPass`, `CatalogQualification`, `TargetFingerprint`, `Run`, `AuditEvent` и `EvidenceEnvelope`.
2. Реализовать endpoint/session adapter: interactive prompt, in-memory cookie container/CSRF, exact endpoint classifier, request capture and fail-before-send behavior.
3. Реализовать generic ordered collection reader и inventory/schema adapters. Перенести только characterized semantics from prototypes after explicit `reuse-semantics|rewrite|drop|defer` classification; no port-by-copy.
4. Реализовать qualification orchestrator: pass A then pass B over the same authorised scope; reconcile identities/counts/hashes/telemetry/unsupported inventory. Различие, классифицированное как target-state change, emits `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, seals the run and forbids retry in-process.
5. Реализовать append-only run store, allowed evidence envelopes, schema validation and secret/value scanner; CLI exposes only safe outcome/recovery/next-action text.
6. Add fixtures, contract capture and CLI tests from [test-plan.md](test-plan.md). После успешных offline checks подготовить conditional manual live-verification path: ручной CLI run либо выполнение по прямой текущей просьбе пользователя, terminal credential prompt и безопасный evidence review. Tasks не выполняют этот run автоматически и не сохраняют credentials.

## S04 corrective reconciliation and dependencies

### Причина и change impact

Static review of the actual worktree confirms that the earlier foundation is not yet a
complete workflow: `ICatalogSource` validates only a fixture, qualification receives
synthetic passes, the runtime/test allowlist contains undocumented `GET_PACKAGES`, the
fingerprint and run-store lifecycle do not yet satisfy the research contracts, `catalog
qualify` is blocked by the historical `AuthorizationGate`/`AuthorizationReference`, and
the current E2E tests compose helpers rather than the production root. These are S04
corrections, not permission to rewrite settled requirements or claim completion of T025–T045.

Reuse the existing Domain/Application/adapter separation, identity and safety contracts,
declared reader/paging concepts, evidence validator/scanner, fixtures and regression test
projects. Change only the incomplete implementations and their tests so the shared workflow
is real; do not create a separate live reader, a second HTTP path, a bypass around the
reader/reconciliation/run-store, or a new feature context. Future Features 002–004 must
reuse this one pipeline.

### Planned dependency order

1. S04-001…S04-005 reconcile exact endpoint, typed source/transport, canonical
   `TargetFingerprint/v1`, bounded reader/inventory and manifest validation.
2. S04-006…S04-014 complete the real two-pass use case, reconciliation and forecast,
   replace the old authorization boundary with the settled manual/direct-current-request
   invocation policy, make one append-only per-`RunId` lifecycle, and wire safe diagnosis.
3. S04-015…S04-021 prove the fixture and fake-HTTP adapters through the same CLI/application
   composition root and complete offline E2E, adversarial, metamorphic and leakage coverage.
4. Only after the preceding implementation evidence, S04-022 re-evaluates original T025–T042
   individually. S04-023 is only the preparatory/control checkpoint; the actual one-time
   T043/T044 reruns follow it, and T045 is the sole new evidence package. T046 is outside
   S04 and requires the separate G-2 manual invocation or direct current user request.

The completed `/SpecKit Tasks` reconciliation has mapped this order and its exact
S04-001…S04-023 identifiers into canonical `tasks.md` without marking historical
T025–T046 complete merely because a slice, pack, old PASS or planned test exists.

### S04 invariants and evidence scope

- The runtime endpoint contract is exactly Decision 1 in `research.md`; any unclassified
  method, path, body, origin, query/fragment, redirect or alternate host fails before send.
- Each invocation uses one immutable target/scope snapshot and exactly Pass A then Pass B.
  `TARGET_STATE_CHANGED_DURING_QUALIFICATION` is terminal with `RetryCount = 0`; there is no
  Pass C, retry or automatic new double pass.
- Offline/automatic paths prove zero credential prompt and zero HTTP send. A later manually
  started interactive command or direct current user request alone may reach an in-memory
  terminal prompt; this neither authorizes Write/Manage nor reopens browser, Excel, compare,
  Apply or Git scope.
- `TargetFingerprint/v1` uses Decision 2 canonical JSON. Every durable path is under one
  `runs/yyyy/MM/dd/<RunId>/` root; schema validation and secret/raw-value scanning happen
  before each durable write and before success seal. `catalog diagnose --run <RunId>` reads
  only validated safe records.
- Fresh T043–T045 evidence must identify the new build, fixture IDs/SHA-256, commands and
  exit codes, run-tree hashes, reconciliation/schema/scan summaries and test results. The
  historical `verification/offline-validation.md` is not reusable evidence for S04.

## Test Strategy

Detailed coverage, evidence and commands: [test-plan.md](test-plan.md). Increment tests cover US1–US3 and FR-001…FR-019; S04 makes the listed final tests run through the production composition root rather than helper-wired approximations. Final automated tests prove offline behaviour with captured/sanitized fixtures and fake HTTP only. После них предусмотрена отдельная ручная live verification по ручному запуску оператора либо прямой текущей просьбе пользователя; она не является автоматическим acceptance и не разрешает write.

## Complexity Tracking

Нет нарушений Constitution Check, требующих оправдания.
