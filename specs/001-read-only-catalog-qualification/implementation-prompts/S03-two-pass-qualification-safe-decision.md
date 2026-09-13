# Prompt реализации S03: two-pass qualification and safe decision

## Цель

Реализовать S03 из [implementation-slices.md](../implementation-slices.md):
exact-two-pass offline qualification, manual live-invocation boundary, append-only safe evidence,
safe diagnosis, handoff package и финальную offline validation.

## Предусловие

S01 и S02 завершены и проверены. Перед началом прочитать [tasks.md](../tasks.md),
[test-plan.md](../test-plan.md), [research.md](../research.md),
[data-model.md](../data-model.md), [quickstart.md](../quickstart.md),
[contracts/cli-contract.md](../contracts/cli-contract.md), [plan.md](../plan.md),
[spec.md](../spec.md) с Clarifications и конституцию.

Это delegated subtask formal `/SpecKit Implement`, но не завершение Feature 001.
До первой записи в worktree исполнитель ОБЯЗАН:

1. прочитать `AGENTS.md`, `.agents/skills/speckit-implement/SKILL.md`,
   `.specify/feature.json`, `.specify/project.yml`, `.specify/extensions.yml`,
   `.specify/memory/constitution.md`, этот prompt, `implementation-slices.md`,
   все перечисленные выше feature-артефакты, фактический код и текущие regression tests;
2. подтвердить exact target
   `specs/001-read-only-catalog-qualification`, успешный S02 report
   `tests/evidence/S02-offline-validation-summary.md` и completion T024;
   `FULL_CATALOG_NOT_QUALIFIED` и `INDEX_SYNC_UNRESOLVED` должны оставаться открытыми;
3. прочитать immutable common vision
   `preparation/docs/product-specs/local-bpmsoft-synchronizer/spec.md`; не изменять
   ничего в `preparation/docs/product-specs/`;
4. проверить `.specify/memory/spec.md`, `.specify/memory/plan.md`,
   `.specify/memory/changelog.md`, predecessor artifacts, фактический код и regression
   tests; отсутствующее явно считать отсутствующим;
5. выполнить mandatory pre-hook `speckit.class.gate` для `/SpecKit Implement` и
   продолжать только после `PASS`;
6. один раз выполнить `.specify/scripts/powershell/check-prerequisites.ps1 -Json
   -RequireTasks -IncludeTasks`, подтвердить read-only status всех `checklists/`.
   При unchecked item остановиться и запросить решение пользователя.

Не выполнять Git commands и не создавать/изменять `.gitignore`. После S03 не запускать
`speckit.converge`, `speckit.verify.run`, Jira sync, `speckit.archive.run` или другие
feature-wide `after_implement` hooks: они выполняются только после полного formal run
всей Feature 001.

### Обязательное дополнение к handoff: test foundation S03

В текущем solution отсутствует `tests/BpmSoftSync.Adapters.FileSystem.Tests/`, хотя
T032–T037 требуют тесты этого проекта. До написания T032 создать BCL-only консольный
test harness `tests/BpmSoftSync.Adapters.FileSystem.Tests/` и добавить его в
`BpmSoftSync.sln`; подключить только `src/BpmSoftSync.Adapters.FileSystem/` и нужные
typed contracts. Это часть test foundation для T032, а не отдельная feature-задача.
Нельзя добавлять external BPMSoft client/test packages, сетевой transport, Excel,
browser или Git types. Проверить, что solution собирается с `net10.0`, nullable и
warnings-as-errors до перехода к T032–T034.

## Границы задачи

Выполнять T025–T037, затем cross-slice T038–T042, затем T043–T045 в dependency order.
T046 не является автоматическим продолжением: это отдельный request-only human-controlled
procedure после успешных T043–T045.

## Непереговорные checks

- Ровно Pass A и Pass B. При target change — terminal
  `TARGET_STATE_CHANGED_DURING_QUALIFICATION`, sealed safe artifacts, `retryCount = 0`,
  без Pass C/retry/automatic new double pass.
- Offline и automatic paths имеют zero terminal credential prompts и zero HTTP sends;
  вручную запущенный interactive `catalog qualify` не требует `AuthorizationReference`.
- `WorkbookScaleForecast/v1` diagnostic-only: zero Excel I/O, никакой acceptance claim.
- Evidence allow-by-schema, scanner before durable write/seal, append-only unique run roots;
  canary/schema failure исключает PASS и не раскрывает значения.
- Handoff и CLI diagnostics содержат только safe reason/scope/recovery/next action.
- T043–T045 используют исключительно fake transport/sanitised fixtures.

## T046: отдельная человеческая граница

Не запускать live run автоматически: он начинается вручную оператором для одного exact
target и declared read scope либо агентом только после прямой текущей просьбы пользователя.
`AuthorizationReference` не используется. После terminal target change не выполнять новый run без
нового ручного запуска или прямой текущей просьбы пользователя. Никогда не выполнять Write/Manage, browser write, Excel,
compare, Apply или Git actions.

## Результат

Считать завершёнными только фактически пройденные offline tasks/checks. `HUMAN_REVIEW_REQUIRED`
— evidence decision point, не команда на live start, не acceptance и не Apply permission.
