# Owner-directed corrections после review r003

**Run ID**: `20260907-165314-7f12`  
**Автор контекста**: существующая задача `03-spec-reviewer`  
**Основание**: последовательные решения владельца по RF-001–RF-004 в этой задаче  
**Статус**: `CORRECTIONS_APPLIED_PENDING_INDEPENDENT_REVIEW`  
**Дата**: 2026-09-08

## 1. Граница и происхождение решений

Исторические `03-review-report.md`, `03-findings.json`, `manifests/r003.json` и
`handoffs/r003.md` не изменялись. Формальная отдельная задача роли 04 и её Phase A
не запускались: владелец проекта попросил разобрать RF-002–RF-004 по одному и после
каждого обсуждения прямо разрешил соответствующую коррекцию в текущей задаче.
Поэтому этот документ не выдаёт текущую работу за независимый review или за завершённую
роль 04; все четыре результата имеют статус `FIXED_PENDING_REVIEW`.

Зафиксированные решения владельца:

- **RF-001**: `«RF-001 - это ошибка последовательности в самом инструменте. Давай закрепим в правилах текущего проекта, что можно запускать Analyze после tasks...»`.
- **RF-002**: `«по RF-002 да, давай.»` — принят двухэтапный deterministic sampling contract.
- **RF-003**: после обсуждения ненужности отдельного `PlanArtifactHash` владелец ответил `«да»` — принят один product identity hash `PlanHash` и fixed `RunContext`.
- **RF-004**: владелец отделил технически несостоявшуюся browser verification от фактического UI mismatch и затем явно решил `«разрешить writeback»` при pending-проверке.

Эти решения не разрешают live BPMSoft access, Apply, browser writes, Git commit/push,
write allowlist или запуск SDD lifecycle.

## 2. Выполненные коррекции

### RF-001 — Analyze после Tasks

**Статус**: `FIXED_PENDING_REVIEW`.

В `AGENTS.md` добавлено узкое project-local правило для `L2 / l2-pilot`:
`/SpecKit Plan` → `/SpecKit Tasks` → `/SpecKit Analyze` → `/SpecKit Implement`.
Analyze требует complete `tasks.md` и сохраняет `-RequireTasks`; все hooks остаются
обязательными на фактических command boundaries. Shared workflow/overlay и skills
не изменялись, placeholder `tasks.md` не создавался.

### RF-002 — deterministic browser sample без hash cycle

**Статус**: `FIXED_PENDING_REVIEW`.

В feature 003/004 и matrices определён `SamplingBasisHash` от canonical plan payload
до `browserSample`, исключая `browserSample` и `PlanHash`. Операции ранжируются по
SHA-256 canonical tuple (`SamplingBasisHash`, operation kind, stable operation key),
с deterministic tie-break, обязательным new/update coverage и сохранением exact sample
keys в plan. Feature 004 только валидирует и использует сохранённую выборку без resampling.

### RF-003 — один product hash и fixed RunContext

**Статус**: `FIXED_PENDING_REVIEW`.

`RunContext` (`RunId`, `CreatedAtUtc`) фиксируется один раз до compare. При одинаковых
domain inputs и том же `RunContext` canonical bytes, hashes и order идентичны; новый
context намеренно создаёт новый plan instance. `PlanHash` покрывает весь canonical plan
после sample, кроме собственного поля. Отдельный product `PlanArtifactHash` не введён;
обычный file SHA-256 допустим только как audit/manifest checksum. Unknown fields,
duplicate JSON keys и non-canonical bytes блокируются.

### RF-004 — browser outcomes, writeback и Git boundary

**Статус**: `FIXED_PENDING_REVIEW`.

Определены три результата:

- `VERIFIED`: 100% автоматической либо самостоятельной read-only проверки exact keys
  подтверждены evidence; только этот статус допускает последующее human success
  confirmation и Git success.
- `VERIFICATION_PENDING`: automation не смогла завершить проверки из-за browser,
  login/session или tooling. Оператор получает уведомление и выбор: самостоятельная
  проверка либо помощь с устранением причины. Apply не объявляется неуспешным.
- `VERIFICATION_FAILED`: завершённая проверка подтвердила, что ожидаемое изменение
  отсутствует или не видно в UI стенда. Git success запрещён; диагностика, исправление
  или откат требуют отдельного решения оператора.

После successful full CLI read-back workbook-only writeback разрешён при `VERIFIED`
и `VERIFICATION_PENDING`, остаётся local-only, делает 0 BPMSoft calls и не разрешает
Git. Если после writeback подтверждён `VERIFICATION_FAILED`, а отдельное исправление
или откат изменили BPMSoft, workbook требует нового successful CLI read-back и повторного
writeback. Автоматический rollback и автоматическая компенсация workbook запрещены.

## 3. Изменённые файлы

- `AGENTS.md` — RF-001.
- `specs/003-compare-read-only-plan/spec.md` — RF-002/RF-003.
- `specs/003-compare-read-only-plan/checklists/requirements.md` — повторная валидация RF-002/RF-003.
- `specs/004-gated-apply-operations/spec.md` — RF-002/RF-003/RF-004.
- `specs/004-gated-apply-operations/checklists/requirements.md` — повторная валидация RF-002/RF-003/RF-004.
- `specReadinessPrompts/runs/20260907-165314-7f12/traceability.md` — решения и recheck contracts RF-002–RF-004.
- `specReadinessPrompts/runs/20260907-165314-7f12/feature-dependencies.md` — cumulative execution/writeback/Git contract.

Удалений, production-code changes, live operations, hooks, lifecycle commands,
browser execution и Git operations не было.

## 4. Проверки

- `git diff --check`: PASS (только существующие line-ending warnings вне коррекции).
- Статусы `VERIFIED`, `VERIFICATION_PENDING`, `VERIFICATION_FAILED`, разрешённый
  writeback-under-pending и запрет automatic rollback присутствуют в нормативных FR/SC.
- RF-002 basis/rank/sample и RF-003 fixed-context/canonical-hash contracts согласованы
  между feature 003, feature 004, traceability и dependency map.
- Immutable tree `preparation/docs/product-specs/`: 19/19 файлов; aggregate SHA-256
  `1D62DB3B4527226570985744F5471D98DE20C2D85E7CA112150E7D593EE874DD`, совпадает
  с `source-manifest.json`.
- Остальные immutable policy/source files из source manifest, кроме явно изменённого
  project-local `AGENTS.md`, совпадают по bytes и SHA-256.
- Shared `.specify/workflows/speckit/class-workflow.yml`, overlay `l2-pilot.yml`,
  SpecKit skills/scripts, constitution и project settings не изменялись этой коррекцией.

## 5. Открытые пункты и следующий переход

Исторические RF-005, RF-006 и RF-007 не исправлялись и остаются открытыми.
RF-001–RF-004 нельзя считать независимо закрытыми той же задачей, которая внесла
изменения. Следующий безопасный переход — возврат r004 Оркестратору. Оркестратор должен
сверить manifest/handoff, обновить собственное `orchestration.json` и получить отдельное
согласие пользователя перед запуском независимой перепроверки либо дальнейшего RF scope.

Готовность текущей передачи: `READY_FOR_ORCHESTRATOR_RECONCILIATION`,
`NOT_YET_INDEPENDENTLY_REVIEWED`, `IMPLEMENTATION_NOT_STARTED`, live/write `DENIED`.
